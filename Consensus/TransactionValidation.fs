module Consensus.TransactionValidation

open System
open MsgPack
open MsgPack.Serialization

open Consensus.Types
open Consensus.Serialization

open Consensus.Tree
open Consensus.Merkle
//open Consensus.SparseMerkleTree


// TODO: Move to constants module
let MaxTransactionSize = pown 2 20
let zhash = Array.zeroCreate<byte>(32)
// TODO: Insert underscore separators when F# 4.1 is released
let MaxKalapa = 21UL * 1000000UL * 100000000UL

let toOpt f x =
    try
        Some <| f x
    with
        | _ -> None

let isCanonical<'V> bytearray =
    let serializer = MessagePackSerializer.Get<'V>(context)
    use stream = new System.IO.MemoryStream(bytearray:byte[])
    let res = serializer.Unpack(stream)
    use reserializedStream = new System.IO.MemoryStream()
    serializer.Pack(reserializedStream, res)
    reserializedStream.ToArray() = bytearray

let guardedDeserialise<'V> s =
    let serializer = MessagePackSerializer.Get<'V>(context)
    use stream = new System.IO.MemoryStream(s:byte[])
    let res = serializer.Unpack(stream)
    use reserializedStream = new System.IO.MemoryStream()
    serializer.Pack(reserializedStream, res)
    if reserializedStream.ToArray() <> s then
        failwith "Non-canonical object"
    res

let contextlessValidate =
    let nonEmptyInputOutputs tx =
        not tx.inputs.IsEmpty && not tx.outputs.IsEmpty
    let txSizeLimit (txb:byte[]) =
        txb.Length <= MaxTransactionSize
    let legalSpendAmount oput =
        oput.spend.asset <> zhash || oput.spend.amount <= MaxKalapa
    fun (tx:Transaction, txbytes: byte[]) ->
        nonEmptyInputOutputs tx &&
        txSizeLimit txbytes &&
        List.forall legalSpendAmount tx.outputs &&
        tx.outputs.Length = tx.witnesses.Length

let matchingSpends ispends ospends =
    let incSpendMap (smap:Map<Hash,uint64>) spend =
        let v = match smap.TryFind(spend.asset) with
                | None -> spend.amount
                | Some v -> v + spend.amount
        smap.Add (spend.asset, v)
    let spendMap = List.fold incSpendMap Map.empty<Hash,uint64> 
    let iMap = spendMap ispends
    let oMap = spendMap ospends
    List.forall2 (=) <| Map.toList iMap <| Map.toList oMap

type PointedInput = Outpoint * Output

type PointedTransaction = {version: uint32; pInputs: PointedInput list; witnesses: Witness list; outputs: Output list; contract: ExtendedContract option}

let toPointedTransaction tx (inputs : _ list) =
    if tx.inputs.Length > inputs.Length then
        failwith "list of inputs is too short for given transaction"
    else
        let pInputs = List.zip tx.inputs inputs
        {version=tx.version;pInputs=pInputs;witnesses=tx.witnesses;outputs=tx.outputs;contract=tx.contract}

let unpoint {version=version;pInputs=pInputs;witnesses=witnesses;outputs=outputs;contract=contract} =
    {version=version;inputs=List.map fst pInputs;witnesses=witnesses;outputs=outputs;contract=contract}

let nonCoinbaseValidate ptx =
    List.forall <|
    ((function
        | CoinbaseLock _ -> false
        | _ -> true) << (fun oput -> oput.lock) << snd
    ) <| ptx.pInputs

// TODO: use in matchingSpends
let spendMap =
    let incSpendMap (smap:Map<Hash,uint64>) spend =
        let v = match smap.TryFind(spend.asset) with
                | None -> spend.amount
                | Some v -> v + spend.amount
        smap.Add (spend.asset, v)
    List.fold incSpendMap Map.empty<Hash,uint64> 

