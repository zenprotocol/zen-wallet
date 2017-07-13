module ContractUtilities.DataGenerator
[<Literal>]
val dataSamples : string = """
[
    {
        "first": "blah"
    },
    {
        "first": "blah",
        "second": {
            "initial": "bbb",
            "final": "ccc"
        }
    },
    {
        "first": {
            "toSign": "rahrah",
            "pubkey": "blahblah",
            "data": "yahyah"
        },
        "second": {
            "initial": "bbb",
            "final": "ccc"
        }
    }
]
""";;

type ContractJsonData =
    FSharp.Data.JsonProvider<dataSamples, SampleIsList=true>
val makeData :
    meta:ContractExamples.Execution.ContractMetadata ->
    utxos:seq<Consensus.Types.Outpoint * Consensus.Types.Output> ->
    opcode:byte -> m:Map<string,string> -> string option



