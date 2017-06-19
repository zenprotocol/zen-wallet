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

[<Literal>]
let oracleSample = """
{
  "auditPath": {
    "data": "ewogICJpdGVtIjogewogICAgInVuZGVybHlpbmciOiAiR09PRyIsCiAgICAicHJpY2UiOiA5NDAuNzIsCiAgICAidGltZXN0YW1wIjogNjM2MzMxNDgwOTc1NzY5NjIwCiAgfSwKICAibm9uY2UiOiAiWlJsWUw2M3FteklYd213c0xsYkhOeHF0dnFDWU9EU25tTjQ4WmNKcDZ2bz0iCn0=",
    "location": 0,
    "path": [
      "ewogICJpdGVtIjogewogICAgInVuZGVybHlpbmciOiAiR09PR0wiLAogICAgInByaWNlIjogOTU4LjYzLAogICAgInRpbWVzdGFtcCI6IDYzNjMzMTQ4MDk3NTc2OTYyMAogIH0sCiAgIm5vbmNlIjogIjNydGtlenlsaVNJK3hGUnlFbFJ5U3RuUldpUU1oM3hORzVwVW5wL1doSUU9Igp9",
      "0Dd1HaW1Xvab+tz727fXydPuyuhvrBDR/aAOz4sLISI=",
      "k/vjrP3C9O8U1LvDeREENVkjk3mazg/O1p2vjOQeNaI="
    ]
  },
  "outpoint": "ksQgi4nI1a+7LHQZgqcHzjLNnXKzJ9vBRCbkIz+tIYx2HPQB"
}
"""
type OracleJsonData = JsonProvider<oracleSample, SampleIsList=true>

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
            let! oracleRawData = m.TryFind "oracleRawData"
            let! oracleJson =
                try
                    Some <| OracleJsonData.Parse oracleRawData
                with _ -> None
            let orStr = oracleJson.Outpoint
            let! oracleOutpoint =
                try
                    Some <| (Consensus.TransactionValidation.guardedDeserialise<Outpoint> <| System.Convert.FromBase64String orStr)
                with _ -> None
            let auditPath =
                oracleJson.AuditPath.JsonValue.ToString() |>
                System.Text.Encoding.ASCII.GetBytes
            return ContractJsonData.Root (
                ContractJsonData.StringOrFirst (
                    Array.concat [[|2uy|]; returnHash; auditPath] |> getString
                ),
                Some <| ContractJsonData.Second (
                    [|2uy|] |> getString,
                    packManyOutpoints [fst dataPair; fst fundsPair; oracleOutpoint] |> getString
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
