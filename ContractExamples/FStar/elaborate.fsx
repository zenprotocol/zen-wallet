#I @"../../tools/FStar/mono"
//#r "FSharp.PowerPack.dll"
//#r "FSharp.PowerPack.Compatibility.dll"
#r "FsLexYacc.Runtime.dll"
#r "basic.dll"
#r "syntax.dll"
#r "tosyntax.dll"
#r "parser.dll"
#r "prettyprint.dll"
#r "FSharp.PPrint.dll"
#r "../../FStar-Utils/bin/Debug/FStar_Utils.dll"

module ToDoc = FStar.Parser.ToDocument

module Pp = FStar.Pprint

let inputFileName = fsi.CommandLineArgs.[1]
let outputFileName = fsi.CommandLineArgs.[2]


let ast = FStar.Parser.Driver.parse_file inputFileName
IOUtils.write_ast_to_file (ASTUtils.elab_ast ast) outputFileName


;
