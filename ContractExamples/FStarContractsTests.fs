module ContractExamples.FStarContractsTests

open NUnit.Framework
open FStarCompatibility
open Consensus.Types
open Zen.Types.Extracted
open Consensus.Serialization
open System.IO

open FStarExecution

let readContract fileName =
    System.IO.File.ReadAllText(Path.Combine (resolvePath "../../FStar", Path.ChangeExtension(fileName, ".fst")))

let getContractFunction contract =
    let extracted = readContract contract |> extract
    Assert.IsTrue ((Option.isSome extracted), "Should extract")

    let compiled = extracted |> Option.get |> compile 
    Assert.NotNull (compiled, "Should compile") // https://stackoverflow.com/questions/10435052/f-passing-none-to-function-getting-null-as-parameter-value
    Assert.That ((Option.isSome compiled), "Should compile")

    let mainFunc = compiled |> Option.get |> deserialize

    Assert.That ((Option.isSome mainFunc), "Should deserialize")

    match mainFunc with 
    | Some (mf, cf) -> mf, cf
    | _ -> failwith "Sould deserialize"


let createHash b : Hash = Array.map (fun x -> b) [|0..31|]
       
//TODO: huh?
context.Serializers.RegisterOverride<data<unit>>(new DataSerializer(context))

[<Test>]
let ``TestSecureToken``() =
    let mainFunc, costFunc = getContractFunction "SecureToken"

    let randomAddr = Array.map (fun x -> x*x) [|0uy..31uy|]
    let address = System.Convert.FromBase64String "AAEECRAZJDFAUWR5kKnE4QAhRGmQueQRQHGk2RBJhME="
    let randomhash = Array.map (fun x -> x*x) [|10uy..41uy|]
    let randomhash2 = Array.map (fun x -> x*x) [|100uy..131uy|]

    let utxo : ContractExamples.Execution.Utxo =
        fun outpoint -> 
            Some { 
                lock = Consensus.Types.PKLock outpoint.txHash; 
                spend = 
                    {
                        asset = randomhash2
                        amount = 1100UL 
                    } 
            }

    let outpoint = { txHash = randomhash; index = 550ul}

    let serializedOutpoint = context.GetSerializer<data<unit>>().PackSingleObject (Outpoint outpoint)

    let message = Array.append [|0uy|] serializedOutpoint 

    let input = (message, randomhash, utxo)

    let cost = costFunc input

    Assert.That (cost, Is.EqualTo 0)

    let resultSuccess = mainFunc input

    match resultSuccess with 
    | Ok (outpointList, outputList, data) -> 
        Assert.AreEqual ([], data)
        let outpoint = List.head outpointList
        Assert.AreEqual (550, outpoint.index)
        Assert.AreEqual (randomhash, outpoint.txHash)
  
        Assert.AreEqual (1000UL, outputList.[0].spend.amount)
        Assert.AreEqual (randomhash, outputList.[0].spend.asset)
        Assert.AreEqual (1100UL, outputList.[1].spend.amount)
        Assert.AreEqual (randomhash2, outputList.[1].spend.asset)

        let pkHash = 
            match outputList.[0].lock with 
            | Consensus.Types.PKLock (pkHash) -> pkHash
            | _ -> Array.empty
        Assert.AreEqual (randomAddr, pkHash)

        let pkHash = 
            match outputList.[1].lock with 
            | Consensus.Types.PKLock (pkHash) -> pkHash
            | _ -> Array.empty

        Assert.AreEqual (randomhash, pkHash)
    | Error msg -> Assert.Fail msg


    let utxoNone : ContractExamples.Execution.Utxo =
        fun outpoint -> None

    let resultCannotResolvedOutpoint = mainFunc (message, randomhash, utxoNone)

    match resultCannotResolvedOutpoint with
    | Ok _ -> Assert.Fail()
    | Error msg -> Assert.AreEqual ("Cannot resolve outpoint", msg)


[<Test>]
let ``TestCallOption``() =
    let mainFunc, costFunc = getContractFunction "CallOption"

    let numeraire = Consensus.Tests.zhash
    let contractHash = createHash 10uy

    let makeParams (cmd, amount, state) : Execution.ContractFunctionInput =
        let h0 = createHash 0uy
        let h1 = createHash 1uy

        let stateOutpoint = 
            match state with
            | Some _ ->
                Optional (1I, FStar.Pervasives.Native.option.Some (Outpoint { txHash = h1; index = 0ul }))
            | None ->
                Optional (1I, FStar.Pervasives.Native.option.None)

        let data = context.GetSerializer<data<unit>>().PackSingleObject (Data2 (1I, 1I, Outpoint { txHash = h0; index = 0ul }, stateOutpoint))

        let message = Array.append [|cmd|] data 

        let utxo : ContractExamples.Execution.Utxo =
            function 
            | { txHash = txHash; index = _ } when txHash = h0 -> 
                Some {
                    lock = Consensus.Types.PKLock contractHash 
                    spend = {
                            asset = numeraire
                            amount = amount 
                        } 
                }
            | { txHash = txHash; index = _ } when txHash = h1 -> state 
            | _ -> None

        (message, contractHash, utxo)
    
    let getStateOutput (result:ContractResult) = 
        match result with 
        | Ok (outpointList, outputList, _) -> 
            Assert.IsNotEmpty (outputList, "no outputs")
            let fstOutput = outputList.[0]
            match fstOutput.lock with
            | Consensus.Types.ContractLock _ ->
                Ok fstOutput
            | _ -> 
                Error "unexpected lock type"
        | Error msg -> 
            Error ("contract responded with error message: " + msg)

    let assertStateCorrectness (desc, state:Output, collateral, tokensIssued, counter) =
        Assert.AreEqual (numeraire, state.spend.asset)
        Assert.AreEqual (collateral, state.spend.amount, "collateral")
        match state.lock with
        | Consensus.Types.ContractLock (hash, data) ->
            let serializer = context.GetSerializer<data<unit>>()
            match serializer.UnpackSingleObject data with
            | Data2 (_, _, UInt64 d1, UInt64 d2) ->
                Assert.AreEqual (tokensIssued, d1, "tokens issued")
                Assert.AreEqual (counter, d2, "counter")
            | _ -> Assert.Fail "unexpected data"
        | _ -> Assert.Fail "unexpected lock type"


    let input = makeParams (0uy, 1100UL, None)
    let cost = costFunc input
    Assert.That (cost, Is.EqualTo 0I)

    let result = mainFunc input
    let state = getStateOutput result
    match state with
    | Error msg -> Assert.Fail msg
    | Ok stateOutput -> 
        assertStateCorrectness ("collateralize (no initial state)", stateOutput, 1100UL, 0UL, 1UL)

        let input = makeParams (0uy, 550UL, Some stateOutput)
        let cost = costFunc input
        Assert.That (cost, Is.EqualTo 0I)

        let result = mainFunc input
        let state = getStateOutput result
        match state with
        | Error msg -> Assert.Fail msg
        | Ok stateOutput -> 
            assertStateCorrectness ("collateralize (with initial state)", stateOutput, 1100UL + 550UL, 0UL, 2UL)
