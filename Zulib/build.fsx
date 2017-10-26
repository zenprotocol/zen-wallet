
//#r @"packages/System.Reflection.Metadata/lib/portable-net45+win8/System.Reflection.Metadata.dll"
#r @"packages/System.Reflection.Metadata/lib/netstandard1.1/System.Reflection.Metadata.dll"
#r @"packages/FAKE/tools/FakeLib.dll"
#r @"packages/Zen.FSharp.Compiler.Service/lib/net45/Zen.FSharp.Compiler.Service.dll"

open Fake
open System.IO
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler

module Array = Microsoft.FSharp.Collections.Array

let extractedDir = "fsharp/Extracted"
let binDir = "bin"

let (++) = Array.append

let getFiles pattern =
  FileSystemHelper.directoryInfo  FileSystemHelper.currentDirectory
  |> FileSystemHelper.filesInDirMatching pattern
  |> Array.map (fun file -> file.FullName)

let zulibFiles = getFiles "fstar/*.fst" ++ getFiles "fstar/*.fsti"

let runFStar args files =

  let join = Array.reduce (fun a b -> a + " " + b)

  let primsFile = FileSystemHelper.currentDirectory + "/fstar/prims.fst"

  let executable,fstarPath,z3Path =
    if EnvironmentHelper.isLinux then ("mono", "../tools/fstar/mono/fstar.exe", "../tools/z3/linux/z3")
    elif EnvironmentHelper.isMacOS then ("mono", "../tools/fstar/mono/fstar.exe", "z3")
    else ("../tools/fstar/dotnet/fstar.exe","","../tools/z3/windows/z3.exe")

  let fstar = [|
    fstarPath;
    "--smt";z3Path;
    "--prims";primsFile;
    "--no_default_includes";
    "--include";"fstar/"; |]
  //printfn "%s" (join (fstar ++ args ++ zulibFiles));
  ProcessHelper.Shell.Exec (executable, join (fstar ++ args ++ files))

Target "Clean" (fun _ ->
  CleanDir extractedDir
  CleanDir binDir
)

Target "RecordHints" (fun _ ->
  let args =
    [| //"--z3refresh";
       //"--verify_all";
       "--record_hints" |]

  let exitCodes = Array.Parallel.map (fun file -> runFStar args [|file|]) zulibFiles
  if not (Array.forall (fun exitCode -> exitCode = 0) exitCodes)
    then failwith "recording Zulib hints failed"

)
Target "Verify" (fun _ ->
  let args =
    [| "--use_hints"; 
       "--use_hint_hashes" 
    |]

  let exitCodes = Array.Parallel.map (fun file -> runFStar args [|file|]) zulibFiles
  if not (Array.forall (fun exitCode -> exitCode = 0) exitCodes)
    then failwith "Verifying Zulib failed"
)


Target "Extract" (fun _ ->
  let cores = System.Environment.ProcessorCount
  let threads = cores * 2  
  
  let args =
    [|
       "--lax";
       //"--use_hints";
       //"--use_hint_hashes";
       "--codegen";"FSharp";
       "--extract_module";"Zen.Base";
       "--extract_module";"Zen.Error";
       "--extract_module";"Zen.ErrorT";
       "--extract_module";"Zen.Option";
       "--extract_module";"Zen.OptionT";
       "--extract_module";"Zen.Tuple";
       "--extract_module";"Zen.TupleT";
       "--extract_module";"Zen.Vector";
       "--extract_module";"Zen.Array.Extracted";
       "--extract_module";"Zen.Cost.Extracted";
       "--codegen-lib";"Zen.Cost";
       "--codegen-lib";"Zen.Array";
       "--extract_module";"Zen.Types.Extracted";
       "--codegen-lib";"Zen.Types";
       "--odir";extractedDir |]

  let exitCode = runFStar args zulibFiles

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
      "fsharp/Extracted/Zen.Error.fs";
      "fsharp/Extracted/Zen.Tuple.fs";
      "fsharp/Realized/Zen.Cost.Realized.fs";
      "fsharp/Extracted/Zen.Cost.Extracted.fs";
      "fsharp/Extracted/Zen.OptionT.fs";
      "fsharp/Extracted/Zen.ErrorT.fs";
      "fsharp/Extracted/Zen.TupleT.fs";
      "fsharp/Extracted/Zen.Vector.fs";
      "fsharp/Realized/Zen.Array.Realized.fs";
      "fsharp/Extracted/Zen.Array.Extracted.fs";
      "fsharp/Realized/Zen.Types.Realized.fs";
      "fsharp/Extracted/Zen.Types.Extracted.fs";
      "fsharp/Realized/Zen.Crypto.fs";
      "fsharp/Realized/Zen.Sha3.Realized.fs";
      "fsharp/Realized/Zen.Merkle.fs";
      "fsharp/Realized/Zen.Util.fs";
    |]

  let checker = FSharpChecker.Create()

  let compileParams =
    [|
      "fsc.exe" ; "-o"; "bin/Zulib.dll"; "-a";
      "-r"; "packages/FSharp.Compatibility.OCaml/lib/net40/FSharp.Compatibility.OCaml.dll"
      "-r"; "packages/libsodium-net/lib/Net40/Sodium.dll"
      "-r"; "packages/BouncyCastle/lib/BouncyCastle.Crypto.dll"
    |]

  let messages, exitCode =
    Async.RunSynchronously (checker.Compile (Array.append compileParams files))

  if exitCode <> 0 then
    let errors = Array.filter (fun (msg:FSharpErrorInfo) -> msg.Severity = FSharpErrorSeverity.Error) messages
    printfn "%A" errors
    failwith "building Zulib failed"
    )

Target "Default" ignore

"Clean"
  ==> "Verify"
  ==> "Extract"
  ==> "Build"
  ==> "Default"

RunTargetOrDefault "Default"
