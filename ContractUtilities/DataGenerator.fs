module ContractUtilities.DataGenerator

open ContractExamples
open Consensus.Types
open FSharp.Data
open QuotedContracts
open Wallet.core.Data
open ResultWorkflow
open FStar.Pervasives
open ContractExamples.FStarCompatibility

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

type Result =  
    | ShouldHaveControlAsset
    | ShouldHavePubKeyAddress
    | InvalidReturnAddress
    | MissingDataLock
    | InvalidCmd
    | ContractTypeNotImplemented

open Consensus.Serialization

context.Serializers.RegisterOverride<Zen.Types.Extracted.data<unit>>(new DataSerializer(context))

let callOptionJson (meta:QuotedContracts.CallOptionParameters) (utxos:(Outpoint*Output) seq) opcode (m:Map<string,string>) =
    result {
        let stateOutput = Seq.tryFind (fun (_,y) -> y.spend.asset = meta.numeraire) utxos

        let stateOutpoint = 
            match stateOutput with
            | Some (o, _) ->
                Zen.Types.Extracted.Optional (1I, Native.option.Some (Zen.Types.Extracted.Outpoint (fsToFstOutpoint o)))
            | None ->
                Zen.Types.Extracted.Optional (1I, FStar.Pervasives.Native.option.None)

        let bytes = context.GetSerializer<Zen.Types.Extracted.data<unit>>().PackSingleObject stateOutpoint
    

        match opcode with
        | 0uy ->
            return ContractJsonData.Root (
                ContractJsonData.StringOrFirst (""),
                Some <| ContractJsonData.Second (
                    [| opcode |] |> getString,
                    bytes |> getString
                )
            )

 


        //let! dataPair = (ShouldHaveControlAsset, Seq.tryFind (fun (_,y) -> y.spend.asset = meta.controlAsset) utxos)
        //let {lock=dataLock} = snd dataPair
        //let! returnHash =
        //    result {
        //        let! returnPubKeyAddressStr = (ShouldHavePubKeyAddress, m.TryFind("returnPubKeyAddress"))
        //        let returnPubKeyAddress = Address(returnPubKeyAddressStr)
        //        if returnPubKeyAddress.AddressType <> AddressType.PK then
        //            return! Error InvalidReturnAddress
        //        return returnPubKeyAddress.Bytes
        //    }
        //let initialized =
        //    match dataLock with
        //    | ContractLock (_, d) when d.Length = 0 -> false
        //    | _ -> true
        //if not initialized && opcode = 0uy then
        //    return initializeCall meta returnHash dataPair
        //else
        //let! tokens, collateral, counter = (MissingDataLock, match dataLock with
        //                                                     | ContractLock (_, d) -> QuotedContracts.tryParseData d
        //                                                     | _ -> None)
        //let! fundsPair = (ShouldHaveCollateralThing, Seq.tryFind
        //                    (fun (_,y) ->
        //                        y.spend.asset = meta.numeraire &&
        //                        y.spend.amount = collateral)
        //                    utxos)
        //match opcode with
        //| 0uy ->
            //return ContractJsonData.Root (
            //    ContractJsonData.StringOrFirst (
            //        ContractJsonData.First (
            //            uint64ToBytes counter |> Array.append [|0uy|] |> getString,
            //            meta.ownerPubKey |> getString,
            //            Array.append [|0uy|] returnHash |> getString
            //        )
            //       ),
            //    Some <| ContractJsonData.Second (
            //        [|0uy|] |> getString,
            //        packManyOutpoints [fst dataPair; fst fundsPair] |> getString
            //    )
            //)
        //| 1uy ->
        //    return ContractJsonData.Root (
        //        ContractJsonData.StringOrFirst (
        //            Array.append [|1uy|] returnHash |> getString
        //        ),
        //        Some <| ContractJsonData.Second (
        //            [|1uy|] |> getString,
        //            packManyOutpoints [fst dataPair; fst fundsPair] |> getString
        //        )
        //    )
        //| 2uy ->
        //    let! oracleRawData = m.TryFind "oracleRawData"
        //    let! oracleJson =
        //        try
        //            Some <| OracleJsonData.Parse oracleRawData
        //        with _ -> None
        //    let orStr = oracleJson.Outpoint
        //    let! oracleOutpoint =
        //        try
        //            Some <| (Consensus.TransactionValidation.guardedDeserialise<Outpoint> <| System.Convert.FromBase64String orStr)
        //        with _ -> None
        //    let auditPath =
        //        oracleJson.AuditPath.JsonValue.ToString() |>
        //        System.Text.Encoding.ASCII.GetBytes
        //    return ContractJsonData.Root (
        //        ContractJsonData.StringOrFirst (
        //            Array.concat [[|2uy|]; returnHash; auditPath] |> getString
        //        ),
        //        Some <| ContractJsonData.Second (
        //            [|2uy|] |> getString,
        //            packManyOutpoints [fst dataPair; fst fundsPair; oracleOutpoint] |> getString
        //        )
        //    )
        //| 3uy ->
            //return ContractJsonData.Root (
            //    ContractJsonData.StringOrFirst (
            //        ContractJsonData.First (
            //            uint64ToBytes counter |> Array.append [|3uy|] |> getString,
            //            meta.ownerPubKey |> getString,
            //            Array.append [|3uy|] returnHash |> getString
            //        )
            //       ),
            //    Some <| ContractJsonData.Second (
            //        [|3uy|] |> getString,
            //        packManyOutpoints [fst dataPair; fst fundsPair] |> getString
            //    )
            //)
        | _ -> return! Error InvalidCmd
    }

let parseJson = ContractJsonData.Parse

let makeJson (meta:Execution.ContractMetadata, utxos:(Outpoint*Output) seq, opcode:byte, m:Map<string,string>) =
    match meta with
    | Execution.CallOption meta -> callOptionJson meta utxos opcode m
   //| Execution.SecureToken _ ->
       //ContractJsonData.Root (
       //ContractJsonData.StringOrFirst (""),
       //Some <| ContractJsonData.Second (getString [|opcode|],""))
    | _ -> Error ContractTypeNotImplemented
    


let makeMessage (json:ContractJsonData.Root, outpoint) =
    let ser = context.GetSerializer<Zen.Types.Extracted.data<unit>>()
    let jsonData = ser.UnpackSingleObject (getBytes json.Second.Value.Final)
    let wrappingData = Zen.Types.Extracted.Data2 (1I, 1I, Zen.Types.Extracted.Outpoint (fsToFstOutpoint outpoint), jsonData)
    let bytes = ser.PackSingleObject (wrappingData)
    let cmd = getBytes json.Second.Value.Initial
    Array.append cmd bytes 