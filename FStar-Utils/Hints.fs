module Hints

open FStar.Util

let opt_hint_exists opt_hint = 
    match opt_hint with
    | Some hint -> true
    | None -> false

// make sure filename is a .hints! 
let get_num_hints (filename:string) =
    match read_hints filename with
    | None -> None
    | Some hints_db ->
        hints_db.hints 
        |> List.takeWhile (fun opt_hint -> opt_hint_exists opt_hint) 
        |> List.length |> Some