let sumMap (ml:Map<'K,uint64>) (mr:Map<'K,_>) =
    let incMap (m:Map<'K,_>) (k,v) =
        let newV = match m.TryFind(k) with
                   | None -> v
                   | Some oldV -> oldV + v
        m.Add (k,newV)
    List.fold incMap ml (Map.toList mr)

let isNotLessThan (ml:Map<'K,uint64>) (mr:Map<'K,_>) =
    mr |>
    Map.forall (fun k v ->
        if v = 0UL then
             true
        else
            (ml.TryFind k) |>
            Option.map ((<=) v) |>
            ((=) (Some true))
        )


let validateCoinbase ptx feeMap claimableSacMap reward =
   let allCoinbase = lazy (
       (ptx.pInputs, ptx.witnesses) ||>
       List.forall2 (fun inp wit ->
           wit.Length = 0 &&
           match snd inp with
           | {lock=CoinbaseLock _} -> true
           | _ -> false
           ))
   let inputSpendMap = lazy (
       ptx.pInputs |> List.map (fun (_,{spend=spend}) -> spend) |> spendMap
   )
   let claimableMap = feeMap |> sumMap <| claimableSacMap |> sumMap <| Map [(zhash,reward)]
   match allCoinbase, inputSpendMap with
   | Lazy false, _ -> false
   | Lazy true, Lazy inputSpendMap ->
       claimableMap |> isNotLessThan <| inputSpendMap

type SigHashOutputType =
    | SigHashAll
    | SigHashNone
    | SigHashSingle
    with
    member this.Byte =
        match this with
        | SigHashAll -> 0x00uy
        | SigHashNone -> 0x02uy
        | SigHashSingle -> 0x01uy

type SigHashInputType =
    | SigHashOneCanPay
    | SigHashAnyoneCanPay
    with
    member this.Byte =
        match this with
        | SigHashOneCanPay ->0x00uy
        | SigHashAnyoneCanPay -> 0x08uy

type SigHashType =
    SigHashType of inputType:SigHashInputType * outputType:SigHashOutputType
    with
    static member Make (hbyte) =
        let itype =
            if hbyte &&& 0x80uy <> 0uy then SigHashAnyoneCanPay else SigHashOneCanPay
        let otype =
            match hbyte &&& 0x02uy <> 0uy, hbyte &&& 01uy <> 0uy with
            | true, _ -> SigHashNone
            | _, true -> SigHashSingle
            | _ -> SigHashAll
        SigHashType (itype, otype)    

    member this.Byte =
        match this with
        | SigHashType (inputType=inputType;outputType=outputType) ->
             inputType.Byte ||| outputType.Byte

type PKWitness =
    PKWitness of publicKey:byte[] * edSignature:byte[] * hashtype:SigHashType
    with
    static member TryMake(wit:Witness) =
        if wit.Length <> 65 then None
        else
            Some <| PKWitness (wit.[0..31], wit.[32..63], SigHashType.Make wit.[64])
    member this.toWitness : Witness =
        match this with
        | PKWitness (publicKey=publicKey;edSignature=edSignature;hashtype=hashtype) ->
            Array.concat [publicKey; edSignature; [|hashtype.Byte|]]

let reducedTx tx index (SigHashType (itype, otype)) =
    let inputs =
        match itype with
        | SigHashOneCanPay -> tx.inputs
        | SigHashAnyoneCanPay -> [tx.inputs.[index]]
    let outputs =
        match otype with
        | SigHashAll -> tx.outputs
        | SigHashNone -> []
        | SigHashSingle -> [tx.outputs.[index]]
    {tx with inputs=inputs; outputs=outputs; witnesses=[]}

let txDigest tx index hashtype = transactionHasher << reducedTx tx index <| hashtype

let goodOutputVersions {version=version; pInputs=pInputs} =
    not <| List.exists (fun (_,{lock=lock}) -> lockVersion lock > version) pInputs

let validatePKLockAtIndex ptx index pkHash =
    match PKWitness.TryMake ptx.witnesses.[index] with
    | None -> false
    | Some (PKWitness (publicKey=publicKey; edSignature=edSignature; hashtype=hashtype)) ->
         if innerHash publicKey <> pkHash then false
         else
             Sodium.PublicKeyAuth.VerifyDetached (edSignature, txDigest (unpoint ptx) index hashtype, publicKey)

// Tx signing is included here for convenience
let signatureAtIndex tx index hashtype privkey =
    Sodium.PublicKeyAuth.SignDetached (txDigest tx index hashtype, privkey)

let pkWitnessAtIndex tx index hashtype privkey =
    PKWitness (publicKey=Sodium.PublicKeyAuth.ExtractEd25519PublicKeyFromEd25519SecretKey privkey, edSignature = signatureAtIndex tx index hashtype privkey, hashtype=hashtype)

let validateAtIndex ptx index =
    let olock =
        match ptx.pInputs.[index] with
        | (_,{lock=lock}) -> lock
    match olock with
    | CoinbaseLock _ // even if high version
    | FeeLock _ // ditto
    | ContractSacrificeLock _ -> false // ditto
    | HighVLock _ -> true
    | PKLock pkHash ->
        validatePKLockAtIndex ptx index pkHash
    | ContractLock _ ->
        false


// Usage: pass in a transaction and a list of private keys.
// Returns the transaction with signatures for each index
// for which the key is not an empty byte array.
let signTx (tx:Transaction) outputkeys =
    let hashtype = SigHashType (SigHashOneCanPay, SigHashAll)
    let witnesses =
        List.mapi <|
        (fun i privkey ->
            if Array.isEmpty privkey then tx.witnesses.[i]
            else (pkWitnessAtIndex tx i hashtype privkey).toWitness) <|
        outputkeys
    {tx with witnesses=witnesses}


let checkHeader (header:BlockHeader) =
    // No additional checks until first soft-fork
    true

let uncompressDifficulty pdiff =
    // get high byte, shift right, use as byte offset
    // get low bits, use as 'significand'
    let lshift = ((pdiff &&& 0xff000000u) >>> 24) - 3u
    let rshift = int <| 31u - lshift //index of low order byte
    let diff = Array.zeroCreate(32)
    diff.[rshift] <- byte (pdiff &&& 0xffu)
    diff.[rshift-1] <- byte ((pdiff &&& 0xff00u) >>> 8)
    diff.[rshift-2] <- byte ((pdiff &&& 0xff0000u) >>> 16)
    diff

let checkPOW (header:BlockHeader) consensusDifficulty =
    if header.pdiff <> consensusDifficulty then false
    else
        let blockID = blockHeaderHasher header
        let target = uncompressDifficulty header.pdiff
        blockID <= target

let transactionMR cTW txs =
    merkleRoot cTW transactionHasher txs

let checkTransactionMerkleRoot block =
    let cTW = block.header.parent
    let txs = block.transactions
    block.header.txMerkleRoot = transactionMR cTW txs

let checkWitnessMerkleRoot block =
    //stub
    true

let checkContractMerkleRoot block =
    //stub
    true

type BlockContext = {difficulty: uint32}

let validateBlock blockcontext (block:Block) =
   checkHeader block.header &&
   checkPOW block.header blockcontext.difficulty &&
   checkTransactionMerkleRoot block &&
   checkWitnessMerkleRoot block &&
   checkContractMerkleRoot block
