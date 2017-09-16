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
module O = Zen.Option

open Zen.Types
open Zen.Cost

val parse_outpoint: n:nat & inputData n -> option outpoint
let parse_outpoint = function
  | (| _ , Outpoint o |) -> Some o
  | _ -> None

val main: inputMsg -> cost transactionSkeleton 0
let main i =
  let open O in
  let addr = i.contractHash in // TODO: to fixed address

  let badTx = Tx 0 V.VNil 0 V.VNil 0 Empty in

  let resTx = match parse_outpoint i.data with
    | Some outpoint ->
        let output = i.utxo outpoint in
        match output with
            | Some output ->
                let outpoints = V.VCons outpoint V.VNil in
                let outputs = V.VCons output V.VNil in
                let tokenOutput = { lock = PKLock addr; spend = {asset = i.contractHash; amount = 1000UL } } in
                let outputs = V.VCons tokenOutput outputs in
                Tx 1 outpoints 2 outputs 0 Empty
            | None -> badTx
    | None -> badTx in

  ret resTx
"""

let deserialize (bs:byte[]) = 
    let pickler = Pickler.auto<CostedFStarContractFunction>
    bs |> Binary.unpickle pickler

open FStarCompatilibity
open Consensus.Types
open Zen.Types
open Zen.Types.Extracted

[<Test>]
let ``Extraction``() =
    let extracted = extract (fstSource, fstModule)
    Assert.IsTrue ((Option.isSome extracted), "Should extract")

    let compiled = extracted |> Option.get |> compile 
    Assert.That ((Option.isSome compiled), "Should compile")

    let func = compiled |> Option.get |> deserialize |> convertContractFunction

    let randomhash = Array.map (fun x -> x*x) [|10uy..41uy|]
    let randomhash2 = Array.map (fun x -> x*x) [|1uy..13uy|]

    let utxo : ContractExamples.Execution.Utxo =
        fun outpoint -> Some { 
                lock = Consensus.Types.PKLock outpoint.txHash; 
                spend = 
                    {
                        asset = randomhash2
                        amount = 1100UL 
                    } 
            }

    let result = func (randomhash, utxo, { Consensus.Types.txHash = randomhash; index = 550ul})

    match result with 
        (outpointList, outputList, data) -> 
            Assert.AreEqual ([], data)
            let outpoint = List.head outpointList
            Assert.AreEqual (550, outpoint.index)
            Assert.AreEqual (randomhash, outpoint.txHash)
      
            Assert.AreEqual (1000UL, outputList.[0].spend.amount)
            Assert.AreEqual (randomhash, outputList.[0].spend.asset)
            Assert.AreEqual (1100UL, outputList.[1].spend.amount)
            Assert.AreEqual (randomhash2, outputList.[1].spend.asset)

            let pkHash = match outputList.[0].lock with 
                | Consensus.Types.PKLock (pkHash) -> pkHash
            //    | _ -> fail here.
            Assert.AreEqual (randomhash, pkHash)