module ContractExamples.Execution

// TODO: Use Unquote
//open FSharp.Quotations.Evaluator
//open Microsoft.FSharp.Quotations
//open MBrace.FsPickler

//type SerializableContract =
//    | QuotedContract of Expr<Contracts.ContractFunction>
//    | UnquotedContract of Contracts.ContractFunction

//let pickler = Combinators.Pickler.auto<SerializableContract>

//let serialize (contract:SerializableContract) =
//    Combinators.Binary.pickle pickler contract

//let deserialize (bs:byte[]) =
    //match Combinators.Binary.unpickle pickler bs with
    //| QuotedContract qc -> QuotationEvaluator.Evaluate qc
    //| UnquotedContract uc -> uc


open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns;;
open Swensen.Unquote.Operators
open MBrace.FsPickler.Combinators

// Repeated code
open Consensus.Types
open Consensus.Authentication

let auth = sign


type ContractFunctionInput = byte[] * Hash * (Outpoint -> Output option)
type TransactionSkeleton = Outpoint list * Output list * byte[]
type ContractFunction = ContractFunctionInput -> TransactionSkeleton

let maybe = MaybeWorkflow.maybe
type InvokeMessage = byte * Outpoint list

let simplePackOutpoint : Outpoint -> byte[] = fun p ->
    match p with
    | {txHash=txHash;index=index} ->
        if index > 255u then failwith "oops!"
        else
            let res = Array.zeroCreate 33
            res.[0] <- (byte)index
            Array.blit txHash 0 res 1 32
            res

let packManyOutpoints : Outpoint list -> byte[] = fun ps ->
    ps |> List.map simplePackOutpoint |> Array.concat

let makeOutpoint (outpointb:byte[]) = {txHash=outpointb.[1..]; index = (uint32)outpointb.[0]}

let tryParseInvokeMessage (message:byte[]) =
    maybe {
        try
            let opcode, rest = message.[0], message.[1..]
            let outpointbytes = Array.chunkBySize 33 rest
            if outpointbytes |> Array.last |> Array.length <> 33 then
                failwith "last output has wrong length"
            let outpoints = Array.map makeOutpoint outpointbytes
            return opcode, outpoints
        with _ ->
            return! None
    }

let bytesToUInt64 : byte[] -> uint64 = fun bs ->
    if bs.Length <> 8 then failwith "wrong length byte array for uint64"
    let sysbytes =
        if System.BitConverter.IsLittleEndian then
            Array.rev bs
        else
            Array.copy bs
    System.BitConverter.ToUInt64 (sysbytes, 0)

let uint64ToBytes : uint64 -> byte[] = fun v ->
    let sysbytes = System.BitConverter.GetBytes v
    if System.BitConverter.IsLittleEndian then
        Array.rev sysbytes
    else
        sysbytes
let BadTx : TransactionSkeleton = [], [], [||]

//End of repeated code

let quotePickler = Pickler.auto<Expr<Contracts.ContractFunction>>
let pickler = Pickler.auto<Contracts.ContractFunction>

let quotedToString qc = qc |> Json.pickle quotePickler |> (fun s -> "QQQ\n" + s)

let compileQuotedContract (code:string) = code |> Json.unpickle quotePickler |> eval

let (|Prefix|_|) (p:string) (s:string) =
    if s.StartsWith(p) then
        Some(s.Substring(p.Length))
    else
        None

