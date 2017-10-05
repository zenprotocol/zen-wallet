module ContractExamples.FStarExecution

open Microsoft.FSharp.Compiler.SourceCodeServices
open System.IO
open System.Reflection
open MBrace.FsPickler.Combinators

let checker = FSharpChecker.Create ()

let currentAssembly = System.Reflection.Assembly.GetExecutingAssembly()
let assemblies = currentAssembly.GetReferencedAssemblies()
let assemblyNames = assemblies |>
                    Array.filter (fun a -> a.Name <> "mscorlib" && a.Name <> "FSharp.Core") |>
                    Array.map (fun a -> Assembly.ReflectionOnlyLoad(a.FullName).Location) |> 
                    Array.toList
                    |> fun l -> System.Reflection.Assembly.GetExecutingAssembly().Location :: l

//TODO: return an un-costed function instead? (to be persisted to disk)
let suffix = """
open MBrace.FsPickler.Combinators
let pickler = Pickler.auto<Zen.Types.Extracted.inputMsg -> Zen.Cost.Realized.cost<FStar.Pervasives.result<Zen.Types.Extracted.transactionSkeleton>, Prims.unit>>
let pickled = Binary.pickle pickler main
"""

// caution, must check for nulls
// https://stackoverflow.com/questions/10435052/f-passing-none-to-function-getting-null-as-parameter-value
let compile source = 
    try 
        let fn = Path.GetTempFileName()
        let fni = Path.ChangeExtension(fn, ".fs")
        let fno = Path.ChangeExtension(fn, ".dll")
        File.WriteAllText(fni, source + System.Environment.NewLine + suffix)
        let assemblyParameters = List.foldBack (fun x xs -> "-r" :: x :: xs) assemblyNames []
        //FIXME: --mlcompatibility
        let compilationParameters = ["--mlcompatibility"; "-o"; fno; "-a"; fni; "--lib:" + System.AppDomain.CurrentDomain.BaseDirectory] @ assemblyParameters |> List.toArray
        let compilationResult =
            checker.CompileToDynamicAssembly(compilationParameters, Some(stdout, stderr))
        let errors, exitCode, dynamicAssembly = Async.RunSynchronously compilationResult
        if exitCode <> 0 then 
            //TODO: trace/log?
            printfn "%A" errors
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
open FSharp.Configuration;

type Settings = AppSettings<"app.config">

let workingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)

let resolvePath (path:string) =
    match Path.IsPathRooted path with
        | true -> Settings.Fstar
        | false -> Path.GetFullPath (Path.Combine (workingDir, path))

let extract source =
    let tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    try
        try
            let moduleName = "ZenModule"
            Directory.CreateDirectory tmp |> ignore
            let fn = (FileInfo (moduleName + ".fst")).Name
            let fni = Path.Combine(tmp, fn)
            let fno = Path.ChangeExtension(fn, ".fs")
            //File.WriteAllText(fni, "module " + moduleName + System.Environment.NewLine + source)
            File.WriteAllText(fni, source)

            let args =
                [|
                    Path.Combine (resolvePath Settings.Fstar, "fstar.exe");
                    //TODO: remove lax
                    "--lax";
                    "--codegen"; "FSharp";
                    "--prims"; Path.Combine (resolvePath Settings.Zulib, "prims.fst");
                    "--extract_module"; moduleName;
                    "--include"; resolvePath Settings.Zulib;
                    "--no_default_includes"; fni;
                    "--verify_all"
                    "--odir"; tmp
                |]

            let procStartInfo = 
                ProcessStartInfo (
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    FileName = "mono",
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
                    printfn "%A" source
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

let deserialize (bs:byte[]) = 
    let pickler = Pickler.auto<ContractFunction>
    try
        bs |> 
        Binary.unpickle pickler |> 
        convertContractFunction |> 
        Some
    with msg -> 
        //TODO: trace/log?
        printfn "%A" msg
        None