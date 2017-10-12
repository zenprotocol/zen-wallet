module Verify

open System.IO
open System.Diagnostics
open ASTUtils

type hints_mode =
    | Record
    | Use

(* verifies an AST
// --in is complicated, this doesn't work yet.
let verify_ast ast hints_mode working_directory =
    let ast_str = ast_to_string ast
    // build the F* process
    let fstar_process = new Process();
    fstar_process.StartInfo.FileName <- "../FStar/bin/fstar.exe";
    fstar_process.StartInfo.WorkingDirectory <- working_directory
    fstar_process.StartInfo.RedirectStandardOutput <- true
    fstar_process.StartInfo.UseShellExecute <- false

    // get number of logical cores on the system
    let num_cores = System.Environment.ProcessorCount
    fstar_process.StartInfo.Arguments <- 
        "--in " 
        + "\""+ast_str+"\" " + 
        begin match hints_mode with
        | Record -> "--record_hints "
        | Use -> "--use_hints "
        end
        // invoke z3 using several cores
        + "--n_cores "+ num_cores.ToString() + ""
        ;
    let _ = fstar_process.Start();

    printfn "%A" (fstar_process.StandardOutput.ReadToEnd());
    if fstar_process.ExitCode <> 0 then failwith "Verification failed"
    printfn "done\n"
*)


(* verifies a file *)
let verify_file filepath working_directory (rlimit:int) (mmcount:int) (mmsize:int) =
    // build the F* process
    
    let fstar_process = new Process();
    fstar_process.StartInfo.FileName <- "fstar.exe";
    fstar_process.StartInfo.WorkingDirectory <- working_directory
    fstar_process.StartInfo.RedirectStandardOutput <- true
    fstar_process.StartInfo.UseShellExecute <- false

    let hints_exist = File.Exists (filepath+".hints")
    
    fstar_process.StartInfo.Arguments <- 
        "\""+filepath+"\"" 
        + 
        if hints_exist 
            then 
                 // get number of logical cores on the system
                let num_cores = System.Environment.ProcessorCount
                printfn "Found %d cores" num_cores;
                " --use_hints"
                // invoke z3 using several cores
                // currently gives strange performance issues; https://github.com/FStarLang/FStar/issues/146
                //+ " --z3rlimit " + rlimit.ToString()
                + " --n_cores "+ num_cores.ToString()
                + " --z3cliopt rlimit=" + rlimit.ToString()
                + " --z3cliopt memory_max_alloc_count=" + mmcount.ToString()
                + " --z3cliopt memory_max_size=" + mmsize.ToString()
        else "--record_hints "
        //+ "--timing " //does nothing?

    
    let _ = fstar_process.Start();
    printfn "%A" (fstar_process.StandardOutput.ReadToEnd());
    printfn "done\n"
    if fstar_process.ExitCode <> 0 then failwith "Verification failed"
    ();

(* generates F# code without verifying *)
let codegen_lax filepath working_directory output_path =
    let fstar_process = new Process();
    fstar_process.StartInfo.FileName <- "fstar.exe";
    fstar_process.StartInfo.WorkingDirectory <- working_directory
    fstar_process.StartInfo.RedirectStandardOutput <- true
    fstar_process.StartInfo.UseShellExecute <- false
    fstar_process.StartInfo.Arguments <- 
        "--extract_module \""+filepath+"\" "
        + "--lax " 
        + "--codegen FSharp" 
        + " --odir " + "\""+output_path+"\" "
        ;
    let _ = fstar_process.Start();

    printfn "%A" (fstar_process.StandardOutput.ReadToEnd());
    printfn "done\n"
    if fstar_process.ExitCode <> 0 then failwith "Extraction failed"
    ();

(** For some reason FStar.Parser.Interleave isn't found in the binary, leading to this breaking the build. 
    Doesn't seem to be critical, so this can be fixed later.

open FStar
open FStar.Errors

module DsEnv   = FStar.ToSyntax.Env
module Desugar = FStar.ToSyntax.ToSyntax
module SMT     = FStar.SMTEncoding.Solver
module Syntax  = FStar.Syntax.Syntax
module Tc      = FStar.TypeChecker.Tc
module TcEnv   = FStar.TypeChecker.Env

(* Parse and desugar an ast *)
let parse (pre_ast': option<ASTUtils.AST>) (ast': ASTUtils.AST) (env:DsEnv.env)
    : DsEnv.env * list<Syntax.modul> =
    let ast_moduls', _ = ast'
    let ast_moduls = 
        match pre_ast' with
        | None ->
            ast_moduls'
        | Some pre_ast ->
            let pre_ast_moduls, _ = pre_ast
            match pre_ast_moduls, ast_moduls' with
            | [ Parser.AST.Interface (lid1, decls1, _) ], [ Parser.AST.Module (lid2, decls2) ]
              when Ident.lid_equals lid1 lid2 ->
                [ Parser.AST.Module (lid1, FStar.Parser.Interleave.interleave decls1 decls2) ]
            | _ ->
                raise (Err ("mismatch between pre-module and module\n"))
    
    Desugar.desugar_file env ast_moduls

**)