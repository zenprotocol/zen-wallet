module ContractExamples.Contracts

open Consensus.Types
open Authentication

type ContractFunctionInput = byte[] * Hash * Map<Outpoint, Output>
type TransactionSkeleton = Outpoint list * Output list * byte[]
type ContractFunction = ContractFunctionInput -> TransactionSkeleton

type CallOptionParameters =
    {
        numeraire: Hash;
        controlAsset: Hash;
        controlAssetReturn: Hash;
        oracle: Hash;
        underlying: string;
        price: uint64;
        minimumCollateralRatio: decimal
    }

type CallMessage =
    | Collateralize of pubkeysig:byte[]
    | Buy of pubkey:Hash
    | Exercise of pubkey:Hash
    | Close of pubkeysig:byte[]


let BadTx : Outpoint list * Output list * byte[] = [], [], [||]


let maybe = MaybeWorkflow.maybe

type InvokeMessage = byte * Outpoint list

let simplePackOutpoint : Outpoint -> byte list = fun p ->
    match p with
    | {txHash=txHash;index=index} ->
        if index > 255u then failwith "oops!"
        else
            (byte)index :: Array.toList txHash

let packManyOutpoints : Outpoint list -> byte list = fun ps ->
    List.concat (List.map simplePackOutpoint ps)

let tryParseInvokeMessage (message:byte[]) =
    let makeOutpoint (outpointb:byte[]) = {txHash=outpointb.[1..]; index = (uint32)outpointb.[0]}
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


type DataFormat = uint64 * uint64 * uint64 // tokens issued, quantity of collateral, authenticated use counter
let bytesToUInt64 : seq<byte> -> uint64 = fun bs ->
    bs |> Seq.indexed |> Seq.sumBy (fun (i, b) -> (uint64)b * pown 256UL i)


let tryParseData (data:seq<byte>) =
    maybe {
        let d = Seq.toArray data
        try
            if d.Length <> 24 then
                failwith "data of wrong length"
            let tokens, collateral, counter = d.[0..7], d.[8..15], d.[16..23]
            return (bytesToUInt64 tokens, bytesToUInt64 collateral, bytesToUInt64 counter)
        with _ ->
            return! None
    }

let returnToSender (opoint:Outpoint, oput:Output) = List.singleton opoint, List.singleton oput, Array.empty<byte>

let collateralize pubkey pubsig (spend:Spend) (data:byte[]) funds =
    maybe {
        let msg = Array.append [|0uy|] data.[16..]
        try
            if not <| verify pubsig msg pubkey then
                failwith "message not verified"
            if spend.asset <> [|0uy|] then failwith "bam"
        with _ ->
            return! None
    } |> ignore
    //verify pubsig msg pubkey |> fun b -> if b then () else ()
    ()


// Usage: Take three outpoints, only the first of which matters, and make a list.
// Then invoke packManyOutpoints to get the "message". Using this message as a
// witness will create an autotransaction which consumes the first outpoint
// and has one output, locked to whatever hash is in the first outpoint's data field.
let basicOption : ContractFunction = fun (message, contracthash, utxos) ->
    maybe {
        // parse message, obtaining opcode and three outpoints
        let! opcode, outpoints = tryParseInvokeMessage message
        let! commandLoc, dataLoc, fundsLoc =
            match outpoints with
            | [|a;b;c|] -> Some (a, b, c)
            | _ -> None
        // try to get the outputs. Fail early if they aren't there!
        let! commandOutput = utxos.TryFind commandLoc
        let! dataOutput = utxos.TryFind dataLoc
        let! fundsOutput = utxos.TryFind fundsLoc
        let! commandData, commandSpend =
            match commandOutput with
            | {
                lock=ContractLock (contractHash=contractHash; data=data);
                spend=spend
              } when contractHash=contracthash
                -> Some (data, spend)
            | _ -> None
        // whatever data is present is used as the return address of the spend
        let oput = {lock=PKLock commandData; spend=commandSpend}
        return returnToSender (commandLoc, oput)
    } |> Option.defaultValue BadTx

let callOptionFactory : CallOptionParameters -> ContractFunction = fun optParams (message,contracthash,utxos) ->
    let controlAsset = optParams.controlAsset
    let controlAssetReturn = optParams.controlAssetReturn
    let numeraire = optParams.numeraire
    let oracle = optParams.oracle
    let underlying = optParams.underlying
    let price = optParams.price
    let minimumCollateralRatio = optParams.minimumCollateralRatio
    let command = maybe {
        // parse message, obtaining opcode and three outpoints
        let! opcode, outpoints = tryParseInvokeMessage message
        let! commandLoc, dataLoc, fundsLoc =
            match outpoints with
            | [|a;b;c|] -> Some (a, b, c)
            | _ -> None
        // try to get the outputs. Fail early if they aren't there!
        let! commandOutput = utxos.TryFind commandLoc
        let! dataOutput = utxos.TryFind dataLoc
        let! fundsOutput = utxos.TryFind fundsLoc
        // the contract's data output must own the control token
        let! optionsOwnData =
            match dataOutput with
            | {
                lock=ContractLock (contractHash=contractHash; data=data);
                spend={asset=asset}
              } when contractHash = contracthash && asset = controlAsset
                -> Some <| data
            | _ -> None // short-circuiting
        // validate funds (to stop lying about amount of collateralization)
        let! tokens, collateral, counter = tryParseData optionsOwnData
        if fundsOutput.spend.asset <> numeraire || fundsOutput.spend.amount <> collateral
        then
            return! None
        // get the user's actual command
        let! commandData, commandSpend =
            match commandOutput with
            | {
                lock=ContractLock (contractHash=contractHash; data=data);
                spend=spend
              } when contractHash=contracthash
                -> Some (Array.toList data, spend)
            | _ -> None
        // opcodes must match
        let! opcodesMatch = match commandData with
                            | h::_ when h = opcode -> Some true
                            | _ -> None
        // parse command message - first byte identifies mode of use
        let! command =
            match commandData with
            | 0uy :: pubs when pubs.Length = 64 ->
                Some <| Collateralize (List.toArray pubs)
            | 1uy :: pubk when pubk.Length = 32 ->
                Some <| Buy (List.toArray pubk)
            | 2uy :: pubk when pubk.Length = 32 ->
                Some <| Exercise (List.toArray pubk)
            | 3uy :: pubs when pubs.Length = 64 ->
                Some <| Close (List.toArray pubs)
            | _ -> None
        match command with
        | Collateralize pubsig -> ()
        | Buy pubkey -> ()
        | Exercise pubkey -> ()
        | Close pubsig -> ()

        return command

    }
    maybe {
        let! cmd = command

        return! None //TODO
    } |> Option.defaultValue BadTx



let oracle : ContractFunction = fun (_,_,_) ->
    [], [], [||]

