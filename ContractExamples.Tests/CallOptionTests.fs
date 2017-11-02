module CallOptionTests

open NUnit.Framework
open ContractExamples
open ContractExamples.FStarCompatibility
open Consensus.Types
open Consensus.Serialization
open ContractExamples.QuotedContracts
open ContractExamples.Oracle
open ContractUtilities
open ContractExamples.Execution
open DataGenerator
open Tests

let createHash b : Hash = Array.map (fun x -> b) [|0..31|]

[<Test>]
let ``CallOptionStateShouldBeUninitalized``() =
    let utxos = []

    let result = DataGenerator.getCallOptionDataPOutput Consensus.Tests.zhash utxos

    Assert.That(true, Is.True)
    let x = Option.isNone result

    Assert.That (x, Is.True) 

[<Test>]
let ``CallOptionStateShouldInitalized``() =
    let serializer = context.GetSerializer<Zen.Types.Extracted.data<unit>>()
    let data = Zen.Types.Extracted.Data2 (1I, 1I, Zen.Types.Extracted.UInt32 1ul, Zen.Types.Extracted.UInt64 1UL)
    let bytes = serializer.PackSingleObject data
    let txHash = createHash 0uy
    let contractHash = createHash 1uy
    let utxos =
        [({ 
            Consensus.Types.txHash = txHash
            index = 199ul
        }, {
            Consensus.Types.lock = Consensus.Types.ContractLock (contractHash, bytes)
            spend =
            {
                asset = Consensus.Tests.zhash
                amount = 1UL
            }
        })]

    let result = DataGenerator.getCallOptionDataPOutput Consensus.Tests.zhash utxos

    Assert.That(true, Is.True)
    let x = Option.isSome result

    Assert.That (x, Is.True) 


//TODO: currently nor using template, but hard-coded values in fst file
let price = 100m
let callOptionMeta = CallOption {
    numeraire = Consensus.Tests.zhash;
    controlAsset = [||];
    controlAssetReturn = [||];
    oracle = [||];
    underlying = "AAPL";
    price = price;
    strike = 10m;
    minimumCollateralRatio = 1m;
    ownerPubKey = [||]
}

open Consensus.Serialization
let serializer = new DataSerializer(context)
context.Serializers.RegisterOverride<Zen.Types.Extracted.data<unit>>(serializer)

[<Test>]
let ``TestCallOptionBuy``() =
    let func, costFunc = getContractFunction "CallOption"

    let numeraire = Consensus.Tests.zhash
    let contractHash = createHash 5uy
    let stateTxHash = createHash 10uy
    let purchaseTxHash = createHash 20uy
    let purchaseAmount = 200UL
    let buyOpcode = 2uy
    let collateral = 1000UL
    let buyerKey = Wallet.core.Data.Key.Create()
    let stateData = Zen.Types.Extracted.UInt64Vector (3I, listToVector [ 0UL; collateral; 0UL ])
    let stateOutput = {
        lock = ContractLock (contractHash, serializer.PackSingleObject stateData)
        spend = {
                asset = numeraire
                amount = 1UL 
            } 
    }
    let stateOutpoint = { txHash = stateTxHash; index = 1ul }
    let purchaseOutput = {
        lock = ContractLock (contractHash, [||])
        spend = {
                asset = numeraire
                amount = purchaseAmount 
            } 
    }
    let purchaseOutpoint = { txHash = purchaseTxHash; index = 1ul }

    let utxos = Seq.ofList [(stateOutpoint, stateOutput)] //; (purchaseOutpoint, purchaseOutput)]
    let args = Map.ofList [("returnPubKeyAddress", buyerKey.Address.ToString())]

    let jsonResult = makeJson callOptionMeta utxos buyOpcode args

    let json = 
        match jsonResult with
        | Error result -> failwith ("couldn't get json: " + result.ToString())
        | Ok result -> result

    let message = makeMessage json purchaseOutpoint

    let utxo : ContractExamples.Execution.Utxo =
        function 
        | { txHash = txHash; index = _} when txHash = stateTxHash -> Some stateOutput
        | { txHash = txHash; index = _} when txHash = purchaseTxHash -> Some purchaseOutput
        | _ -> failwith "Outpoint not found"

    let result = func (message, contractHash, utxo)

    match result with 
    | Ok (outpoints, outputs, _) ->
        let tokensIssued = purchaseAmount / (uint64) price

        let stateMatcher = function
            | { lock = ContractLock (hash, bytes); spend = spend } when hash = contractHash && spend.asset = numeraire ->
                match serializer.UnpackSingleObject bytes with
                | Zen.Types.Extracted.UInt64Vector (l, vec) when l = 3I ->
                    let list = vectorToList vec
                    list.[0] = tokensIssued
                | _ -> false
            | _ -> false
        
        Assert.IsTrue (Option.isSome <| List.tryFind stateMatcher outputs)

        let purchaseMatcher = function
            | { lock = PKLock address; spend = spend } when 
                address = buyerKey.Address.Bytes && 
                spend.asset = contractHash && 
                spend.amount = tokensIssued -> true
            | _ -> false

        Assert.IsTrue (Option.isSome <| List.tryFind purchaseMatcher outputs)

    | Error msg -> failwith ("contract returned error: " + msg)
     
