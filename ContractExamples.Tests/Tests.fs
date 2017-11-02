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
    System.IO.File.ReadAllText(Path.Combine (rootPath "../../../ContractExamples/FStar", Path.ChangeExtension(fileName, ".fst")))

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
let ``TestSecureTokenMessageComposition``() =
    let contractTpl = readContract "SecureToken"
    let mainFunc, costFunc = getContractFunction "SecureToken"

    let destination = System.Convert.FromBase64String "AAEECRAZJDFAUWR5kKnE4QAhRGmQueQRQHGk2RBJhME="
    let txHash = Array.map (fun x -> x*x) [|100uy..131uy|]
    let contractHash = Array.map (fun x -> x*x) [|0uy..31uy|]
    let zhash = Consensus.Tests.zhash
    
    let utxo : ContractExamples.Execution.Utxo =
        fun outpoint -> 
            Some { 
                lock = Consensus.Types.PKLock outpoint.txHash; 
                spend = 
                    {
                        asset = zhash
                        amount = 1UL 
                    } 
            }

    let outpoint = { Consensus.Types.txHash = txHash; index = 550ul}

    //let input = (message, randomhash, utxo)

    //let cost = costFunc input

    //Assert.That (cost, Is.EqualTo 0I)

    let meta = SecureToken {
        destination = destination;
    }

    let utxos = []
    let args = Map.ofList [("", "");]

    match DataGenerator.makeJson meta utxos 0uy args with 
    | Ok json ->
        let input = DataGenerator.makeMessage json outpoint
        let message = DataGenerator.makeMessage json outpoint
        let funcResult = mainFunc (message, contractHash, utxo)

        match funcResult with 
        | Ok (outpointList, outputList, data) -> 
            Assert.AreEqual ([], data)
            let outpoint = List.head outpointList
            Assert.AreEqual (550, outpoint.index)
            Assert.AreEqual (txHash, outpoint.txHash)
      
            let tokenOutput = outputList.[0]
            let connotativeOutput = outputList.[1]
            
            Assert.AreEqual (1000UL, tokenOutput.spend.amount)
            Assert.AreEqual (contractHash, tokenOutput.spend.asset)
            Assert.AreEqual (1UL, connotativeOutput.spend.amount)
            Assert.AreEqual (zhash, connotativeOutput.spend.asset)

            let pkHash = 
                match tokenOutput.lock with 
                | Consensus.Types.PKLock (pkHash) -> pkHash
                | _ -> Array.empty
            Assert.AreEqual (destination, pkHash)

            let pkHash = 
                match connotativeOutput.lock with 
                | Consensus.Types.PKLock (pkHash) -> pkHash
                | _ -> Array.empty

            Assert.AreEqual (destination, pkHash)
            
            let tx = {
                version = Consensus.Tests.tx.version;
                witnesses = [];
                inputs = outpointList;
                outputs = outputList;
                contract = None
            }
            
            let serializer = context.GetSerializer<Consensus.Types.Transaction>()
            let packedTx = serializer.PackSingleObject tx
            
            let unpackedTx = serializer.UnpackSingleObject packedTx
            
            Assert.AreEqual (tx, unpackedTx, "serialization assertion")
            
        | Error msg -> Assert.Fail msg


        //let utxoNone : ContractExamples.Execution.Utxo =
        //    fun outpoint -> None

        //let resultCannotResolvedOutpoint = mainFunc (message, randomhash, utxoNone)

        //match resultCannotResolvedOutpoint with
        //| Ok _ -> Assert.Fail()
        //| Error msg -> Assert.AreEqual ("Cannot resolve outpoint", msg)
    | Error x -> Assert.Fail <| x.ToString()
