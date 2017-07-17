module Consensus.TransactionValidation

open System
open MsgPack
open MsgPack.Serialization

open Consensus.Types
open Consensus.Serialization

open Consensus.Tree
open Consensus.Merkle
//open Consensus.SparseMerkleTree
open Consensus.Authentication

// TODO: Move to constants module
let MaxTransactionSize = pown 2 20
let zhash = Array.zeroCreate<byte>(32)
let MaxKalapa = 100_000_000UL * 100_000_000UL

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

//let contextlessValidate =
    //let nonEmptyInputOutputs tx =
    //    not tx.inputs.IsEmpty && not tx.outputs.IsEmpty
    //let txSizeLimit (txb:byte[]) =
    //    txb.Length <= MaxTransactionSize
    //let legalSpendAmount oput =
    //    oput.spend.asset <> zhash || oput.spend.amount <= MaxKalapa
    //fun (tx:Transaction, txbytes: byte[]) ->
        //nonEmptyInputOutputs tx &&
        //txSizeLimit txbytes &&
        //List.forall legalSpendAmount tx.outputs &&
        //tx.outputs.Length = tx.witnesses.Length

//let matchingSpends ispends ospends =
    //let incSpendMap (smap:Map<Hash,uint64>) spend =
    //    let v = match smap.TryFind(spend.asset) with
    //            | None -> spend.amount
    //            | Some v -> v + spend.amount
    //    smap.Add (spend.asset, v)
    //let spendMap = List.fold incSpendMap Map.empty<Hash,uint64> 
    //let iMap = spendMap ispends
    //let oMap = spendMap ospends
    //List.forall2 (=) <| Map.toList iMap <| Map.toList oMap

//let spendTransferLimit spends (limit:uint64) =
    //let incSpendMap (smap:Map<Hash,bigint>) spend =
    //    let v = match smap.TryFind(spend.asset) with
    //            | None -> bigint spend.amount
    //            | Some v -> v + bigint spend.amount
    //    smap.Add (spend.asset, v)
    //let spendMap = List.fold incSpendMap Map.empty<Hash,bigint>
    //let m = spendMap spends
    //List.forall (fun (_,v) -> v < bigint limit) <| Map.toList m

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

//let nonCoinbaseValidate ptx =
    //List.forall <|
    //((function
    //    | CoinbaseLock _ -> false
    //    | _ -> true) << (fun oput -> oput.lock) << snd
    //) <| ptx.pInputs

// TODO: use in matchingSpends
//let spendMap =
    //let incSpendMap (smap:Map<Hash,uint64>) spend =
    //    let v = match smap.TryFind(spend.asset) with
    //            | None -> spend.amount
    //            | Some v -> v + spend.amount
    //    smap.Add (spend.asset, v)
    //List.fold incSpendMap Map.empty<Hash,uint64> 

//let sumMap (ml:Map<'K,bigint>) (mr:Map<'K,bigint>) =
//    let incMap (m:Map<'K,bigint>) (k,v) =
//        let newV = match m.TryFind(k) with
//                   | None -> v
//                   | Some oldV -> oldV + v
//        m.Add (k,newV)
//    List.fold incMap ml (Map.toList mr)

//let mapToBigInt (m:Map<'K,uint64>) =
//    Map.map (fun _ (v:uint64) -> bigint v) m

//let isNotLessThan (ml:Map<'K,bigint>) (mr:Map<'K,bigint>) =
    //mr |>
    //Map.forall (fun k v ->
        //if v = 0I then
        //     true
        //else
        //    (ml.TryFind k) |>
        //    Option.map ((<=) v) |>
        //    ((=) (Some true))
        //)

// nb generating a POINTED transaction for the coinbase is done differently
//let validateCoinbase ptx feeMap claimableSacMap (reward:uint64) =
   //let bfm = mapToBigInt feeMap
   //let bcsm = mapToBigInt claimableSacMap
   //let breward = bigint reward
   //let allCoinbase = lazy (
   //    (ptx.pInputs, ptx.witnesses) ||>
   //    List.forall2 (fun inp wit ->
   //        wit.Length = 0 &&
   //        match snd inp with
   //        | {lock=CoinbaseLock _} -> true
   //        | _ -> false
   //        ))
   //let inputSpendMap = lazy (
   //    ptx.pInputs |> List.map (fun (_,{spend=spend}) -> spend) |> spendMap
   //)
   //let claimableMap = bfm |> sumMap <| bcsm |> sumMap <| Map [(zhash,breward)]
   //match allCoinbase, inputSpendMap with
   //| Lazy false, _ -> false
   //| Lazy true, Lazy inputSpendMap ->
       //claimableMap |> isNotLessThan <| (mapToBigInt inputSpendMap)

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
        if wit.Length <> 97 then None
        else
            Some <| PKWitness (wit.[0..31], wit.[32..95], SigHashType.Make wit.[96])
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

//let txDigest tx =
    //mutable cachedDigest

let goodOutputVersions {version=version; pInputs=pInputs} =
    not <| List.exists (fun (_,{lock=lock}) -> lockVersion lock > version) pInputs

let validatePKLockAtIndex ptx index pkHash =
    match Option.bind (PKWitness.TryMake) (List.tryItem index ptx.witnesses) with
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
    if index >= ptx.pInputs.Length then false else
    let olock =
        match ptx.pInputs.[index] with
        | (_,{lock=lock}) -> lock
    match olock with
    | CoinbaseLock _                    // even if high version
    | FeeLock _                         // ditto
    | ContractSacrificeLock _ -> false  // ditto
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

