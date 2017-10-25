module CallOptionTests

open NUnit.Framework
open ContractUtilities
open ContractExamples.Execution
open Consensus.Serialization
open Consensus.Types

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


