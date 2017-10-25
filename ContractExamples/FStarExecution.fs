module ContractExamples.FStarExecution

open Microsoft.FSharp.Compiler.SourceCodeServices
open System.IO
open System.Reflection
open MBrace.FsPickler.Combinators

open FSharp.Configuration;

let workingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)

let rootPath path = Path.Combine (workingDir, path)

let fsSuffix = """
open MBrace.FsPickler.Combinators
let pickler = Pickler.auto<Zen.Types.Extracted.mainFunction>
let pickled = Binary.pickle pickler mainFunction
"""

let checker = FSharpChecker.Create ()

// caution, must check for nulls
// https://stackoverflow.com/questions/10435052/f-passing-none-to-function-getting-null-as-parameter-value
let compile source = 
    try 
        let fn = Path.GetTempFileName()
        let fni = Path.ChangeExtension(fn, ".fs")
        let fno = Path.ChangeExtension(fn, ".dll")

        File.WriteAllText(fni, source + System.Environment.NewLine + fsSuffix)

        let compilationParameters = [|
            "--mlcompatibility"; 
            "-o"; fno;
            "-a"; fni;
            "-r"; "Zulib.dll";
            "-r"; "FsPickler.dll";
            "-r"; "FSharp.Compatibility.OCaml.dll"
        |]
                    
        let compilationResult =
            checker.CompileToDynamicAssembly(compilationParameters, Some(stdout, stderr))
        let errors, exitCode, dynamicAssembly = Async.RunSynchronously compilationResult
        if exitCode <> 0 then 
            //TODO: trace/log?
            printfn "compilation failed: %A" errors
            None
        else 
            match dynamicAssembly with
                | None -> None
                | Some asm -> 
                    let compiledType = asm.GetModules().[0].GetTypes().[0]
                    let propertyValue = compiledType.GetProperty("pickled").GetValue(null)
                    Some (propertyValue :?> byte[])
    with err -> 
        printfn "%A" err
        None

open System.Diagnostics;

let mono_locations = [ //TODO: prioritize
    "/usr/bin/mono"
    "/usr/local/bin/mono"
    "/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono"
]
    
let mono = List.tryFind File.Exists mono_locations 

let fstarPath =     
    let fstarDevPath =
        match System.Environment.OSVersion.Platform with
        | System.PlatformID.Win32NT -> rootPath "../../../tools/fstar/dotnet/fstar.exe"               
        | _ -> rootPath "../../../tools/fstar/mono/fstar.exe"
                                      
    List.find File.Exists [fstarDevPath; rootPath "fstar/fstar.exe"]            
    
let z3Path =
    let z3Locations = 
        match System.Environment.OSVersion.Platform with
        | System.PlatformID.Win32NT -> [rootPath "../../../tools/z3/windows/z3.exe"; rootPath "z3/z3.exe"]
        | System.PlatformID.MacOSX -> [rootPath "../../../tools/z3/osx/z3"; rootPath "z3/z3"]
        | _ -> [rootPath "../../../tools/z3/linux/z3"; rootPath "z3/z3"]
        
    List.find File.Exists z3Locations
    
let zulibPath = List.find Directory.Exists [rootPath "../../../Zulib/fstar"; rootPath "Zulib"]
    

let extract source =
    let tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())    
    try
        try
            let moduleName = "ZenModule" //TODO: use contract's hash as module name?
            Directory.CreateDirectory tmp |> ignore
            let fn = (FileInfo (moduleName + ".fst")).Name
            let fni = Path.Combine(tmp, fn)
            let fn'elabed = Path.ChangeExtension(fni, ".elab.fst")
            let fno = Path.ChangeExtension(fn, ".fs")
            //File.WriteAllText(fni, "module " + moduleName + System.Environment.NewLine + source)
            File.WriteAllText(fni, source)
            IOUtils.elaborate fni fn'elabed

            let executableArg =  
                match System.Environment.OSVersion.Platform with
                | System.PlatformID.Win32NT -> [| |]
                | _ -> [| fstarPath |]                                  

            let (++) = Array.append

            let args = executableArg ++ [|                    
                                        //TODO: remove lax
                                        "--lax";
                                        "--smt";z3Path;
                                        "--codegen"; "FSharp";
                                        "--prims"; Path.Combine (zulibPath, "prims.fst");
                                        "--extract_module"; moduleName;
                                        "--include"; zulibPath;
                                        "--no_default_includes"; fn'elabed;
                                        "--verify_all";
                                        "--odir"; tmp
                                    |]
                                    
            let executable = 
                match System.Environment.OSVersion.Platform with
                | System.PlatformID.Win32NT -> fstarPath
                | _ -> Option.get mono //TODO: handle None                                  

            let procStartInfo = 
                ProcessStartInfo (
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    FileName = executable, 
                    Arguments = String.concat " " args
                )

            let p = new Process(StartInfo = procStartInfo)

            //TODO: trace/log?
            let outputHandler f (_sender:obj) (args:DataReceivedEventArgs) = f args.Data
            p.OutputDataReceived.AddHandler(DataReceivedEventHandler (outputHandler (printfn "output: %A")))
            p.ErrorDataReceived.AddHandler(DataReceivedEventHandler (outputHandler(printfn "error: %A")))

            if not (p.Start()) then
                None
            else
                p.BeginOutputReadLine()
                p.BeginErrorReadLine()
                p.WaitForExit()

                if p.ExitCode <> 0 then
                    printfn "extraction/verification failed: %A" source
                    None
                else
                    Some (File.ReadAllText (Path.Combine (tmp, fno)))
        with msg -> 
            //TODO: trace/log?
            printfn "%A" msg
            None
    finally
        Directory.Delete (tmp, true)

open FStarCompatibility
open Zen.Types.Extracted

let deserialize (bs:byte[]) = 
    try
        bs 
        |> Binary.unpickle Pickler.auto<mainFunction> 
        |> convertContractFunction 
        |> Some
    with msg -> 
        //TODO: trace/log?
        printfn "%A" msg
        None