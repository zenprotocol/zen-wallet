(**
    This module defines functions useful for formatting ASTs for printing.
**)

module IOUtils

open System.IO
open System.Text

open FStar.Parser.ToDocument
open FStar.Pprint

open ASTUtils

let print_modul m = (m |> modul_to_document |> pretty_out_channel 1.0 100) <| stdout

let print_ast (ast as module_, comments) =
    let doc, comments = modul_with_comments_to_document module_ comments
    pretty_out_channel 1.0 100 doc stdout

let write_ast_to_file (ast as module_, comments) (filename:string) =
    let doc, comments = modul_with_comments_to_document module_ comments
    let swriter = new StreamWriter(filename,false);
    pretty_out_channel 1.0 100 doc swriter
    swriter.Flush();
    swriter.Close();
    ()

let elaborate input_filepath output_target =
    let ast = FStar.Parser.Driver.parse_file input_filepath
    let elab'd_ast = elab_ast ast
                     |> add_main_to_ast
    write_ast_to_file elab'd_ast output_target