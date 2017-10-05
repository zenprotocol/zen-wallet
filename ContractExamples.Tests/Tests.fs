module Tests

open NUnit.Framework
open ContractExamples
open ContractExamples.FStarCompatibility
open Consensus.Types
open Zen.Types.Extracted
open Consensus.Serialization
open System.IO

open ContractExamples.FStarExecution

let readContract fileName =
    System.IO.File.ReadAllText(Path.Combine (resolvePath "../../FStar", Path.ChangeExtension(fileName, ".fst")))

let getContractFunction contract =
    let extracted = readContract contract |> extract
    Assert.IsTrue ((Option.isSome extracted), "Should extract")

    let compiled = extracted |> Option.get |> compile 
    Assert.NotNull (compiled, "Should compile") // https://stackoverflow.com/questions/10435052/f-passing-none-to-function-getting-null-as-parameter-value
    Assert.That ((Option.isSome compiled), "Should compile")

    let func = compiled |> Option.get |> deserialize
    Assert.That ((Option.isSome func), "Should deserialize")

    Option.get func

let createHash b : Hash = Array.map (fun x -> b) [|0..31|]
       
//TODO: huh?
context.Serializers.RegisterOverride<data<unit>>(new DataSerializer(context))


open ContractExamples.QuotedContracts
open ContractUtilities
open ContractExamples.Execution
open ResultWorkflow

[<Test>]
let ``TestCallOptionMessageComposition``() =
    let func = getContractFunction "CallOption"

    let numeraire = createHash 0uy
    let contractHash = createHash 5uy
    let txHash = createHash 10uy
    let amount = 50UL

    let meta = CallOption {
        numeraire = Consensus.Tests.zhash;
        controlAsset = [||];
        controlAssetReturn = [||];
        oracle = [||];
        underlying = "GOOG";
        price = 10m;
        strike = 10m;
        minimumCollateralRatio = 1m;
        ownerPubKey = [||]
    }

    let utxos = []
        //[({ 
        //    Consensus.Types.txHash = txHash
        //    index = 199ul
        //}, {
        //    Consensus.Types.lock = Consensus.Types.PKLock contractHash
        //    spend =
        //    {
        //        asset = Consensus.Tests.zhash
        //        amount = amount
        //    }
        //})]

    let utxo : ContractExamples.Execution.Utxo =
        function 
        | { txHash = txHash; index = 55ul } when txHash = txHash -> 
            Some {
                lock = Consensus.Types.PKLock contractHash 
                spend = {
                        asset = numeraire
                        amount = amount 
                    } 
            }
        | _ -> None

    let args = Map.ofList [("", "");]

    let outpoint = { Consensus.Types.txHash = txHash; index = 55ul }

    // TODO: pyramid of doom!
    let checkStateCorrectness (state:Output, collateral, tokensIssued, counter) =
        if numeraire <> state.spend.asset then
            Error "Invalid asset"
        else
            if collateral <> state.spend.amount then
                Error "Invalid collateral"
            else
                match state.lock with
                | Consensus.Types.ContractLock (hash, data) ->
                    let serializer = context.GetSerializer<data<unit>>()
                    match serializer.UnpackSingleObject data with
                    | Data2 (_, _, UInt64 d1, UInt64 d2) ->
                        if tokensIssued <> d1 then
                            Error "Invalid tokens issued"
                        else 
                            if counter <> d2 then
                                Error "Invalid counter"
                            else
                                Ok true //
                    | _ -> Error "unexpected data"
                | _ -> Error "unexpected lock type"

    let getStateOutput (result:ContractResult) = 
        match result with 
        | Ok (outpointList, outputList, _) -> 
            if List.isEmpty outputList then
                Error "no outputs"
            else
                let fstOutput = outputList.[0]
                match fstOutput.lock with
                | Consensus.Types.ContractLock _ ->
                    Ok fstOutput
                | _ -> Error "unexpected lock type"
        | Error msg -> 
            Error ("contract responded with error message: " + msg)

    let result = result {
        match DataGenerator.makeJson (meta, utxos, 0uy, args) with 
        | Ok json ->
            let funcResult = func (DataGenerator.makeMessage (json, outpoint), contractHash, utxo)
            let! state = getStateOutput funcResult
            return! checkStateCorrectness (state, amount, 0UL, 1UL)
        | Error x -> return! Error ("Message generator error: " + x.ToString())
    }

    match result with 
    | Error x -> Assert.Fail x
    | _ -> ignore()
