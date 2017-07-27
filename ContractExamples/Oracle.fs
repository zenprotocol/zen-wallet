module ContractExamples.Oracle

open FSharp.Data
open Newtonsoft.Json.Linq
let innerHash = Consensus.Merkle.innerHash
type AuditPath = Merkle.AuditPath
type Outpoint = Consensus.Types.Outpoint
let deserializeOutpoint = Consensus.TransactionValidation.guardedDeserialise<Outpoint>

type TickerItem = {underlying:string; price:decimal;timestamp:int64}

let commitments (items: TickerItem seq) (secret: byte[]) =
    let jsonOfTickerItem ({underlying=underlying;price=price;timestamp=timestamp} as item) =
        //ItemJsonData.Item(underlying,price,timestamp)
        new JObject([new JProperty("underlying", underlying); new JProperty("price", price); new JProperty("timestamp", timestamp)])
    let serializedTickerItem item = 
        jsonOfTickerItem(item).ToString() |> System.Text.Encoding.ASCII.GetBytes
    let nonceB (bs:byte[]) = innerHash (Array.append bs secret)
    let leaf (item:TickerItem) =
        let itemJson = jsonOfTickerItem item
        let nonceBytes = nonceB (serializedTickerItem item)
        let nonce = System.Convert.ToBase64String nonceBytes
        //ItemJsonData.Root(itemJson,nonce)
        new JObject(
            [
                new JProperty("item", itemJson);
                new JProperty("nonce", nonce)
            ]
        )
    let leafData = [|
        for item in items ->
            (leaf item).ToString() |> System.Text.Encoding.ASCII.GetBytes
            |]
    let tree = Merkle.merkleTree leafData
    let auditPaths = seq {
        for i in 0 .. Seq.length items - 1 ->
            Merkle.auditPath (uint32 i) tree
            }
    let proofs = Map.ofSeq <| Seq.zip (seq { for item in items -> item.underlying }) auditPaths
    (proofs, tree |> Array.last |> (fun x -> x.[0]))

let proofMapSerializer =
    System.Runtime.Serialization.Json.DataContractJsonSerializer(
        typeof<Map<string,AuditPath>>)

let pathToTypedJson (path:AuditPath) =
    let (data, loc, pa) = 
        (System.Convert.ToBase64String path.data, int64 path.location, Array.map (System.Convert.ToBase64String) path.path)
    new JObject(
        [
            new JProperty("data", data);
            new JProperty("location", loc);
            new JProperty("path", pa)
        ]
    )

let pathData = pathToTypedJson >> (fun d -> d.ToString())

let rawDataTypedJson (path:AuditPath, outpoint:Outpoint) =
    let opnt = Consensus.Merkle.serialize outpoint |> System.Convert.ToBase64String
    new JObject(
        [
            new JProperty("auditPath", pathToTypedJson path);
            new JProperty("outpoint", opnt)
        ]
    )
let fromPath (s:string) : AuditPath =
    let raw = JObject.Parse s

    let jsonPaths = raw.Item("path").Children()
    let paths = Seq.toArray <| Seq.map<JToken, string> (fun x -> x.Value<string>()) jsonPaths

    {
        data = System.Convert.FromBase64String <| raw.Item("data").Value<string>();
        location = uint32 <| raw.Item("location").Value<string>();
        path = Array.map (System.Convert.FromBase64String) <| paths
    }

let rawData = rawDataTypedJson >> (fun d -> d.ToString())

let fromRawData (s:string) : (AuditPath * Outpoint) =
    let raw = JObject.Parse(s)
    let rawAuditPath = raw.Item("auditPath")
    let rawOutpoint = raw.Item("outpoint").Value<string>()
    let auditPath:AuditPath = {
        data = System.Convert.FromBase64String <| rawAuditPath.Item("data").Value<string>();
        location = uint32 <| rawAuditPath.Item("location").Value<string>();
        path = Array.map (System.Convert.FromBase64String) <| rawAuditPath.Item("path").Value<string[]>()
        }
    let outpoint = rawOutpoint |> System.Convert.FromBase64String |> deserializeOutpoint
    (auditPath, outpoint)

let priceTable (m:Map<string,Merkle.AuditPath>) =
    let price (bs:byte[]) =
        let item = JObject.Parse(System.Text.Encoding.ASCII.GetString bs)
        item.Item("item").Item("price")
    let s = Map.toList m
    [ for (underlying, path) in s -> (underlying, price <| path.data)]

