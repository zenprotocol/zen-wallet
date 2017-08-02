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

// Repeated code
open Consensus.Types
open Consensus.Authentication

open QuotedContracts
open Newtonsoft.Json.Linq

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

let (|Prefix|_|) (p:string) (s:string) =
    if s.StartsWith(p) then
        Some(s.Substring(p.Length))
    else
        None

let compilationTemplate = """
module Zen.Main
open Microsoft.FSharp.Quotations
open System.Collections.Generic
open Newtonsoft.Json.Linq

// Repeated code
open Consensus.Types
open Consensus.Authentication


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
        return asm.GetModules().[0].GetTypes().[0].GetMethod("contract")
}

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

let metadata (s:string) =
    let json = s.Split '\n' |> fun s -> s |> Array.item (s.Length - 1) |> fun s -> s.Substring 2 |> JObject.Parse
    match json.Item("contractType").Value<string>() with
    | "oracle" ->
        Some <| Oracle {ownerPubKey = System.Convert.FromBase64String <| json.Item("ownerPubKey").Value<string>()}
    | "calloption" ->
        Some <| CallOption {
            numeraire = System.Convert.FromBase64String <| json.Item("numeraire").Value<string>();
            controlAsset = System.Convert.FromBase64String <| json.Item("controlAsset").Value<string>();
            controlAssetReturn = System.Convert.FromBase64String <| json.Item("controlAssetReturn").Value<string>();
            oracle = System.Convert.FromBase64String <| json.Item("oracle").Value<string>();
            underlying = json.Item("underlying").Value<string>();
            price = json.Item("price").Value<decimal>();
            strike = json.Item("strike").Value<decimal>();
            minimumCollateralRatio = json.Item("minimumCollateralRatio").Value<decimal>();
            ownerPubKey = System.Convert.FromBase64String <| json.Item("ownerPubKey").Value<string>()
        }
    | "securetoken" ->
        Some <| SecureToken {destination = System.Convert.FromBase64String <| json.Item("destination").Value<string>()}
    | _ -> None