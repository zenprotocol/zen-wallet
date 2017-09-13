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
open ContractExamples.FStarCompatilibity
let pickler = Pickler.auto<CostedFStarContractFunction>
let pickled = Binary.pickle pickler main
"""

let compile (source:string) = 
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
                    Some (asm.GetModules().[0].GetTypes().[0].GetProperty("pickled").GetValue(null) :?> byte[])
    with _ -> None

open System.Diagnostics;
open FSharp.Configuration;

type Settings = AppSettings<"app.config">

let workingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)

let resolvePath (path:string) =
    match Path.IsPathRooted path with
        | true -> Settings.Fstar
        | false -> Path.GetFullPath (Path.Combine (workingDir, path))

let extract (source:string, moduleNameTemp:string) =
    let tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    try
        try
            Directory.CreateDirectory tmp |> ignore
            let fn = (FileInfo (moduleNameTemp + ".fst")).Name
            let fni = Path.Combine(tmp, fn)
            let fno = Path.ChangeExtension(fn, ".fs")
            File.WriteAllText(fni, source)

            let args =
                [|
                    Path.Combine (resolvePath Settings.Fstar, "fstar.exe");
                    //TODO: remove lax
                    "--lax";
                    "--codegen"; "FSharp";
                    "--prims"; Path.Combine (resolvePath Settings.Zulib, "prims.fst");
                    "--extract_module"; moduleNameTemp;
                    "--include"; resolvePath Settings.Zulib;
                    "--no_default_includes"; fni;
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
                    None
                else
                    Some (File.ReadAllText (Path.Combine (tmp, fno)))
        with msg -> 
            //TODO: trace/log?
            printfn "%A" msg
            None
    finally
        Directory.Delete (tmp, true)


open NUnit.Framework
open FStarCompatilibity

let fstModule = "CostedSimpleContract"
let fstSource = """
module CostedSimpleContract
module V = Zen.Vector
open Zen.Types.Extracted
open Zen.Base
open Zen.Cost
open Zen


val makeTx:
    l1:nat
 -> l2:nat
 -> l3:nat
 -> outpoints: V.t outpoint l1
 -> outputs: V.t output l2
 -> data:data l3
 -> transactionSkeleton
let makeTx l1 l2 l3 outpoints outputs data =
  Tx l1 outpoints l2 outputs l3 data


val main: i:inputMsg -> cost transactionSkeleton 0
let main inputMsg =
  let data = inputMsg.contractHash in

  let outpoints' = V.VCons ({ txHash = data; index = 1ul }) V.VNil in
  let outputs' = V.VCons ({ lock = PKLock data; spend = {asset = data; amount = 1900UL } }) V.VNil in
  let freeTx = makeTx 1 1 32 outpoints' outputs' (ByteArray 32 data) in

  ret freeTx
"""

let deserialize (bs:byte[]) = 
    let pickler = Pickler.auto<CostedFStarContractFunction>
    bs |> Binary.unpickle pickler

open FStarCompatilibity

[<Test>]
let ``Extraction``() =
    let extracted = extract (fstSource, fstModule)
    Assert.IsTrue ((Option.isSome extracted), "Should extract")

    let compiled = extracted |> Option.get |> compile 
    Assert.That ((Option.isSome compiled), "Should compile")

    let func = compiled |> Option.get |> deserialize |> convertContractFunction

    let utxo : ContractExamples.Execution.Utxo =
        fun outpoint -> Some { lock = Consensus.Types.PKLock outpoint.txHash; spend = {asset = outpoint.txHash; amount = 1100UL } }

    let randomhash = Array.map (fun x -> x*x) [|10uy..41uy|]

    let input = (randomhash, randomhash, utxo)

    let result = func input

    match result with 
        (outpointList, outputList, data) -> 
            Assert.AreEqual (randomhash, data)
            let outpoint = List.head outpointList
            Assert.AreEqual (1, outpoint.index)
            Assert.AreEqual (randomhash, outpoint.txHash)
            let output = List.head outputList
            Assert.AreEqual (1900UL, output.spend.amount)
            Assert.AreEqual (randomhash, output.spend.asset)
            let pkHash = match output.lock with Consensus.Types.PKLock (pkHash) -> pkHash
            Assert.AreEqual (randomhash, pkHash)