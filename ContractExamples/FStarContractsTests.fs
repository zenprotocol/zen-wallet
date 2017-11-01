module ContractExamples.FStarContractsTests

open NUnit.Framework
open FStarCompatibility
open Consensus.Types
open Zen.Types.Extracted
open Consensus.Serialization
open System.IO

open FStarExecution

let readContract fileName =
    System.IO.File.ReadAllText(Path.Combine (rootPath "../../FStar", Path.ChangeExtension(fileName, ".fst")))

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

    Assert.That (cost, Is.EqualTo 45I)

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

        Assert.AreEqual (address, pkHash)
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

        let stateOutpoint = { txHash = h1; index = 11ul }
        let fundsOutpoint = { txHash = h0; index = 10ul }

        let data = 
            match state with
            | Some _ ->
                OutpointVector (2I, listToVector [ stateOutpoint; fundsOutpoint ])
            | None ->
                Outpoint fundsOutpoint 

        let bytes = context.GetSerializer<data<unit>>().PackSingleObject data

        let message = Array.append [|cmd|] bytes 

        let utxo : ContractExamples.Execution.Utxo =
            function 
            | { txHash = txHash; index = 10u } when txHash = h0 -> 
                Some {
                    lock = Consensus.Types.ContractLock (contractHash, [||])
                    spend = {
                            asset = numeraire
                            amount = amount 
                        } 
                }
            | { txHash = txHash; index = 11u } when txHash = h1 -> state 
            | x -> 
                printfn "unit-testing utxo fn returning none, query was %A" x
                None

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
            | UInt64Vector (_, v) ->
                let list = FStarCompatibility.vectorToList v
                Assert.AreEqual (tokensIssued, list.[0], "tokens issued, " + desc)
                Assert.AreEqual (collateral, list.[1], "collateral, " + desc)
                Assert.AreEqual (counter, list.[2], "counter, " + desc)
            | _ -> Assert.Fail ("unexpected data, " + desc)
        | _ -> Assert.Fail ("unexpected lock type, " + desc)


    let input = makeParams (0uy, 1100UL, None)
    let cost = costFunc input
    Assert.That (cost, Is.EqualTo 160I)

    let result = mainFunc input
    let state = getStateOutput result
    match state with
    | Error msg -> Assert.Fail msg
    | Ok stateOutput -> 
        let desc = "collateralize (no initial state)"
        assertStateCorrectness (desc, stateOutput, 1100UL, 0UL, 0UL)

        let input = makeParams (0uy, 550UL, Some stateOutput)
        let cost = costFunc input
        Assert.That (cost, Is.EqualTo 105I)

        let result = mainFunc input
        let state = getStateOutput result
        match state with
        | Error msg -> Assert.Fail (desc + ", " + msg)
        | Ok stateOutput -> 
            assertStateCorrectness ("collateralize (with initial state)", stateOutput, 1100UL + 550UL, 0UL, 1UL)


[<Test>]
let ``TestOracleContract``() =
    let mainFunc, costFunc = getContractFunction "OracleContract"

    let txHash = createHash 1uy
    let contractHash = createHash 2uy

    let utxo : ContractExamples.Execution.Utxo =
        fun outpoint -> 
            Some { 
                lock = Consensus.Types.ContractLock (contractHash, [||]); 
                spend = 
                    {
                        asset = Consensus.Tests.zhash
                        amount = 1100UL 
                    } 
            }

    let outpoint = { txHash = txHash; index = 550ul}

    let message = context.GetSerializer<data<unit>>().PackSingleObject (Data2 (1I, 1I, Outpoint outpoint, Hash (createHash 10uy)))

    let input = (Array.append [|0uy|] message, contractHash, utxo)

    let cost = costFunc input

    Assert.That (cost, Is.EqualTo 58I)

    let resultSuccess = mainFunc input

    match resultSuccess with 
    | Ok (outpointList, outputList, data) -> 
        Assert.AreEqual ([], data)
        let outpoint = List.head outpointList
        Assert.AreEqual (550, outpoint.index)
        Assert.AreEqual (txHash, outpoint.txHash)
  
        Assert.AreEqual (1100UL, outputList.[0].spend.amount)
        Assert.AreEqual (Consensus.Tests.zhash, outputList.[0].spend.asset)
        Assert.AreEqual (1UL, outputList.[1].spend.amount)
        Assert.AreEqual (contractHash, outputList.[1].spend.asset)

        let pkHash = 
            match outputList.[0].lock with 
            | Consensus.Types.ContractLock (hash, _) -> hash
            | _ -> Array.empty
        Assert.AreEqual (contractHash, pkHash)

        let pkHash = 
            match outputList.[1].lock with 
            | Consensus.Types.ContractLock (hash, _) -> hash
            | _ -> Array.empty

        Assert.AreEqual (contractHash, pkHash)
    | Error msg -> Assert.Fail msg