[<Test>]
let ``TestCallOptionExersize``() =
    let func, costFunc = getContractFunction "CallOption"

    let numeraire = Consensus.Tests.zhash
    let contractHash = createHash 5uy
    let stateTxHash = createHash 10uy
    let oracleTxHash = createHash 15uy
    let exersizeTxHash = createHash 20uy
    let exersizeAmount = 2UL
    let exersizeOpcode = 3uy
    let collateral = 100UL
    let appleSpot = 11m
    let buyerKey = Wallet.core.Data.Key.Create()
    let oracleAddress = Consensus.Tests.zhash

    let stateData = Zen.Types.Extracted.UInt64Vector (3I, listToVector [ 10UL; collateral; 1UL ])
    let stateOutput = {
        lock = ContractLock (contractHash, serializer.PackSingleObject stateData)
        spend = {
                asset = numeraire
                amount = 1UL 
            } 
    }
    let stateOutpoint = { txHash = stateTxHash; index = 1ul }
    let exersizeOutput = {
        lock = ContractLock (contractHash, [||])
        spend = {
                asset = contractHash
                amount = exersizeAmount 
            } 
    }
    let exersizeOutpoint = { txHash = exersizeTxHash; index = 1ul }

    let timestamp = 0L
    let tickerItem1 = {underlying = "AAPL"; price = appleSpot; timestamp = timestamp}
    let tickerItem2 = {underlying = "GOOG"; price = 9m; timestamp = timestamp}

    let (proofsMap, root) = commitments  (Seq.ofList [ tickerItem1; tickerItem2 ]) [| 0uy |]
    let oracleData = Zen.Types.Extracted.Hash root
    let oracleOutput = {
        lock = ContractLock (oracleAddress, serializer.PackSingleObject oracleData)
        spend = {
                asset = Consensus.Tests.zhash
                amount = 1UL
            } 
    }
    let oracleOutpoint = { txHash = oracleTxHash; index = 1ul }

    let utxos = Seq.ofList [(stateOutpoint, stateOutput)] //; (purchaseOutpoint, purchaseOutput)]

    let (proof, origin) = Map.find "AAPL" proofsMap
    let oracleRawData = rawDataTypedJson (proof, oracleOutpoint) origin
        
    let args = Map.ofList [
        ("returnPubKeyAddress", buyerKey.Address.ToString())
        ("oracleRawData", oracleRawData.ToString())
    ]

    let jsonResult = makeJson callOptionMeta utxos exersizeOpcode args

    let json = 
        match jsonResult with
        | Error result -> failwith ("couldn't get json: " + result.ToString())
        | Ok result -> result

    let message = makeMessage json exersizeOutpoint

    let utxo : ContractExamples.Execution.Utxo =
        function 
        | { txHash = txHash; index = _} when txHash = stateTxHash -> Some stateOutput
        | { txHash = txHash; index = _} when txHash = exersizeTxHash -> Some exersizeOutput
        | { txHash = txHash; index = _} when txHash = oracleTxHash -> Some oracleOutput
        | _ -> failwith "Outpoint not found"

    let result = func (message, contractHash, utxo)

    match result with 
    | Ok (outpoints, outputs, _) ->

        let strike = 5m //TODO: value is hardcoded in fst file
        let payoff = exersizeAmount * uint64 (appleSpot - strike)
        let stateMatcher = function
            | { lock = ContractLock (hash, bytes); spend = spend } when hash = contractHash && spend.asset = numeraire && spend.amount = collateral - payoff ->
                match serializer.UnpackSingleObject bytes with
                | Zen.Types.Extracted.UInt64Vector (l, vec) when l = 3I ->
                    let list = vectorToList vec
                    list.[0] = exersizeAmount
                    list.[1] = collateral - payoff
                | _ -> false
            | _ -> false

        Assert.IsTrue (Option.isSome <| List.tryFind stateMatcher outputs)

        let purchaseMatcher = function
            | { lock = PKLock address; spend = spend } when 
                address = buyerKey.Address.Bytes && 
                spend.asset = numeraire && 
                spend.amount = payoff -> true
            | _ -> false

        Assert.IsTrue (Option.isSome <| List.tryFind purchaseMatcher outputs)

    | Error msg -> failwith ("contract returned error: " + msg)
