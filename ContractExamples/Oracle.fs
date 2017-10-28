module ContractExamples.Oracle

open Newtonsoft.Json.Linq
open Zen.Types

let innerHash = Consensus.Merkle.innerHash
type Outpoint = Consensus.Types.Outpoint
let deserializeOutpoint = Consensus.TransactionValidation.guardedDeserialise<Outpoint>

type TickerItem = {underlying:string; price:decimal;timestamp:int64}

open Consensus.Serialization
open FStarCompatibility
let serializer = new DataSerializer(context)
context.Serializers.RegisterOverride<Zen.Types.Extracted.data<unit>>(serializer)

let commitments (items: TickerItem seq) (secret: byte[]) =
    let jsonOfTickerItem ({underlying=underlying;price=price;timestamp=timestamp} as item) =
        //ItemJsonData.Item(underlying,price,timestamp)
        new JObject([new JProperty("underlying", underlying); new JProperty("price", price); new JProperty("timestamp", timestamp)])
    let serializedTickerItem item = 
        jsonOfTickerItem(item).ToString() |> System.Text.Encoding.ASCII.GetBytes
    let nonceB (bs:byte[]) = innerHash (Array.append bs secret)
    let leafData item = 
        let nonceBytes = nonceB (serializedTickerItem item)
        let underlying = System.Text.Encoding.ASCII.GetBytes item.underlying
        let underlyingBytes = Array.append underlying (Array.zeroCreate<byte>(32 - (Array.length underlying)))
        Extracted.Data4(32I, 1I, 1I, 1I, 
            Extracted.ByteArray (32I, underlyingBytes), 
            Extracted.UInt64 (uint64 item.price * 1000UL), 
            Extracted.UInt64 (uint64 item.timestamp), 
            Extracted.Hash nonceBytes)
    let leaf (item:TickerItem) =
        item
        |> leafData
        |> Zen.Merkle.serialize 
        |> Option.map innerHash
        |> Option.get //TODO

    let tree = Seq.map leaf items |> Seq.toArray |> Merkle.merkleTree
    let root = (Array.last tree).[0]
    let auditPaths = seq {
        for i in 0 .. Seq.length items - 1 ->
            Merkle.auditPath (uint32 i) tree
            }
    let datas = Seq.map (leafData >> serializer.PackSingleObject) items
    let underlyings = Seq.map (fun x -> x.underlying) items
    let proofsMap = Map.ofSeq <| Seq.zip underlyings (Seq.zip auditPaths datas)
    (proofsMap, root)

let proofMapSerializer =
    System.Runtime.Serialization.Json.DataContractJsonSerializer(
        typeof<Map<string,(byte[] * uint32 * byte[][]) * byte[]>>)

let pathToTypedJson (path:byte[] * uint32 * byte[][]) =
    let (data, loc, pa) = 
        match path with
        | (data, loc, pa) -> System.Convert.ToBase64String data, int64 loc, Array.map (System.Convert.ToBase64String) pa
    new JObject(
        [
            new JProperty("data", data);
            new JProperty("location", loc);
            new JProperty("path", pa)
        ]
    )

//let pathData = pathToTypedJson >> (fun d -> d.ToString())

let rawDataTypedJson (path:byte[] * uint32 * byte[][], outpoint:Outpoint) origin =
    let opnt = Consensus.Merkle.serialize outpoint |> System.Convert.ToBase64String
    new JObject(
        [
            new JProperty("auditPath", pathToTypedJson path);
            new JProperty("origin", System.Convert.ToBase64String origin);
            new JProperty("outpoint", opnt)
        ]
    )
let fromPath (s:string) : byte[] * uint32 * byte[][] =
    let raw = JObject.Parse s

    let jsonPaths = raw.Item("path").Children()
    let paths = Seq.toArray <| Seq.map<JToken, string> (fun x -> x.Value<string>()) jsonPaths

    (
        System.Convert.FromBase64String <| raw.Item("data").Value<string>(),
        uint32 <| raw.Item("location").Value<string>(),
        Array.map (System.Convert.FromBase64String) <| paths
    )

//let rawData = rawDataTypedJson >> (fun d -> d.ToString())

let fromRawData (s:string) : ((byte[] * uint32 * byte[][]) * Outpoint) =
    let raw = JObject.Parse(s)
    let rawAuditPath = raw.Item("auditPath")
    let rawOutpoint = raw.Item("outpoint").Value<string>()
    let auditPath:byte[] * uint32 * byte[][] = (
        System.Convert.FromBase64String <| rawAuditPath.Item("data").Value<string>(),
        uint32 <| rawAuditPath.Item("location").Value<string>(),
        Array.map (System.Convert.FromBase64String) <| rawAuditPath.Item("path").Value<string[]>()
    )
    let outpoint = rawOutpoint |> System.Convert.FromBase64String |> deserializeOutpoint
    (auditPath, outpoint)

let priceTable m =
    let price (bs:byte[]) =
        let data = serializer.UnpackSingleObject bs
        match data with 
        | Extracted.Data4(l1, l2, l3, l4, Extracted.ByteArray (l5, _), Extracted.UInt64 price, Extracted.UInt64 _, Extracted.Hash _) when l1 = 32I && l2 = 1I && l3 = 1I && l4 = 1I && l5 = 32I 
            -> (decimal price) / 1000m
        | _ -> 0m
//        let item = JObject.Parse(System.Text.Encoding.ASCII.GetString bs)
//        item.Item("item").Item("price").Value<decimal>()
    let s = Map.toList m
    [ for (underlying, path) in s -> (underlying, price <| match path with | (_, data) -> data)]

