module ASTUtils

open System

//open FSharpPlus
open FStar.Parser.AST
open FStar.Ident
open FStar.Const
//open FStar

module S = FSharpx.String

type AST = modul list * (string * FStar.Range.range) list

(* parenthesises a term *)
let paren tm = mk_term (Paren tm) tm.range tm.level

(* constructs a term by applying constructor at x, at the range and level of tm. 
   eg. mk_term_at Paren (tm1:term) tm1 => (tm1) *)
let mk_tm_at (constructor: 'a -> term') (x:'a) (tm:term) : term =
    mk_term (constructor x) tm.range tm.level

(* applies f at x, at the level of x *)
let mk_explicit_app_at (f:term) (x:term) : term = mk_tm_at App (f, x, Nothing) x


(* converts an integer n to an unsigned integer literal at the level of the second argument. *)
let mk_int_at (n:int) : term -> term = 
    mk_tm_at Const (Const_int (n.ToString(), None))


(* applies a function with name fstring at tm *)
let mk_explicit_app_str (fs:string) (x:term) : term =
    let f = mk_tm_at Var (lid_of_str fs) x
    mk_explicit_app_at f x

(*  qualifies ident with nsstr as a namespace. 
    nsstr is of the form "Ab.Bc.Cd.De..." where each namespace identifier must begin with an uppercase.
    eg. add_ns_ident "Foo.Bar" baz => Foo.Bar.baz *)
let qual_ns_ident (nsstr:string) (ident:ident) : lid = 
    let firstCharIsUpper : string -> bool = Seq.head >> Char.IsUpper
    
    let ns_ids = S.splitChar [|'.'|] nsstr
    let ns:list<ident> = ns_ids |> Array.map (fun (s:string) -> {idText=s; idRange=ident.idRange}) 
                                |> List.ofArray
    if not (Array.forall firstCharIsUpper ns_ids)
        then invalidArg nsstr "Invalid namespace identifier format";
    
    {ns=ns; ident=ident; nsstr=nsstr; str=String.concat "." [nsstr; ident.idText]}

(* equivalent to add_ns_ident, where the second argument is a string. *)
let qual_ns_str (nsstr:string): string -> lid = id_of_text >> qual_ns_ident nsstr

(* given a term x , elaborates it with a ret. eg. x => (ret (x)) *)
let mk_ret (x:term) : term =
    let ret = mk_tm_at Var (qual_ns_str "Zen.Cost" "ret") x
    mk_explicit_app_at ret (paren x) |> paren

(* Increments an applied function by n if 0 < n. eg x => (inc (x) n) *)
let mk_inc (expr:term) n =
    if n < 0 then failwith "Error: negative increments should be impossible"
    else match n with
         | 0 -> expr
         | _ -> let inc = mk_tm_at Var (qual_ns_str "Zen.Cost" "inc") expr
                mkExplicitApp inc [paren expr; mk_int_at n expr] expr.range 
                |> paren

// These names are not permitted to be bound in a user module
let reserved_names =
    ["cost";"Cost";
    "ret";"Ret";
    "hide";"Hide";
    "inc";"+!";"!+";
    "op_Plus_Bang";"op_Bang_Plus";
    ">>=";">>==";
    "op_Greater_Greater_Equals";"op_Greater_Greater_Equals_Equals";
    "=<<";"==<<";
    "op_Equals_Less_Less";"op_Equals_Equals_Less_Less";
    "+";"-";"*";"/";
    "op_Plus";"op_Minus";"op_Star";"op_Slash";
    "log";"flog";"clog";"exp";"pown";"pow";"logkn"]

(* similar of FStar.Parser.ToDocument.unparen, since the .fsi does not expose it *)
let rec unparen t =
  match t.tm with
  | Paren t -> unparen t
  | _ -> t

(* clone of FStar.Parser.ToDocument.matches_var, since the .fsi does not expose it *)
let matches_var t x =
    match (unparen t).tm with
        | Var y -> x.idText = text_of_lid y
        | _ -> false

let check_ident ident =
    if List.contains ident.idText reserved_names then failwith ("Binding to \"" + ident.idText + "\" is not permitted.")
    else ();
// Fails if a declaration uses a name from the cost library, otherwise returns pat.
let rec check_pattern pat =
    match pat.pat with
    | PatApp (p1, ls_pat) ->
        check_pattern p1;
        ls_pat |> List.iter (fun p -> check_pattern p);
    | PatName lid -> // What is this?
        check_ident lid.ident
    | PatVar (ident,_)
    | PatTvar (ident,_) -> // What is this? Looks like a PatVar to me
        check_ident ident
    | PatList ls_pat
    | PatTuple (ls_pat, _)
    | PatOr ls_pat -> // What is this?
        ls_pat |> List.iter (fun p -> check_pattern p);
    | PatRecord ls_lid_pat -> // When does this occur?
        ls_lid_pat |>
        List.iter (fun (l,p) ->
            check_ident l.ident;
            check_pattern p);
    | PatAscribed (p1,_) -> check_pattern p1;
    | PatOp op_ident ->
        let symbol = op_ident.idText
        if  reserved_names |> List.contains symbol then failwith ("Binding to \"" + symbol + "\" is not permitted.")
    | _ -> ();

let rec pat_cost {pat=pat} = 
    match pat with
    | PatWild
    | PatConst _
    | PatVar _
    | PatName _
    | PatTvar _
    | PatOp _ -> 1
    | PatAscribed (pat, _) -> 
        pat_cost pat
    
    | PatList pats
    | PatVector pats
    | PatTuple (pats, _)
    | PatOr pats ->
        pats |> List.map pat_cost
             |> List.sum
    
    | PatRecord fields ->
        let _, field_pats = List.unzip fields
        field_pats |> List.map pat_cost
                   |> List.sum
    
    | PatApp (patn, arg_pats) ->
        let patn_cost = pat_cost patn
        let arg_pats_costs = List.map pat_cost arg_pats
        let sum_arg_pats_costs = List.sum arg_pats_costs
        patn_cost + sum_arg_pats_costs

(* returns a tuple of the cost of an ast branch, and the elaborated branch. *)
let rec elab_term_branch 
    ({tm=tm; range=range; level=level} as branch)
    : int * term = 
    
    let mk_term_here (tm':term') : term = 
        mk_term tm' range level
    
    match tm with
    | Wild (* The hole, [_] *)
    | Const _ (* Effect constants,
                 The unit constant [()],
                 Boolean constants [true, false],
                 Integer constants [7, 0x6F, 13UL],
                 Character constants ['c'],
                 Float constants [1.23],
                 ByteArray constants ["c"B, "q"B, "?"B]
                    Note that the bytes are in reversed order.
                    eg. 'c' = 0x0063, but the bytearray is [|0x63uy; 0x00uy|].
                 String constants ["this is a string constant"]
                 Range constants (not denotable in source code)
                 Reification constants [reify]
                 Reflection constants [lident1?.reflect, lident2?.reflect] *)
    | Tvar _ (* Type variable names [ident1, ident2] *)
    | Uvar _ (* Universe variable. Should not be elaborated. *)
    | Var _  (* Variable names [lident1, lident2, Foo.Bar.baz] *)
    | Name _ (* Non-variable names; begin with uppercase. [Lident1, Foo.Bar.Baz] *)
        -> (0, branch)
    | Projector (tm',lid) -> // terms like Cons?.hd, NOT THE SAME AS "Project"
        (1, branch)   
    | Project (tm,lid) -> // terms like tm.lid
        let tm_cost, tm_elabed = elab_term_branch tm
        let project = mk_term_here <| Project (tm_elabed, lid)
        (1 + tm_cost, project)
    
    | Abs ( ( [ { pat=PatVar (x, typ_opt) } ] as pat),
            ( { tm=Match (maybe_x, branches) } as match_term ) )
        when matches_var maybe_x x ->
        (* Special case of [fun x -> match x with ...], 
           To be handled as [function | ...].
           F* uses this representation for [function]. 
           Should really use a unique constructor... *)
            let match_cost, match_elaborated = elab_term_branch match_term
            let function_term = mk_term_here <| Abs (pat, match_elaborated)
            (match_cost, function_term)
            
    | Abs (patterns, expr) -> // lambdas
        let expr_elaborated = elab_term_node expr
        patterns |> List.iter check_pattern;
        let lambda_elaborated = mk_term_here <| Abs (patterns, expr_elaborated)
        (0, lambda_elaborated)
    
    | Ascribed (expr1, expr2, None) -> (* [expr1 <: expr2] *)
        let expr1_cost, expr1_elaborated = elab_term_branch expr1
        let ascribed_elaborated = mk_term_here <| Ascribed (expr1_elaborated, expr2, None)
        (expr1_cost, ascribed_elaborated)
        
    | Op (op_name, args:list<term>) ->
        (* Operators. 
           [ Op ( "+", [x;y] ) ] 
           = [x + y] *)
        let args_costs, args_elaborated = 
            List.map elab_term_branch args 
                |>  List.unzip
        let op_term = mk_term_here <| Op (op_name, args_elaborated)
        let sum_args_cost = List.sum args_costs
        let num_args = List.length args
        let op_term_cost = num_args + sum_args_cost
        (op_term_cost, op_term)
    
    | App (expr1, expr2, imp) -> (* Application, eg. [expr1 (expr2)] *)
        let expr1_cost, expr1_elaborated = elab_term_branch expr1
        let expr2_cost, expr2_elaborated = elab_term_branch expr2
        let app_term = 
            mk_term_here <| App (expr1_elaborated, expr2_elaborated, imp)
        let app_term_cost = 1 + expr1_cost + expr2_cost
        (app_term_cost, app_term)
    
    | Construct (ctor_name : lid, ctor_args:list< term * imp >) ->
        (* Constructors. 
           [ Construct ( "Some", [x, Nothing] ) ] 
           = [Some x] *)
        let (ctor_args_terms, ctor_args_imps) : (list<term> * list<imp>) =
            List.unzip ctor_args
        let ctor_args_costs, ctor_args_terms_elaborated = 
            List.map elab_term_branch ctor_args_terms  
                |>  List.unzip
        let ctor_args_elaborated : list< term * imp > = 
            List.zip ctor_args_terms_elaborated 
                ctor_args_imps 
        let construct_term = 
            mk_term_here <| Construct (ctor_name, ctor_args_elaborated)
        let sum_ctor_args_cost = List.sum ctor_args_costs
        let num_ctor_args = List.length ctor_args
        let construct_term_cost = sum_ctor_args_cost + num_ctor_args
        (construct_term_cost, construct_term)
    
    | Seq (expr1, expr2) -> (* Sequenced expression, eg. [expr1; expr2] *)
        let expr1_cost, expr1_elaborated = elab_term_branch expr1
        let expr2_cost, expr2_elaborated = elab_term_branch expr2
        let seq_tm = mk_term_here <| Seq (expr1_elaborated, expr2_elaborated)
        let seq_tm_cost = expr1_cost + expr2_cost
        (seq_tm_cost, seq_tm)
    
    | Bind (patn, expr1, expr2) -> (* Bind patterns, eg. [patn <-- expr1; expr2] *)
        let expr1_cost, expr1_elaborated = elab_term_branch expr1
        let expr2_cost, expr2_elaborated = elab_term_branch expr2
        let bind_term = mk_term_here <| Bind (patn, expr1_elaborated, expr2_elaborated)
        let bind_term_cost = 1 + expr1_cost + expr2_cost
        (bind_term_cost, bind_term)
    
    | Paren expr -> (* Parenthesized expression, ie. [(expr)] *)
        let expr_cost, expr_elaborated = elab_term_branch expr
        let paren_term = mk_term_here <| Paren expr_elaborated
        (expr_cost, paren_term)

    | Match (e1, branches) -> (* match e1 with | branches [0] | branches [1] ... | branches [last] *)
        let e1_cost, e1_elaborated = elab_term_branch e1
        let (branches_patterns : list<pattern>), // the match cases
            (branches_when : list<( option<term> )>), // optional when clause, currently not enabled
            (branches_terms : list<term>) = // the term in each branch
                List.unzip3 branches
        
        let failOnWhenClause (whenClause : option<term>) = // fail on when clauses
            match whenClause with 
            | Some when_term -> failwith "Error: when clauses are not implemented yet."
            | None -> ()  
        branches_when |> List.iter failOnWhenClause ;
        
        let branches_patterns_costs = List.map pat_cost branches_patterns
        let sum_branches_patterns_costs = List.sum branches_patterns_costs
        let (branches_terms_costs : list<int>),
            (branches_terms_elaborated : list<term>) = 
                 List.map elab_term_branch branches_terms
                    |>  List.unzip  
        let max_branch_cost = List.max branches_terms_costs
        let match_cost = e1_cost + sum_branches_patterns_costs + max_branch_cost
        let branches_elaborated : list<branch> = 
            List.zip3 branches_patterns 
                      branches_when 
                      branches_terms_elaborated
        let match_term = mk_term_here <| Match (e1_elaborated, branches_elaborated)
        (match_cost, match_term)

    | Let (qualifier, pattern_term_pairs, expr1) -> (* let [patterns] = [terms] in expr1 *)
        match pattern_term_pairs with
        | [({pat=PatApp _ }, _)] -> (0, elab_term_node branch) // functions declared with let must have annotated cost
        | _ -> 
            let patterns, terms = List.unzip pattern_term_pairs
            let pat_costs = patterns |> List.map pat_cost
            let term_costs, elaborated_terms = terms |> List.map elab_term_branch
                                                     |> List.unzip
            //let sum_pat_costs  = List.sum pat_costs
            let sum_term_costs = List.sum term_costs
            let expr1_cost, expr1_elaborated = elab_term_branch expr1
            let pattern_term_pairs_elaborated = List.zip patterns 
                                                         elaborated_terms
            let let_term = mk_term_here <| Let ( qualifier,
                                                 pattern_term_pairs_elaborated,
                                                 expr1_elaborated )
            let let_cost = sum_term_costs + expr1_cost
            (let_cost, let_term)
            
    | Record (e1 : option<term>, fields: list<lid * term>) ->
        (* [ Record ( Some e1, [(lid2,e2); (lid3,e3); ...; (lidn,en)] ) ]
            = [ { e1 with lid2=e2; lid3=e3; ...; lidn=en } ],
           [ Record ( None, [(lid2,e2); (lid3,e3); ...; (lidn,en)] ) ]
            = [ { lid2=e2; lid3=e3; ...; lidn=en } ] *)
        
        let num_fields = List.length fields
        let (field_names, field_terms) : (list<lid> * list<term>) = 
            List.unzip fields      
        let (fields_costs, fields_elaborated) : (list<int> * list<term>) = 
            List.map elab_term_branch field_terms 
            |> List.unzip           
        let fields_cost = List.sum fields_costs
        let fields_elaborated = (field_names, fields_elaborated) ||> List.zip
        let (e1_cost, e1_elaborated) : (option<int> * option<term>) = 
            match Option.map elab_term_branch e1 with
            | Some (e1_cost, e1_elaborated) -> (Some e1_cost, Some e1_elaborated)
            | None -> (None, None)
        let record_cost = 
            match e1_cost with
            | None -> num_fields + fields_cost
            | Some e1_cost -> num_fields + e1_cost + fields_cost
        let record_term = mk_term_here <| Record (e1_elaborated, fields_elaborated)
        (record_cost, record_term)
        
    | If (cond_branch, then_branch, else_branch) ->
        let cond_branch_cost, cond_branch_elabd = elab_term_branch cond_branch
        let then_branch_cost, then_branch_elabd = elab_term_branch then_branch
        let else_branch_cost, else_branch_elabd = elab_term_branch else_branch
        
        let if_term =
            If (cond_branch_elabd, then_branch_elabd, else_branch_elabd)
            |> mk_term_here
        let max_branch_cost = max then_branch_cost 
                                  else_branch_cost
        let if_term_cost = 3 + cond_branch_cost + max_branch_cost
        (if_term_cost, if_term)
        
    | LetOpen (module_name, expr) -> (* [let open module_name in expr] *)
        let expr_elaborated = elab_term_node expr
        let letopen_tm = mk_term_here <| LetOpen (module_name, expr_elaborated)
        (0, letopen_tm)
    // try..with block; not currently permitted
    | TryWith _ -> failwith "try..with blocks are not currently permitted"
    | _ -> failwith ("unrecognised term" + branch.ToString() + ": please file a bug report")

(* Attaches an increment to tm. tm => inc tm n *)
and elab_term_node ({tm=tm; range=range; level=level} as node) =
    match tm with
    | Wild | Const _ | Tvar _ | Uvar _ | Var _ | Name _ ->
        node
    | App _ | Op _ | Construct _ | Seq _ | Bind _ | Paren _ | Match _ 
    | Record _ | Projector _ | Project _ | Abs _ | If _ | Ascribed _ 
    | LetOpen _ ->
        let branch_cost, branch_elaborated = elab_term_branch node
        mk_inc branch_elaborated branch_cost
    
    | Let (qualifier, pattern_term_pairs, expr1) ->
        let patterns, terms = List.unzip pattern_term_pairs
        //let pattern_costs = patterns |> List.map pat_cost
        let _ = patterns |> List.map check_pattern
        let term_costs, elaborated_terms = terms |> List.map elab_term_branch
                                                 |> List.unzip
        let sum_term_costs = List.sum term_costs
        let expr1_cost, expr1_elaborated = elab_term_branch expr1
        let elaborated_pattern_term_pairs = List.zip patterns 
                                                     elaborated_terms
        let elaborated_let_term' = Let (qualifier, elaborated_pattern_term_pairs, expr1_elaborated)
        let elaborated_let_term = mk_term elaborated_let_term' range level
        mk_inc elaborated_let_term (sum_term_costs + expr1_cost)
        
    // try..with block; not currently permitted
    | TryWith _ -> failwith "try..with blocks are not currently permitted"
    | _ -> failwith ("unrecognised term " + node.ToString() + ": please file a bug report")

let check_tycon tycon =
    match tycon with
    | TyconAbstract (ident, _, _)
    | TyconAbbrev   (ident, _, _, _)
    | TyconRecord   (ident, _, _, _)
    | TyconVariant  (ident, _, _, _) -> check_ident ident;
    
//elaborates a tll
let elab_tll (qual,ls_pat_tms) = (qual, ls_pat_tms |> List.map(fun (p,t) -> check_pattern p; (p,elab_term_node t)))

//elaborates a decl
let elab_decl decl =
    { decl with d = ( match decl.d with
                        | Main tm -> Main (elab_term_node tm)
                        | TopLevelLet (q,p) -> TopLevelLet (elab_tll (q,p))
                        | Tycon (is_effect, ls_tycons_optfsdocs) ->
                            if is_effect then failwith "effect declarations are not currently permitted"
                            ls_tycons_optfsdocs |> List.iter(fun (tyc,_) -> check_tycon tyc);
                            decl.d
                        | Exception _ -> failwith "exceptions are not currently permitted"
                        | NewEffect _ -> failwith "effect declarations are not currently permitted"
                        | SubEffect _ -> failwith "effect declarations are not currently permitted"
                        | Assume _ -> failwith "assumes are not currently permitted"
                        | Pragma _ -> failwith "pragmas are not currently permitted"
                        | TopLevelModule _ | Open _ | Include _ | ModuleAbbrev _ | Val _ | Fsdoc _ -> decl.d
                    )
    }

let elab_decls decls = decls |> List.map elab_decl

let main_decl_val range = (* [val mainFunction : Zen.Types.mainFunction] *)
    let main_ident = id_of_text "mainFunction"
    let main_decl_val_term' = Var <| qual_ns_str "Zen.Types" "mainFunction"
    let main_decl_val_term = mk_term main_decl_val_term' range Type_level
    let main_decl'_val = Val (main_ident, main_decl_val_term)
    { d=main_decl'_val; drange=range; doc=None; quals=[]; attrs=[] }

let main_decl_let range = (* [let mainFunction = MainFunc (CostFunc cf) main] *)     
    let main_ident = id_of_text "main"
    let main_term' = Var <| lid_of_ns_and_id [] main_ident
    let main_term = mk_term main_term' range Expr
    
    let cf_ident = id_of_text "cf"
    let cf_term' = Var <| lid_of_ns_and_id [] cf_ident
    let cf_term = mk_term cf_term' range Expr
    
    let costFunc_term' = Construct (qual_ns_str "Zen.Types" "CostFunc", [cf_term, Nothing])
    let costFunc_term = mk_term costFunc_term' range Expr
    
    let mainFunc_term' = 
        Construct ( qual_ns_str "Zen.Types" "MainFunc",
                    [ (paren costFunc_term, Nothing); (main_term, Nothing) ] )
    let mainFunc_term = mk_term mainFunc_term' range Expr
    
    let main_decl'_let = TopLevelLet ( NoLetQualifier, 
                                       [ { pat=PatVar (id_of_text "mainFunction", None);
                                           prange=range }, 
                                           mainFunc_term ] )
      
    { d=main_decl'_let; drange=range; doc=None; quals=[]; attrs=[] }

let main_decl range = [ main_decl_val range; main_decl_let range ]

let elab_module m =
    match m with
    | Module (lid,decls) -> 
        let decls = elab_decls decls @ main_decl FStar.Range.dummyRange 
        Module (lid, decls)
    | Interface (lid, decls, bool) -> 
        let decls = elab_decls decls @ main_decl FStar.Range.dummyRange
        Interface (lid, decls, bool)

let elab_ast (ast as module_, comments) =
        (elab_module module_, comments)

open System.Text
open System.IO
open FStar.Pprint
open FStar.Parser.ToDocument

let ast_to_string ast =
    let modules, _comments = ast
    let modul = List.head modules
    let doc, comments = modul_with_comments_to_document modul _comments
    let sb = new StringBuilder();
    let sw = new StringWriter(sb);
    pretty_out_channel 1.0 100 doc sw
    sw.Flush();
    sw.Close();
    sb.ToString()
    
(*
let getCost_tm tm = match tm.tm with
                    |
let getCostDecls decls =
  let mains = decls |> List.filter isDeclMain
  if List.length mains <> 1 then failwith "Only 1 main function is permitted."
  else let mainDeclVal = List.head mains
       let mainVal_tm = match mainDeclVal with
                        | Val (_, tm) -> tm
                        | _ -> failwith "Impossible: Please file a bug report."
       let cost = getCost_tm mainVal_tm
       cost



let getCostModule m =
  match m with
  | Module (lid, decls) -> Module (lid, getCostDecls decls)
  | Interface (lid, decls, bool) -> failwith "cannot submit an interface."
*)
