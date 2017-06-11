module ContractUtilities.DataGenerator

open ContractExamples
open Consensus.Types
open FSharp.Data
open QuotedContracts
open Wallet.core.Data

let maybe = MaybeWorkflow.maybe

let getString = System.Convert.ToBase64String
let getBytes:string->byte[] = System.Convert.FromBase64String

[<Literal>]
let dataSamples = """
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
"""
type ContractJsonData = JsonProvider<dataSamples, SampleIsList=true>

let callOptionJson (meta:QuotedContracts.CallOptionParameters) (utxos:(Outpoint*Output) seq) opcode (m:Map<string,string>) =
    maybe {
        let! dataPair = Seq.tryFind (fun (_,y) -> y.spend.asset = meta.controlAsset) utxos
        let {lock=dataLock} = snd dataPair
        let! tokens, collateral, counter =
            match dataLock with
            | ContractLock (_, d) -> QuotedContracts.tryParseData d
            | _ -> None
        let! fundsPair = Seq.tryFind
                            (fun (_,y) ->
                                y.spend.asset = meta.numeraire &&
                                y.spend.amount = collateral)
                            utxos
        let! returnHash =
            maybe {
                let! returnPubKeyAddressStr = m.TryFind("returnPubKeyAddress")
                let returnPubKeyAddress = Address(returnPubKeyAddressStr)
                if returnPubKeyAddress.AddressType <> AddressType.PK then
                    return! None
                return returnPubKeyAddress.Bytes
            }
        match opcode with
        | 0uy ->
            return ContractJsonData.Root (
                ContractJsonData.StringOrFirst (
                    ContractJsonData.First (
                        uint64ToBytes counter |> Array.append [|0uy|] |> getString,
                        meta.ownerPubKey |> getString,
                        Array.append [|0uy|] returnHash |> getString
                    )
                   ),
                Some <| ContractJsonData.Second (
                    [|0uy|] |> getString,
                    packManyOutpoints [fst dataPair; fst fundsPair] |> getString
                )
            )
        | 1uy ->
            return ContractJsonData.Root (
                ContractJsonData.StringOrFirst (
                    Array.append [|1uy|] returnHash |> getString
                ),
                Some <| ContractJsonData.Second (
                    [|1uy|] |> getString,
                    packManyOutpoints [fst dataPair; fst fundsPair] |> getString
                )
            )
        | 2uy ->
            return ContractJsonData.Root (
                ContractJsonData.StringOrFirst (
                    Array.append [|2uy|] returnHash |> getString
                ),
                Some <| ContractJsonData.Second (
                    [|2uy|] |> getString,
                    packManyOutpoints [fst dataPair; fst fundsPair] |> getString
                )
            )
        | 3uy ->
            return ContractJsonData.Root (
                ContractJsonData.StringOrFirst (
                    ContractJsonData.First (
                        uint64ToBytes counter |> Array.append [|3uy|] |> getString,
                        meta.ownerPubKey |> getString,
                        Array.append [|3uy|] returnHash |> getString
                    )
                   ),
                Some <| ContractJsonData.Second (
                    [|3uy|] |> getString,
                    packManyOutpoints [fst dataPair; fst fundsPair] |> getString
                )
            )
        | _ -> return! None
    }


let makeData :  Execution.ContractMetadata -> (Outpoint*Output) seq -> byte -> Map<string,string> -> string option =
    fun meta utxos opcode m -> maybe {
        let! json =
            match meta with
            | Execution.Oracle _ -> None // Oracles not operated via contract website
            | Execution.CallOption meta ->
                callOptionJson meta utxos opcode m
            | Execution.SecureToken _ ->
                Some <|
                ContractJsonData.Root (
                        ContractJsonData.StringOrFirst (""),
                        Some <| ContractJsonData.Second (getString [|opcode|],""))
        return json.JsonValue.ToString ()
        }
