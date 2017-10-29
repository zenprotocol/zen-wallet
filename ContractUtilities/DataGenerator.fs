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
  "origin": "some string",
  "outpoint": "ksQgi4nI1a+7LHQZgqcHzjLNnXKzJ9vBRCbkIz+tIYx2HPQB"
}
"""
type OracleJsonData = JsonProvider<oracleSample, SampleIsList=true>

type Result =  
    | ShouldHaveState
    | ShouldHaveReturnAddress
    | ShouldHaveValidReturnAddress
    | ShouldHaveOracleJson
    | ShouldParseOracleJson
    | ShouldParseOracleJsonOutpoint
    | InvalidCmd
    | ContractTypeNotImplemented

open Consensus.Serialization
//open Zen.Types.Extracted

context.Serializers.RegisterOverride<Zen.Types.Extracted.data<unit>>(new DataSerializer(context))

let getCallOptionDataPOutput numeraire (utxos:(Outpoint*Output) seq) =
    let matcher = fun (_, output) -> 
        match output.lock with
        | ContractLock (_, null) -> false
        | ContractLock (_, [||]) -> false
        | ContractLock (_, bytes) ->
            let data = context.GetSerializer<Zen.Types.Extracted.data<unit>>().UnpackSingleObject bytes
            match data with
            | Zen.Types.Extracted.UInt64Vector (l, _) when l = 3I ->
                output.spend.asset = numeraire
            | _ -> false
        | _ -> false

    Seq.tryFind matcher utxos

let stubFundsOutpoint = { Zen.Types.Extracted.txHash = Array.zeroCreate<byte>(32); Zen.Types.Extracted.index = System.UInt32.MaxValue }
let serializer = context.GetSerializer<Zen.Types.Extracted.data<unit>>()

let secureTokenOptionJson =
    let data = Zen.Types.Extracted.Outpoint stubFundsOutpoint

    result {
        return ContractJsonData.Root (
            ContractJsonData.StringOrFirst (""),
            Some <| ContractJsonData.Second (getString [|0uy|], data |> serializer.PackSingleObject |> getString)
        )
    }

let callOptionJson (meta:QuotedContracts.CallOptionParameters) (utxos:(Outpoint*Output) seq) opcode (m:Map<string,string>) =
    result {
        let stateOutput = getCallOptionDataPOutput meta.numeraire utxos

        match opcode with
        | 0uy ->
            let data = 
                match stateOutput with
                | Some (stateOutpoint, _) ->
                    Zen.Types.Extracted.OutpointVector (2I, listToVector [ fsToFstOutpoint stateOutpoint; stubFundsOutpoint ])
                | None ->
                    Zen.Types.Extracted.Outpoint stubFundsOutpoint

            return ContractJsonData.Root (
                ContractJsonData.StringOrFirst (""),
                Some <| ContractJsonData.Second (
                    [| opcode |] |> getString,
                    data |> serializer.PackSingleObject |> getString
                )
            )
        | 1uy ->
            let! (stateOutpoint, _) = (ShouldHaveState, stateOutput)
            let! addressStr = (ShouldHaveReturnAddress, m.TryFind("returnPubKeyAddress"))
            let! address = (ShouldHaveValidReturnAddress, try Some <| Address addressStr with _ -> None)

            let data = 
                Zen.Types.Extracted.Data2 (
                    2I, 
                    1I, 
                    Zen.Types.Extracted.OutpointVector (2I, listToVector [ fsToFstOutpoint stateOutpoint; stubFundsOutpoint ]),
                    Zen.Types.Extracted.OutputLock (Zen.Types.Extracted.PKLock address.Bytes))

            return ContractJsonData.Root (
                ContractJsonData.StringOrFirst (""),
                Some <| ContractJsonData.Second (
                    [| opcode |] |> getString,
                    data |> serializer.PackSingleObject |> getString
                )
            )
        | 2uy ->
            let! (stateOutpoint, _) = (ShouldHaveState, stateOutput)
            let! addressStr = (ShouldHaveReturnAddress, m.TryFind("returnPubKeyAddress"))
            let! address = (ShouldHaveValidReturnAddress, try Some <| Address addressStr with _ -> None)

            let! oracleRawData = (ShouldHaveOracleJson, m.TryFind "oracleRawData")
            let! oracleJson = (ShouldParseOracleJson, try Some <| OracleJsonData.Parse oracleRawData with _ -> None)
            let! oracleOutpoint = (ShouldParseOracleJsonOutpoint, try Some <| (Consensus.TransactionValidation.guardedDeserialise<Outpoint> <| System.Convert.FromBase64String oracleJson.Outpoint) with _ -> None)

            let originalCommitment = oracleJson.Origin |> getBytes |> serializer.UnpackSingleObject 

            //TODO: validate data matches expected pattern
            //match commitmentData with 
            //| Zen.Types.Extracted.Data4(32I, 1I, 1I, 1I, 
            //    Zen.Types.Extracted.ByteArray (32I, _), 
            //    Zen.Types.Extracted.UInt64 (uint64 _), 
            //    Zen.Types.Extracted.UInt64 (uint64 _), 
            //    Zen.Types.Extracted.Hash nonceBytes) -> true
            //| _ -> false

            let path = oracleJson.AuditPath.Path
            let pathLength = bigint path.Length
            let auditPath = 
                Zen.Types.Extracted.Data2(
                    1I, 
                    pathLength,
                    Zen.Types.Extracted.UInt32 (uint32 oracleJson.AuditPath.Location), 
                    Zen.Types.Extracted.HashArray (pathLength, Array.map getBytes path))

            let data = 
                Zen.Types.Extracted.Data4(
                    3I, 
                    32I + 1I + 1I + 1I,
                    1I + pathLength,
                    1I, 
                    Zen.Types.Extracted.OutpointVector (3I, listToVector [ fsToFstOutpoint stateOutpoint; stubFundsOutpoint; fsToFstOutpoint oracleOutpoint ]),
                    originalCommitment,
                    auditPath,
                    Zen.Types.Extracted.OutputLock (Zen.Types.Extracted.PKLock address.Bytes))

            let auditPath =
                oracleJson.AuditPath.JsonValue.ToString() |>
                System.Text.Encoding.ASCII.GetBytes

            return ContractJsonData.Root (
                ContractJsonData.StringOrFirst (""),
                Some <| ContractJsonData.Second (
                    [| opcode |] |> getString,
                    data |> serializer.PackSingleObject |> getString
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

let makeJson (meta:Execution.ContractMetadata) (utxos:(Outpoint*Output) seq) (opcode:byte) (m:Map<string,string>) =
    match meta with
    | Execution.CallOption meta -> callOptionJson meta utxos opcode m
    | Execution.SecureToken _ -> secureTokenOptionJson
    | _ -> Error ContractTypeNotImplemented

let makeMessage (json:ContractJsonData.Root) outpoint =
    let final = getBytes json.Second.Value.Final
    let data = serializer.UnpackSingleObject final
    let fundsOutpoint = outpoint |> fsToFstOutpoint
    let actualData = 
        match data with 
        | Zen.Types.Extracted.Outpoint o -> fundsOutpoint |> Zen.Types.Extracted.Outpoint
        | Zen.Types.Extracted.OutpointVector (l, vec) ->
            let list = vectorToList vec
            Zen.Types.Extracted.OutpointVector (l, listToVector [ list.[0]; fundsOutpoint ])
        | Zen.Types.Extracted.Data2 (l1, l2, Zen.Types.Extracted.OutpointVector (l3, vec), lock) ->
            let list = vectorToList vec
            let vec2 = Zen.Types.Extracted.OutpointVector (l3, listToVector [ list.[0]; fundsOutpoint ])
            Zen.Types.Extracted.Data2 (l1, l2, vec2, lock)
        | Zen.Types.Extracted.Data4 (l1, l2, l3, l4, Zen.Types.Extracted.OutpointVector (l, vec), originalCommitment, auditPath, lock) 
            when l1 = 3I && l2 = 32I + 1I + 1I + 1I && l4 = 1I ->
            let list = vectorToList vec
            let vec2 = Zen.Types.Extracted.OutpointVector (l3, listToVector [ list.[0]; fundsOutpoint; list.[2] ])
            Zen.Types.Extracted.Data4 (l1, l2, l3, l4, vec2, originalCommitment, auditPath, lock)
        


    let bytes = serializer.PackSingleObject actualData 

    let cmd = getBytes json.Second.Value.Initial
    Array.append cmd bytes 

let makeOracleMessage data outpoint = 
    let ser = context.GetSerializer<Zen.Types.Extracted.data<unit>>()
    
    let message = ser.PackSingleObject (Zen.Types.Extracted.Data2 (1I, 32I, Zen.Types.Extracted.Outpoint (fsToFstOutpoint outpoint), Zen.Types.Extracted.Hash data))

    Array.append [|0uy|] message
   