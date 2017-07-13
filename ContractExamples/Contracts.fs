module ContractExamples.Contracts

open Consensus.Types
open Consensus.Authentication


type ContractFunctionInput = byte[] * Hash * (Outpoint -> Output option)
type TransactionSkeleton = Outpoint list * Output list * byte[]
type ContractFunction = ContractFunctionInput -> TransactionSkeleton

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
   
let BadTx : TransactionSkeleton = [], [], [||]


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

let (|Collateralize|_|) (data:byte[]) =
    maybe {
        let! opcode = Array.tryHead data
        if opcode <> 0uy then
            return! None
        if data.Length <> 97 then
            return! None // TODO: return to default instead
        return (data.[1..32], data.[33..96])
    }

let (|Buy|_|) (data:byte[]) =
    maybe {
        let! opcode = Array.tryHead data
        if opcode <> 1uy then
            return! None
        if data.Length <> 33 then
            return! None
        return data.[1..32]
    }

let (|Exercise|_|) (data:byte[]) =
    maybe {
        let! opcode = Array.tryHead data
        if opcode <> 2uy then
            return! None
        if data.Length <> 33 then
            return! None
        return data.[1..32]
    }

let (|Close|_|) (data:byte[]) =
    maybe {
        let! opcode = Array.tryHead data
        if opcode <> 3uy then
            return! None
        if data.Length <> 97 then
            return! None // TODO: return to default instead
        return (data.[1..32], data.[33..96])
    }

type DataFormat = uint64 * uint64 * uint64 // tokens issued, quantity of collateral, authenticated use counter
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

// TODO: time limits?
let collateralize :
          CallOptionParameters ->
          Outpoint * Outpoint * Outpoint -> 
          Output * Output * Output -> 
          uint64 * uint64 * uint64 ->
          Hash -> byte[] -> Hash -> TransactionSkeleton =
    fun optionParams
        ((x,y,z) as outpoints)
        ((b,d,f) as outputs)
        (tokens, collateral, counter)
        returnHash
        pubsig
        contractHash ->
            let optTx = maybe {
                let msg = Array.append [|0uy|] <| uint64ToBytes counter
                if not <| verify pubsig msg optionParams.ownerPubKey then
                    return! None
                if b.spend.asset <> optionParams.numeraire then
                    return! None
                let updated = (tokens, collateral + b.spend.amount, counter+1UL)
                let data = { d with lock=ContractLock (contractHash, makeData updated) }
                let funds = { f with spend={ f.spend with amount = f.spend.amount+b.spend.amount } }
                return ([x;y;z], [data;funds], [||])
            }
            match optTx with
            | Some tx -> tx
            | None ->
                let returnOutput = { lock=PKLock returnHash; spend=b.spend }
                returnToSender (x, returnOutput)

// TODO: collateral limits, use oracle, time limits
let buy : CallOptionParameters ->
          Outpoint * Outpoint * Outpoint -> 
          Output * Output * Output -> 
          uint64 * uint64 * uint64 ->
          Hash -> Hash -> TransactionSkeleton =
    fun optionParams outpoints outputs optionData pubkeyhash contracthash ->
        let optTx = maybe {
            let b, d, f = outputs
            if b.spend.asset <> optionParams.numeraire then
                return! None
            let! optionsPurchased =
                maybe { try
                            let res = ((decimal)b.spend.amount / optionParams.price) |> fun frac ->
                                if frac <= 0m then failwith "non-positive"
                                else frac |> floor |> (uint64)
                            return res
                        with _ -> return! None
                }
            if optionsPurchased = 0UL then
              return! None
            let tokens, collateral, counter = optionData
            let purchase = { lock=PKLock pubkeyhash; spend={ asset=contracthash;amount=optionsPurchased } }
            let data = { d with lock=ContractLock (
                                        contracthash,
                                        makeData (tokens + optionsPurchased,collateral + b.spend.amount,counter))}
            let funds = { f with spend={ f.spend with amount = f.spend.amount + b.spend.amount } }
            let outpointlist = match outpoints with (x,y,z) -> [x;y;z]
            return (outpointlist,[purchase; data; funds],[||])
        }
        match optTx with
        | Some tx -> tx
        | None ->
            match outpoints, outputs with
            | (buypoint, _, _), (buyput, _, _) ->
                let returnOutput = { lock=PKLock pubkeyhash; spend=buyput.spend }
                returnToSender (buypoint, returnOutput)

let exercise : CallOptionParameters ->
          Outpoint * Outpoint * Outpoint -> 
          Output * Output * Output -> 
          uint64 * uint64 * uint64 ->
          Hash -> Hash -> TransactionSkeleton =
    fun optionParams ((x,y,z) as outpoints) ((b,d,f) as outputs) (tokens, collateral, counter) pubkeyhash contracthash ->
        let optTx = maybe {
            if b.spend.asset <> contracthash then
                return! None
            let price = optionParams.price //TODO: oracle
            let collateralizedTokens = (decimal)collateral / price |> floor |> (uint64)
            if b.spend.amount > collateralizedTokens then // could create change, but it'd be a mess
                return! None
            let payoffamtd = (decimal)b.spend.amount * optionParams.price
            if payoffamtd <= 0m then return! None
            let payoffamt = payoffamtd |> floor |> (uint64)
            let remainingCollateral = collateral - payoffamt
            let payoff = { lock=PKLock pubkeyhash; spend={ asset=optionParams.numeraire; amount=payoffamt } }
            let data = { d with lock=ContractLock (
                                        contracthash,
                                        makeData (tokens-b.spend.amount, remainingCollateral, counter)) }
            let outputlist =
                if remainingCollateral <= 0UL then
                    [payoff;data]
                else
                    let funds = { f with spend={ f.spend with amount = remainingCollateral } }
                    [payoff;data;funds]
            return ([x;y;z], outputlist, [||])
        }
        match optTx with
        | Some tx -> tx
        | None ->
            let returnOutput = { lock=PKLock pubkeyhash; spend=b.spend }
            returnToSender (x, returnOutput)

