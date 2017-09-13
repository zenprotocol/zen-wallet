#r @"packages/FAKE/tools/FakeLib.dll"
#r @"packages/Zen.FSharp.Compiler.Service/lib/net45/Zen.FSharp.Compiler.Service.dll"

open Fake
open System.IO
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler

Target "Extract" (fun _ ->

  let getFiles pattern =
    FileSystemHelper.directoryInfo  FileSystemHelper.currentDirectory
    |> FileSystemHelper.filesInDirMatching pattern
    |> Array.map (fun file -> file.FullName)

  let files =
    Array.append (getFiles "fstar/*.fst") (getFiles "fstar/*.fsti")

  printfn "%A" files

  // we should check the OS have different path for each OS
  let z3path = "../tools/z3/z3"

  let args =
    [| "../tools/fstar/fstar.exe";
       //"--smt";z3path;
       "--lax";
       "--codegen";"FSharp";
       "--prims";"fstar/prims.fst"; // Set the prims file to use
       "--include";"fstar";         // Set the environment to Zulib
       "--extract_module";"Zen.Base";
       "--extract_module";"Zen.Option";
       "--extract_module";"Zen.Cost.Extracted";
       (** --codegen-lib allows user code to do `open Zen.Cost`,
           although in extracted code, references to `Zen.Cost` are resolved
           to `Zen.Cost.Extracted` or `Zen.Cost.Realized`.
           Do not attempt to extract `Zen.Cost`. **)
       "--codegen-lib";"Zen.Cost";
       //"--extract_module";"Zen.Cost";
       "--extract_module";"Zen.OptionT";
       "--extract_module";"Zen.Tuple";
       "--extract_module";"Zen.TupleT";
       "--extract_module";"Zen.Vector";
       "--extract_module";"Zen.Array.Extracted";
       "--codegen-lib";"Zen.Array";
       //"--extract_module";"Zen.Array";
       "--extract_module";"Zen.Types.Extracted";
       "--codegen-lib";"Zen.Types";
       //"--extract_module";"Zen.Types";
       "--odir";"fsharp/Extracted"; |]
       |> Array.append <| files
       |> Array.reduce (fun a b -> a + " " + b)

  let exitCode =
    ProcessHelper.Shell.Exec ("mono", args)

  if exitCode <> 0 then
    failwith "extracting Zulib failed"
)

Target "Build" (fun _ ->

  let files =
    [| "fsharp/Realized/prims.fs";
      "fsharp/Realized/FStar.Pervasives.fs";
      "fsharp/Realized/FStar.Mul.fs";
      "fsharp/Realized/FStar.UInt.fs";
      "fsharp/Realized/FStar.UInt8.fs";
      "fsharp/Realized/FStar.UInt32.fs";
      "fsharp/Realized/FStar.UInt64.fs";
      "fsharp/Realized/FStar.Int.fs";
      "fsharp/Realized/FStar.Int64.fs";
      "fsharp/Extracted/Zen.Base.fs";
      "fsharp/Extracted/Zen.Option.fs";
      "fsharp/Extracted/Zen.Tuple.fs";
      "fsharp/Realized/Zen.Cost.Realized.fs";
      "fsharp/Extracted/Zen.Cost.Extracted.fs";
      (** `Zen.Cost.fs` should not exist.
          It should be extracted with `--codegen-lib`, which generates no code.
          Do not use `--extract_module Zen.Cost`. **)
      //"fsharp/Extracted/Zen.Cost.fs";
      "fsharp/Extracted/Zen.OptionT.fs";
      "fsharp/Extracted/Zen.TupleT.fs";
      "fsharp/Extracted/Zen.Vector.fs";
      "fsharp/Realized/Zen.Array.Realized.fs";
      "fsharp/Extracted/Zen.Array.Extracted.fs";
      //"fsharp/Extracted/Zen.Array.fs";
      "fsharp/Realized/Zen.Crypto.fs";
      "fsharp/Realized/Zen.Types.Realized.fs";
      "fsharp/Extracted/Zen.Types.Extracted.fs";
      //"fsharp/Extracted/Zen.Types.fs"
    |]

  let checker = FSharpChecker.Create()

  let compileParams =
    [|
      "fsc.exe" ; "-o"; "bin/Zulib.dll"; "-a";
      "-r"; "packages/FSharp.Compatibility.OCaml/lib/net40/FSharp.Compatibility.OCaml.dll"
      "-r"; "packages/libsodium-net/lib/Net40/Sodium.dll"
    |]

  let messages, exitCode =
    Async.RunSynchronously (checker.Compile (Array.append compileParams files))

  if exitCode <> 0 then
    let errors = Array.filter (fun (msg:FSharpErrorInfo) -> msg.Severity = FSharpErrorSeverity.Error) messages
    printfn "%A" errors
    failwith "building Zulib failed"
    )

Target "Default" (fun _ -> ())

"Extract"
  ==> "Build"
  ==> "Default"

RunTargetOrDefault "Default"