let spendMap (outputs:seq<Output>) =
    let emptyMap = Map.empty<Hash,uint64>
    let folder m output =
        match output with
        | {spend={asset=asset;amount=amount}} ->
            let v = Map.tryFind asset m |> Option.defaultValue 0UL
            Map.add asset (v+amount) m
    Seq.fold folder emptyMap outputs

let checkUserTransactionAmounts (ptx:PointedTransaction) =
    match ptx with
    | {pInputs=pInputs; outputs=outputs} ->
        let ins = List.map (fun (_,output) -> output) pInputs
        //let inList = spendMap ins |> Map.toList
        //let outList = spendMap outputs |> Map.toList
        //List.forall2 (=) inList outList
        spendMap ins = spendMap outputs

let checkAutoTransactionAmounts (ptx:PointedTransaction) (contract:Hash) =
    match ptx with
    | {pInputs=pInputs; outputs=outputs} ->
        let ins = List.map (fun (_,output) -> output) pInputs
        //let inList = spendMap ins |> Map.remove contract |> Map.toList |> List.sortBy fst
        //let outList = spendMap outputs |> Map.remove contract |> Map.toList |> List.sortBy fst
        //List.forall2 (=) inList outList
        spendMap ins |> Map.remove contract = (spendMap outputs |> Map.remove contract)

let checkCoinbaseTransactionAmounts (ptx:PointedTransaction) (claimable:Map<Hash,uint64>) =
    match ptx with
    | {pInputs=pInputs; outputs=outputs} ->
        let ins = List.map (fun (_,output) -> output) pInputs
        let addToMap m k claim =
            let v = Map.tryFind k m |> Option.defaultValue 0UL
            Map.add k (v+claim) m
        let totals = Map.fold addToMap (spendMap ins) claimable
        //let inList = totals |> Map.toList
        //let outList = spendMap outputs |> Map.toList
        //List.forall2 (>=) inList outList
        totals = spendMap outputs

let validateUserTx (ptx:PointedTransaction) =
    if ptx.version > 0u then false
    elif ptx.pInputs.Length < 1 || ptx.outputs.Length < 1 then false
    elif List.exists
            (fun input -> match input with
                          | PKLock _ -> false
                          | _ -> true           // fixme: high version
            )
            (List.map (snd >> (fun output -> output.lock)) ptx.pInputs) then false
    elif not <| checkUserTransactionAmounts ptx then false
    elif not <| goodOutputVersions ptx then false
    else List.forall
            (validateAtIndex ptx)
            [0 .. ptx.pInputs.Length - 1]

type ContractFunctionInput = byte[] * Hash * (Outpoint -> Output option)
type TransactionSkeleton = Outpoint list * Output list * byte[]
type ContractFunction = ContractFunctionInput -> TransactionSkeleton

let validateAutoTx  (ptx:PointedTransaction)
                    (utxos:Outpoint -> Output option)
                    (contracts: Hash -> ContractFunction option) =
    if ptx.version > 0u then false
    elif ptx.pInputs.Length < 1 || ptx.outputs.Length < 1 then false
    elif List.exists
            (fun input -> match input with
                          | ContractLock _ -> false
                          | _ -> true
            )
            (List.map (snd >> (fun output -> output.lock)) ptx.pInputs) then false
    else
        let contracthash = ptx.pInputs.[0]
                                |> snd
                                |> fun output ->
                                    match output with 
                                    | {lock=ContractLock (h,_)} -> h
                                    | _ -> failwith "Can't happen"
        if List.exists
                (fun input -> match input with
                              | ContractLock (contracthash, _) -> false
                              | _ -> true
                )
                (List.map (snd >> (fun output -> output.lock)) ptx.pInputs) then false
        elif not <| checkAutoTransactionAmounts ptx contracthash then false
        else
        let contractopt = contracts contracthash
        if contractopt.IsNone then false
        else
        let contract = contractopt.Value
        let msg =
            if ptx.witnesses.[0].Length > 0
            then ptx.witnesses.[0]
            else match snd ptx.pInputs.[0] with
                 | {lock=ContractLock (_, data)} -> data
                 | _ -> Array.empty
        let (outpoints, outputs, newcontractbytes) = contract (msg, contracthash, utxos)
        let newcontract =
            if newcontractbytes.Length = 0 then None
            else try
                    Some <| guardedDeserialise<ExtendedContract> newcontractbytes
                 with _ ->
                    None
        (outpoints = List.map fst ptx.pInputs) &&
        (outputs = ptx.outputs) &&
        (newcontract = ptx.contract)


let isUserTx ptx =
    List.forall
            (fun input -> match input with
                          | PKLock _ -> true
                          | _ -> false           // fixme: high version
            )
            (List.map (snd >> (fun output -> output.lock)) ptx.pInputs)

let isAutoTx ptx =
    List.forall
            (fun input -> match input with
                          | ContractLock _ -> true
                          | _ -> false           // fixme: high version
            )
            (List.map (snd >> (fun output -> output.lock)) ptx.pInputs)

let validateNonCoinbaseTx ptx utxos contracts =
    if isUserTx ptx then validateUserTx ptx
    elif isAutoTx ptx then validateAutoTx ptx utxos contracts
    else false

let validateCoinbaseTx ptx claimable blocknumber =
    if ptx.version > 0u then false      // fixme
    elif ptx.pInputs.Length <> 0 || ptx.outputs.Length < 1 then false
    elif ptx.witnesses.Length < 1 || ptx.witnesses.[0] <> BitConverter.GetBytes(blocknumber:uint32) then false
    elif not <| checkCoinbaseTransactionAmounts ptx claimable then false
    else true