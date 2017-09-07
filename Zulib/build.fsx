#r @"packages/FAKE/tools/FakeLib.dll"
#r @"packages/Zen.FSharp.Compiler.Service/lib/net45/Zen.FSharp.Compiler.Service.dll"
#r @"packages/System.Reflection.Metadata/lib/portable-net45+win8/System.Reflection.Metadata.dll"


open Fake
open System.IO
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler
  
Target "Extract" (fun _ ->  

  let files =
    FileSystemHelper.directoryInfo  FileSystemHelper.currentDirectory
    |> FileSystemHelper.filesInDirMatching "fstar/*.fst*"
    |> Array.map (fun file -> file.FullName)

  let args = 
    [| "../../FStar/bin/fstar.exe"
       "--lax";"--codegen";"FSharp";
       "--prims";"fstar/prims.fst";
       "--extract_module";"Consensus.Types";
       "--extract_module";"Zen.Base";
       "--extract_module";"Zen.Option";
       "--extract_module";"Zen.OptionT";
       "--extract_module";"Zen.Tuple";
       "--extract_module";"Zen.TupleT";
       "--extract_module";"Zen.Vector";
       "--extract_module";"Zen.Array";
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
      "fsharp/Realized/Zen.Cost.fs";
      "fsharp/Extracted/Zen.OptionT.fs";
      "fsharp/Extracted/Zen.TupleT.fs";
      "fsharp/Extracted/Zen.Vector.fs";
      "fsharp/Realized/Zen.ArrayRealized.fs";
      "fsharp/Extracted/Zen.Array.fs";
      "fsharp/Realized/Zen.Crypto.fs"; 
      "fsharp/Realized/Consensus.Realized.fs";
      "fsharp/Extracted/Consensus.Types.fs" |]    

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
