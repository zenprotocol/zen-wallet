module ASTUtils

open System
open FStar.Parser.AST
open FStar.Ident
open FStar.Const

module S = FSharpx.String

type AST = modul list * (string * FStar.Range.range) list

(* parenthesises a term *)
let paren tm = mk_term (Paren tm) tm.range tm.level

(* constructs a term by applying constructor at x, at the range and level of tm. 
   eg. mk_term_at Paren (tm1:term) tm1 => (tm1) *)
let mk_tm_at (constructor: 'a -> term') (x:'a) (tm:term) : term =
    mk_term (constructor x) tm.range tm.level

(* converts an integer n to an unsigned integer literal at the level of the second argument. *)
let mk_int_at (n:int) : term -> term = 
    mk_tm_at Const (Const_int (n.ToString(), None))

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
let rec unparen = function
  | {tm=Paren t} -> unparen t
  | t -> t

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

(* returns a tuple of the elaborated branch, and the cost of the branch *)
let rec elab_term
    ({tm=tm; range=range; level=level} as branch)
    : term * int = 
    
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
        -> (branch, 0)
    | Projector (tm',lid) -> // terms like Cons?.hd, NOT THE SAME AS "Project"
        (branch, 1)   
    | Project (tm,lid) -> // terms like tm.lid
        let tm_elabed, tm_cost = elab_term tm
        let project = mk_term_here <| Project (tm_elabed, lid)
        (project, 1 + tm_cost)
    
    | Abs ( ( [ { pat=PatVar (x, typ_opt) } ] as pat),
            ( { tm=Match (maybe_x, branches) } as match_term ) )
        when matches_var maybe_x x ->
        (* Special case of [fun x -> match x with ...], 
           To be handled as [function | ...].
           F* uses this representation for [function]. 
           Should really use a unique constructor... *)
            let match_elaborated, match_cost = elab_term match_term
            let function_term = mk_term_here <| Abs (pat, match_elaborated)
            (function_term, match_cost)
            
    | Abs (patterns, expr) -> // lambdas
        let expr_elaborated = elab_term expr ||> mk_inc
        patterns |> List.iter check_pattern;
        let lambda_elaborated = mk_term_here <| Abs (patterns, expr_elaborated)
        (lambda_elaborated, 0)
    
    | Ascribed (expr1, expr2, None) -> (* [expr1 <: expr2] *)
        let expr1_elaborated, expr1_cost = elab_term expr1
        let ascribed_elaborated = mk_term_here <| Ascribed (expr1_elaborated, expr2, None)
        (ascribed_elaborated, expr1_cost)
        
    | Op (op_name, args:list<term>) ->
        (* Operators. 
           [ Op ( "+", [x;y] ) ] 
           = [x + y] *)
        let args_elaborated, args_costs = 
            List.map elab_term args 
                |>  List.unzip
        let op_term = mk_term_here <| Op (op_name, args_elaborated)
        let sum_args_cost = List.sum args_costs
        let num_args = List.length args
        let op_term_cost = num_args + sum_args_cost
        (op_term, op_term_cost)
    
    | App (expr1, expr2, imp) -> (* Application, eg. [expr1 (expr2)] *)
        let expr1_elaborated, expr1_cost = elab_term expr1
        let expr2_elaborated, expr2_cost = elab_term expr2
        let app_term = 
            mk_term_here <| App (expr1_elaborated, expr2_elaborated, imp)
        let app_term_cost = 1 + expr1_cost + expr2_cost
        (app_term, app_term_cost)
    
    | Construct (ctor_name : lid, ctor_args:list< term * imp >) ->
        (* Constructors. 
           [ Construct ( "Some", [x, Nothing] ) ] 
           = [Some x] *)
        let (ctor_args_terms, ctor_args_imps) : (list<term> * list<imp>) =
            List.unzip ctor_args
        let ctor_args_terms_elaborated, ctor_args_costs = 
            List.map elab_term ctor_args_terms  
                |>  List.unzip
        let ctor_args_elaborated : list< term * imp > = 
            List.zip ctor_args_terms_elaborated 
                ctor_args_imps 
        let construct_term = 
            mk_term_here <| Construct (ctor_name, ctor_args_elaborated)
        let sum_ctor_args_cost = List.sum ctor_args_costs
        let num_ctor_args = List.length ctor_args
        let construct_term_cost = sum_ctor_args_cost + num_ctor_args
        (construct_term, construct_term_cost)
    
    | Seq (expr1, expr2) -> (* Sequenced expression, eg. [expr1; expr2] *)
        let expr1_elaborated, expr1_cost = elab_term expr1
        let expr2_elaborated, expr2_cost = elab_term expr2
        let seq_tm = mk_term_here <| Seq (expr1_elaborated, expr2_elaborated)
        let seq_tm_cost = expr1_cost + expr2_cost
        (seq_tm, seq_tm_cost)
    
    | Bind (patn, expr1, expr2) -> (* Bind patterns, eg. [patn <-- expr1; expr2] *)
        let expr1_elaborated, expr1_cost = elab_term expr1
        let expr2_elaborated, expr2_cost = elab_term expr2
        let bind_term = mk_term_here <| Bind (patn, expr1_elaborated, expr2_elaborated)
        let bind_term_cost = 1 + expr1_cost + expr2_cost
        (bind_term, bind_term_cost)
    
    | Paren expr -> (* Parenthesized expression, ie. [(expr)] *)
        let expr_elaborated, expr_cost = elab_term expr
        let paren_term = mk_term_here <| Paren expr_elaborated
        (paren_term, expr_cost)

    | Match (e1, branches) -> (* match e1 with | branches [0] | branches [1] ... | branches [last] *)
        let e1_elaborated, e1_cost = elab_term e1
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
        let (branches_terms_elaborated : list<term>),
            (branches_terms_costs : list<int>) = 
                 List.map elab_term branches_terms
                    |>  List.unzip  
        let max_branch_cost = List.max branches_terms_costs
        let match_cost = e1_cost + sum_branches_patterns_costs + max_branch_cost
        let branches_elaborated : list<branch> = 
            List.zip3 branches_patterns 
                      branches_when 
                      branches_terms_elaborated
        let match_term = mk_term_here <| Match (e1_elaborated, branches_elaborated)
        (match_term, match_cost)

    | Let (qualifier, pattern_term_pairs, expr1) -> (* let [patterns] = [terms] in expr1 *)
        let elab_pat_term_pair (pat, term) =
            let term_elaborated, term_cost = elab_term term
            match pat with
            | { pat=PatApp _ } -> // functions must have annotated cost
                let inc_term_elaborated = mk_inc term_elaborated term_cost
                ((pat, inc_term_elaborated), 0) 
            | _ -> ((pat, term_elaborated), term_cost)
        
        let pattern_term_pairs_elaborated, term_costs = 
            pattern_term_pairs |> List.map elab_pat_term_pair
                               |> List.unzip
        let sum_term_costs = List.sum term_costs
        let expr1_elaborated, expr1_cost = elab_term expr1
        let let_term = mk_term_here <| Let ( qualifier,
                                             pattern_term_pairs_elaborated,
                                             expr1_elaborated )
        let let_cost = sum_term_costs + expr1_cost
        (let_term, let_cost)
            
    | Record (e1 : option<term>, fields: list<lid * term>) ->
        (* [ Record ( Some e1, [(lid2,e2); (lid3,e3); ...; (lidn,en)] ) ]
            = [ { e1 with lid2=e2; lid3=e3; ...; lidn=en } ],
           [ Record ( None, [(lid2,e2); (lid3,e3); ...; (lidn,en)] ) ]
            = [ { lid2=e2; lid3=e3; ...; lidn=en } ] *)
        
        let num_fields = List.length fields
        let (field_names, field_terms) : (list<lid> * list<term>) = 
            List.unzip fields      
        let (fields_elaborated, fields_costs) : (list<term> * list<int>) = 
            List.map elab_term field_terms 
            |> List.unzip           
        let fields_cost = List.sum fields_costs
        let fields_elaborated = (field_names, fields_elaborated) ||> List.zip
        let (e1_elaborated, e1_cost) : (option<term> * option<int>) = 
            match Option.map elab_term e1 with
            | Some (e1_cost, e1_elaborated) -> (Some e1_cost, Some e1_elaborated)
            | None -> (None, None)
        let record_cost =
            match e1_cost with
            | None -> num_fields + fields_cost
            | Some e1_cost -> num_fields + e1_cost + fields_cost
        let record_term = mk_term_here <| Record (e1_elaborated, fields_elaborated)
        (record_term, record_cost)
        
    | If (cond_branch, then_branch, else_branch) ->
        let cond_branch_elabd, cond_branch_cost = elab_term cond_branch
        let then_branch_elabd, then_branch_cost = elab_term then_branch
        let else_branch_elabd, else_branch_cost = elab_term else_branch
        
        let if_term =
            If (cond_branch_elabd, then_branch_elabd, else_branch_elabd)
            |> mk_term_here
        let max_branch_cost = max then_branch_cost 
                                  else_branch_cost
        let if_term_cost = 3 + cond_branch_cost + max_branch_cost
        (if_term, if_term_cost)
        
    | LetOpen (module_name, expr) -> (* [let open module_name in expr] *)
        let expr_elaborated = elab_term expr ||> mk_inc
        let letopen_tm = mk_term_here <| LetOpen (module_name, expr_elaborated)
        (letopen_tm, 0)
    // try..with block; not currently permitted
    | TryWith _ -> failwith "try..with blocks are not currently permitted"
    | _ -> failwith ("unrecognised term" + branch.ToString() + ": please file a bug report")

(* Attaches an increment to tm. tm => inc tm n *)
let elab_term_node node =
    elab_term node ||> mk_inc

let check_tycon tycon =
    match tycon with
    | TyconAbstract (ident, _, _)
    | TyconAbbrev   (ident, _, _, _)
    | TyconRecord   (ident, _, _, _)
    | TyconVariant  (ident, _, _, _) -> check_ident ident;

let is_lemma_lid : lid -> bool = function
    | { ns=[]; ident={idText="Lemma"}; nsstr=""; str="Lemma" } -> true
    | _ -> false

let rec is_lemma_type : term' -> bool = function
    | Construct(lid, _) -> is_lemma_lid lid
    | Product(binders, tm) -> is_lemma_type tm.tm
    | _ -> false

let is_lemma_val : decl -> bool = function
    | { d=Val(_, {tm=signature_tm}) } -> is_lemma_type signature_tm
    | _ -> false 

let is_lemma_pat : pattern' -> bool = function
    | PatAscribed(_, {tm=ascription_type}) -> is_lemma_type ascription_type
    | _ -> false
    
let is_lemma_tll : decl -> bool = function
    | { d=TopLevelLet(_, [{pat=tll_patn}, _]) } -> // TODO: Should we allow recursion-recursion here?
            is_lemma_pat tll_patn
    | _ -> false    

    
//elaborates a tll
let elab_tll (qual,ls_pat_tms) = (qual, ls_pat_tms |> List.map(fun (p,t) -> check_pattern p; (p,elab_term_node t)))

//elaborates a decl
let elab_decl ({d=d} as decl) =
    let d_elaborated = 
        match d with
        | Main tm -> Main (elab_term_node tm)
        | TopLevelLet (q,p) -> 
            if is_lemma_tll decl then d else // Do not elaborate lemmas
            TopLevelLet (elab_tll (q,p))
        | Tycon (is_effect, ls_tycons_optfsdocs) ->
            if is_effect then failwith "effect declarations are not currently permitted"
            ls_tycons_optfsdocs |> List.iter(fun (tyc,_) -> check_tycon tyc);
            d
        | Exception _ -> failwith "exceptions are not currently permitted"
        | NewEffect _ -> failwith "effect declarations are not currently permitted"
        | SubEffect _ -> failwith "effect declarations are not currently permitted"
        | Assume _ -> failwith "assumes are not currently permitted"
        | Pragma _ -> failwith "pragmas are not currently permitted"
        | TopLevelModule _ | Open _ | Include _ | ModuleAbbrev _ | Val _ | Fsdoc _ -> d
    
    { decl with d = d_elaborated }

let name_of_decl : decl -> string = function
    | { d=Val(i, _) } -> i.idText
    | { d=TopLevelLet(_, pat_tm_pairs) } ->
        lids_of_let pat_tm_pairs 
        |> List.map (fun l -> l.str) 
        |> String.concat ", "
    | _ -> failwith "Please file a bug report: name_of_decl failed."

let rec elab_decls : list<decl> -> list<decl> = function
    | [] -> []
    | [d] -> [elab_decl d]
    | fst::snd::tl ->
        match snd.d with
        | TopLevelLet _ ->
            (* If we have a val and a top-level-let with the same name, 
               and the val is a lemma, do not elaborate the top-level-let. *)
            if is_lemma_val fst &&
               name_of_decl fst = name_of_decl snd
            then fst::snd::elab_decls tl
            else elab_decl fst :: elab_decls (snd::tl)
        | _ -> elab_decl fst :: elab_decls (snd::tl)
     
                        
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

let main_decl' range = [ main_decl_val range; main_decl_let range ]
let main_decls = main_decl' FStar.Range.dummyRange

let elab_module m =
    match m with
    | Module (lid,decls) -> 
        let decls = elab_decls decls
        Module (lid, decls)
    | Interface (lid, decls, bool) -> 
        let decls = elab_decls decls
        Interface (lid, decls, bool)

let add_main_decl : modul -> modul = function
    | Module (lid, decls) -> 
        Module (lid, decls @ main_decls)
    | Interface (lid, decls, bool) -> 
        Interface (lid, decls @ main_decls, bool)

let elab_ast (ast as module_, comments) =
    (elab_module module_, comments)

let add_main_to_ast (ast as module_, comments) =
    (add_main_decl module_, comments)

module PP = FStar.Pprint
module TD = FStar.Parser.ToDocument

let ast_to_string ast =
    let modules, _comments = ast
    let modul = List.head modules
    let doc, comments = TD.modul_with_comments_to_document modul _comments
    let sb = new Text.StringBuilder();
    let sw = new IO.StringWriter(sb);
    PP.pretty_out_channel 1.0 100 doc sw
    sw.Flush();
    sw.Close();
    sb.ToString()
