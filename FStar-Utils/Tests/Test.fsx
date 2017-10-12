#I @"../../../FStar/bin"
//#r "FSharp.PowerPack.dll"
//#r "FSharp.PowerPack.Compatibility.dll"
#r "FsLexYacc.Runtime.dll"
#r "basic.dll"
#r "syntax.dll"
#r "tosyntax.dll"
#r "parser.dll"
#r "prettyprint.dll"
#r "FSharp.PPrint.dll"
#r "../bin/Debug/FStar_Utils.dll"

module ToDoc = FStar.Parser.ToDocument

module Pp = FStar.Pprint

let ast = FStar.Parser.Driver.parse_file "Test.fst"
System.IO.File.WriteAllText("Outputs/TestAST.txt", sprintf "%A" ast);
//IOUtils.write_ast_to_file (ASTUtils.elab_ast ast) "Outputs/Testoutput.fst"

(*
// Testing the pretty_printer
let (module_, comments) = ast
let (doc, comments') = 
    ToDoc.modul_with_comments_to_document module_ comments
let doc_string = Pp.pretty_string 1.0 100 doc
printfn "%s" doc_string
*)

(*
open FStar.Parser.AST
open FStar.Ident
open FStar.Range
open FStar.Const

let mk_modul (decls:list<decl>) : modul =
    let moduleNameIdent : ident = { idText="MinimalAST";
                                    idRange={ def_range=612490106594410498L;
                                              use_range=612490106594410498L } }
    let moduleNameLid : lid = { ns=[]; 
                                ident=moduleNameIdent;
                                nsstr="";
                                str="MinimalAST" }
    
    Module (moduleNameLid, decls)

let mk_topLevelLet (bindings: list< pattern * term >) : decl' =
    TopLevelLet(NoLetQualifier, bindings)

let mk_decl (bindings: list< pattern * term >) : decl =
    let drange : range = { def_range=720577589646770178L;
                           use_range=720577589646770178L }
    
    { d=mk_topLevelLet bindings;
      drange=drange;
      doc=None;
      quals=[];
      attrs=[] }

let mk_binding (tm:term) : pattern * term =
    let patIdent : ident = 
        { idText="pat1GoesHere";
          idRange = { def_range=576462405865881602L;
                      use_range=576462405865881602L } }
    let prange : range = 
        { def_range=576462405865881602L;
          use_range=576462405865881602L }
    let pat : pattern =
        { pat=PatVar (patIdent, None);
          prange=prange }
    pat, tm

let mk_tm (tm:term') : term =
    let range : range =
        { def_range=720577610047864834L;
          use_range=720577610047864834L }
    let level = Un
    
    { tm=tm;
      range=range;
      level=level }

let mk_ast (m:modul) 
    : modul * list< string * range > =
        m, []

let const_tm' : term' = 
    let range = { def_range=720577610097864834L;
                  use_range=720577610097864834L }    
    let ident1 : ident = 
        { idText="ident1";
          idRange = { def_range=720577610097864834L;
                      use_range=720577610097864834L } }
    let lid1 : lid = { ns=[]; 
                      ident=ident1;
                      nsstr="";
                      str="lident1" }
    let tm1 = mk_tm <| Const (Const_bool true)
    let tm2 = mk_tm <| Const (Const_bool false)
    
    Record <| (Some tm1, [lid1, tm2])
        
let ast =
    let tm = mk_tm const_tm'
    let binding = mk_binding tm
    let decl = mk_decl [binding]
    let modul = mk_modul [decl]
    mk_ast modul

IOUtils.write_ast_to_file ast "Outputs/ConstASTOutput.fst"
*)

(*
let ast = FStar.Parser.Driver.parse_file "MinimalAST.fst"
System.IO.File.WriteAllText("Outputs/MinimalAST.txt", sprintf "%A" ast);
IOUtils.write_ast_to_file (ASTUtils.elab_ast ast) "Outputs/MinimalOutput.fst"
*)