let compilationTemplate = """
module Zen.Main
open Microsoft.FSharp.Quotations
open MBrace.FsPickler.Combinators

// Repeated code
open Consensus.Types
open Consensus.Authentication
let pickler = Pickler.auto<ContractExamples.Contracts.ContractFunction>


type ContractFunctionInput = byte[] * Hash * (Outpoint -> Output option)
type TransactionSkeleton = Outpoint list * Output list * byte[]
type ContractFunction = ContractFunctionInput -> TransactionSkeleton

let maybe = ContractExamples.MaybeWorkflow.maybe
type InvokeMessage = byte * Outpoint list

let simplePackOutpoint : Outpoint -> byte[] = fun p ->
    match p with
    | {txHash=txHash;index=index} ->
        if index > 255u then failwith "oops!"
        else
            let res = Array.zeroCreate 33
            res.[0] <- (byte)index
            Array.blit txHash 0 res 1 32
            res

let packManyOutpoints : Outpoint list -> byte[] = fun ps ->
    ps |> List.map simplePackOutpoint |> Array.concat

let makeOutpoint (outpointb:byte[]) = {txHash=outpointb.[1..]; index = (uint32)outpointb.[0]}

let tryParseInvokeMessage (message:byte[]) =
    maybe {
        try
            let opcode, rest = message.[0], message.[1..]
            let outpointbytes = Array.chunkBySize 33 rest
            if outpointbytes |> Array.last |> Array.length <> 33 then
                failwith "last output has wrong length"
            let outpoints = Array.map makeOutpoint outpointbytes
            return opcode, outpoints
        with _ ->
            return! None
    }

let bytesToUInt64 : byte[] -> uint64 = fun bs ->
    if bs.Length <> 8 then failwith "wrong length byte array for uint64"
    let sysbytes =
        if System.BitConverter.IsLittleEndian then
            Array.rev bs
        else
            Array.copy bs
    System.BitConverter.ToUInt64 (sysbytes, 0)

let uint64ToBytes : uint64 -> byte[] = fun v ->
    let sysbytes = System.BitConverter.GetBytes v
    if System.BitConverter.IsLittleEndian then
        Array.rev sysbytes
    else
        sysbytes

   
let BadTx : TransactionSkeleton = [], [], [||]


let contract:ContractFunction = %%%SRC%%%
let pickledContract = Binary.pickle pickler contract
"""

open Microsoft.FSharp.Compiler.SourceCodeServices
open System.IO
open System.Reflection
let checker = FSharpChecker.Create ()

let assemblies = System.Reflection.Assembly.GetExecutingAssembly().GetReferencedAssemblies()
let assemblyNames = assemblies |>
                        Array.filter (fun a -> a.Name <> "mscorlib" && a.Name <> "FSharp.Core") |>
                        Array.map (fun a -> Assembly.ReflectionOnlyLoad(a.FullName).Location) |> 
                        Array.toList
                        |> fun l -> System.Reflection.Assembly.GetExecutingAssembly().Location :: l

let compile (code:string) = maybe {
    match code with
    | Prefix "QQQ\n" rest -> return rest |> compileQuotedContract |> Binary.pickle pickler
    | _ ->
        let source = compilationTemplate.Replace (@"%%%SRC%%%", code)
        let fn = Path.GetTempFileName()
        let fni = Path.ChangeExtension(fn, ".fs")
        let fno = Path.ChangeExtension(fn, ".dll")
        File.WriteAllText(fni, source)
        let assemblyParameters = List.foldBack (fun x xs -> "-r" :: x :: xs) assemblyNames []
        let compilationParameters = ["-o"; fno; "-a"; fni; "--lib:" + System.AppDomain.CurrentDomain.BaseDirectory] @ assemblyParameters |> List.toArray
        let compilationResult =
            checker.CompileToDynamicAssembly(compilationParameters, Some(stdout, stderr))
        let errors, exitCode, dynamicAssembly = Async.RunSynchronously compilationResult
        if exitCode <> 0 then return! None // ignore compiler warning messages
        match dynamicAssembly with
        | None -> return! None
        | Some asm ->
            let m = Array.head <| asm.GetModules()
            let pickled = m.GetTypes().[0].GetProperty("pickledContract").GetValue(m) :?> byte[]
            return pickled
    }

let deserialize (bs:byte[]) = bs |> Binary.unpickle pickler

type ContractMetadata =
    public
    | Oracle of ContractExamples.QuotedContracts.OracleParameters
    | CallOption of ContractExamples.QuotedContracts.CallOptionParameters
    | SecureToken of ContractExamples.QuotedContracts.SecureTokenParameters

let (|ContractMetadata|_|) (name:string) (parameters:obj) =
    match name, parameters with
    | "calloption", (:? ContractExamples.QuotedContracts.CallOptionParameters as cparams) ->
        Some <| CallOption (cparams)
    | "oracle", (:? ContractExamples.QuotedContracts.OracleParameters as oparams) ->
        Some <| Oracle (oparams)
    | "securetoken", (:? ContractExamples.QuotedContracts.SecureTokenParameters as sparams) ->
        Some <| SecureToken (sparams)
    | _ -> None

let tryParseContractMetadata (s:string) =
    None // Not implemented

let metadata (s:string) =
    match s with
    | Prefix "QQQ\n" rest ->
        let qc = Json.unpickle quotePickler rest
        match qc with
        | Let (v1,Value(:? string as cType,_), Let(v2, ValueWithName(m,_,_), _)) when v1.Name = "contractType" && v2.Name= "meta" ->
            match m with
            | ContractMetadata cType md -> Some md
            | _ -> None
        | _ -> None
    | _ ->
        tryParseContractMetadata s