// TODO: timelocks
let close : CallOptionParameters ->
        Outpoint * Outpoint * Outpoint -> 
        Output * Output * Output -> 
        uint64 * uint64 * uint64 ->
        Hash -> byte[] -> Hash -> TransactionSkeleton =
    fun optionParams ((x,y,z) as outpoints) ((b,d,f) as outputs) (tokens, collateral, counter) returnHash pubsig contracthash ->
        let optTx = maybe {
            let msg = Array.append [|3uy|] <| uint64ToBytes counter
            if not <| verify pubsig msg optionParams.ownerPubKey then
                return! None
            if b.spend.asset <> optionParams.numeraire then
                return! None
            let funds = {f with lock=PKLock returnHash}
            let control = {d with lock=PKLock returnHash}
            let returnOutput = {b with lock = PKLock returnHash}
            return ([x;y;z], [returnOutput;funds;control], [||])
        }
        match optTx with
        | Some tx -> tx
        | None ->
            let returnOutput = { lock=PKLock returnHash; spend=b.spend }
            returnToSender (x, returnOutput)
        
let basicOption : ContractFunction = fun (message, contracthash, utxos) ->
    maybe {
        let! opcode, outpoints = tryParseInvokeMessage message
        let! commandLoc = Array.tryHead outpoints   
        let! commandOutput = utxos commandLoc
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
        // send a contract token as well
        let cput = {lock=PKLock commandData; spend={asset=contracthash; amount=1000UL}}
        return ([commandLoc;],[oput; cput;],[||])
    } |> Option.defaultValue BadTx

let callOptionFactory : CallOptionParameters -> ContractFunction = fun optParams (message,contracthash,utxos) ->
    maybe {
        // parse message, obtaining opcode and three outpoints
        let! opcode, outpoints = tryParseInvokeMessage message
        let! commandLoc, dataLoc, fundsLoc =
            match outpoints with
            | [|a;b;c|] -> Some (a, b, c)
            | _ -> None
        // try to get the outputs. Fail early if they aren't there!
        let! commandOutput = utxos commandLoc
        let! dataOutput = utxos dataLoc
        let! fundsOutput = utxos fundsLoc
        // the contract's data output must own the control token
        let! optionsOwnData =
            match dataOutput with
            | {
                lock=ContractLock (contractHash=contractHash; data=data);
                spend={asset=asset}
              } when contractHash = contracthash && asset = optParams.controlAsset
                -> Some <| data
            | _ -> None // short-circuiting
        // validate funds (to stop lying about amount of collateralization)
        let! tokens, collateral, counter = tryParseData optionsOwnData
        if fundsOutput.spend.asset <> optParams.numeraire || fundsOutput.spend.amount <> collateral
        then
            return! None
        // get the user's actual command
        let! commandData, commandSpend =
            match commandOutput with
            | {
                lock=ContractLock (contractHash=contractHash; data=data);
                spend=spend
              } when contractHash=contracthash
                -> Some (data, spend)
            | _ -> None
        // opcodes must match
        let! commandOp = Array.tryHead commandData
        if opcode <> commandOp then
            return! None
        // switch on commands
        let! txskeleton =
            match commandData with
            | Collateralize (returnAddress, pubsig) ->
                Some <| collateralize
                            optParams
                            (commandLoc,dataLoc,fundsLoc)
                            (commandOutput, dataOutput, fundsOutput)
                            (tokens, collateral, counter)
                            returnAddress
                            pubsig
                            contracthash
            | Buy pubkeyhash ->
                Some <| buy
                            optParams
                            (commandLoc,dataLoc,fundsLoc)
                            (commandOutput, dataOutput, fundsOutput) 
                            (tokens, collateral, counter)
                            pubkeyhash
                            contracthash
            | Exercise pubkeyhash ->
                Some <| exercise
                            optParams
                            (commandLoc,dataLoc,fundsLoc)
                            (commandOutput, dataOutput, fundsOutput) 
                            (tokens, collateral, counter)
                            pubkeyhash
                            contracthash
            | Close (pubkeyhash, pubsig) ->
                Some <| close
                            optParams
                            (commandLoc,dataLoc,fundsLoc)
                            (commandOutput, dataOutput, fundsOutput) 
                            (tokens, collateral, counter)
                            pubkeyhash
                            pubsig
                            contracthash
            | _ ->
                None

        return txskeleton

    } |> Option.defaultValue BadTx

type OracleParameters =
    {
        ownerPubKey: byte[]
    }

let oracle : OracleParameters -> ContractFunction = fun oParams (message,contracthash,utxos)  ->
    maybe {
        if message.Length <> 129 then return! None
        let m, s = message.[0..64], message.[65..128]
        if not <| verify s m oParams.ownerPubKey then return! None
        let opoint = {txHash=m.[1..32]; index = (uint32)m.[0]}
        let! oput = utxos opoint
        let dataOutput = {
            spend={asset=contracthash; amount=1UL};
            lock=ContractLock (contracthash, m.[33..64])
        }
        return ([opoint;], [oput; dataOutput], [||])
    } |> Option.defaultValue BadTx

