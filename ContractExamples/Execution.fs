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
open Swensen.Unquote.Operators
open MBrace.FsPickler.Combinators

// Repeated code
open Consensus.Types
open Authentication


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

type CallOptionParameters =
    {
        numeraire: Hash;
        controlAsset: Hash;
        controlAssetReturn: Hash;
        oracle: Hash;
        underlying: string;
        price: decimal;
        minimumCollateralRatio: decimal;
        ownerPubKey: byte[]
    }
   
let BadTx : Expr<TransactionSkeleton> = <@ [], [], [||] @>

type DataFormat = uint64 * uint64 * uint64 // tokens issued, quantity of collateral, authenticated use counter

let tryParseData (data:byte[]) =
    maybe {
        try
            if data.Length <> 24 then
                failwith "data of wrong length"
            let tokens, collateral, counter = data.[0..7], data.[8..15], data.[16..23]
            return (bytesToUInt64 tokens, bytesToUInt64 collateral, bytesToUInt64 counter)
        with _ ->
            return! None
    }
let makeData (tokens, collateral, counter) = Array.concat <| List.map uint64ToBytes [tokens; collateral; counter]

let returnToSender (opoint:Outpoint, oput:Output) = List.singleton opoint, List.singleton oput, Array.empty<byte>

//End of repeated code

let quotePickler = Pickler.auto<Expr<Contracts.ContractFunction>>

let quotedToString qc = qc |> Binary.pickle quotePickler |> System.Convert.ToBase64String

let compileContract (code:string) = code |> System.Convert.FromBase64String |> Binary.unpickle quotePickler |> eval

//let compile (code:string) =
    //match code with
    //| "QQQ\n" -> ()
    //| _ -> ()