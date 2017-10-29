// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler

[<EntryPoint>]
let main argv = 

    let checker = FSharpChecker.Create()

    let compileParams =
        [|
          "fsc.exe" 
          //; "-o"; "bin/Zulib.dll"; "-a";
          //"-r"; "packages/FSharp.Compatibility.OCaml/lib/net40/FSharp.Compatibility.OCaml.dll"
          //"-r"; "packages/libsodium-net/lib/Net40/Sodium.dll"
          //"-r"; "packages/BouncyCastle/lib/BouncyCastle.Crypto.dll"
        |]

    let messages, exitCode =
        Async.RunSynchronously (checker.Compile (Array.append compileParams argv))

    let errors = Array.filter (fun (msg:FSharpErrorInfo) -> msg.Severity = FSharpErrorSeverity.Error) messages

    printfn "%A" errors
    printfn "%A" exitCode
    exitCode // return an integer exit code
