module ContractExamples.FStarExecution

open Microsoft.FSharp.Compiler.SourceCodeServices
open System.IO
open System.Reflection
open MBrace.FsPickler.Combinators

let checker = FSharpChecker.Create ()
let pickler = Pickler.auto<ContractExamples.FStarCompatilibity.FStarContractFunction>

let currentAssembly = System.Reflection.Assembly.GetExecutingAssembly()
let assemblies = currentAssembly.GetReferencedAssemblies()
let assemblyNames = assemblies |>
                        Array.filter (fun a -> a.Name <> "mscorlib" && a.Name <> "FSharp.Core") |>
                        Array.map (fun a -> Assembly.ReflectionOnlyLoad(a.FullName).Location) |> 
                        Array.toList
                        |> fun l -> System.Reflection.Assembly.GetExecutingAssembly().Location :: l

let suffix = """

open MBrace.FsPickler.Combinators
let pickler = Pickler.auto<ContractExamples.FStarCompatilibity.FStarContractFunction>
let pickled = Binary.pickle pickler main
"""

let compile (source:string) = 
    try 
        let fn = Path.GetTempFileName()
        let fni = Path.ChangeExtension(fn, ".fs")
        let fno = Path.ChangeExtension(fn, ".dll")
        File.WriteAllText(fni, source +  suffix)
        let assemblyParameters = List.foldBack (fun x xs -> "-r" :: x :: xs) assemblyNames []
        let compilationParameters = ["--mlcompatibility"; "-o"; fno; "-a"; fni; "--lib:" + System.AppDomain.CurrentDomain.BaseDirectory] @ assemblyParameters |> List.toArray
        let compilationResult =
            checker.CompileToDynamicAssembly(compilationParameters, Some(stdout, stderr))
        let errors, exitCode, dynamicAssembly = Async.RunSynchronously compilationResult
        if exitCode <> 0 then 
            printfn "%A" errors
            None
        else 
            match dynamicAssembly with
                | None -> None
                | Some asm -> 
                    Some (asm.GetModules().[0].GetTypes().[0].GetProperty("pickled").GetValue(null) :?> byte[])
    with _ -> None


open System.Diagnostics;

//TODO: fix paths
let fstar = "../../../../FStar/bin/fstar.exe"
let zulib = "../../../Zen.Lib/"
let prims = "../../../Zen.Lib/prims.fst"

//TODO: temp files, cleanups
let extract (source:string, moduleNameTemp:string) =
    try
        let fn = Path.GetFileName(moduleNameTemp) //.GetTempFileName()
        let fni = Path.ChangeExtension(fn, ".fst")
        let fno = Path.ChangeExtension(fn, ".fs")
        File.WriteAllText(fni, source)

        let args =
            String.concat " " [|
                fstar; "--codegen"; "FSharp";
                "--prims"; prims;
                "--extract_module"; moduleNameTemp;
                "--include"; zulib;
                "--no_default_includes"; fni
            |]

        let procStartInfo = 
            ProcessStartInfo (
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                FileName = "mono",
                Arguments = args
            )

        let p = new Process(StartInfo = procStartInfo)

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
                None
            else
                Some (File.ReadAllText fno)
    with _ -> None