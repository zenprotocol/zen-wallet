module ContractExamples.Oracle

open Newtonsoft.Json.Linq
let innerHash = Consensus.Merkle.innerHash
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
        typeof<Map<string,byte[] * uint32 * byte[][]>>)

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

let pathData = pathToTypedJson >> (fun d -> d.ToString())

let rawDataTypedJson (path:byte[] * uint32 * byte[][], outpoint:Outpoint) =
    let opnt = Consensus.Merkle.serialize outpoint |> System.Convert.ToBase64String
    new JObject(
        [
            new JProperty("auditPath", pathToTypedJson path);
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

let rawData = rawDataTypedJson >> (fun d -> d.ToString())

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

let priceTable (m:Map<string,byte[] * uint32 * byte[][]>) =
    let price (bs:byte[]) =
        let item = JObject.Parse(System.Text.Encoding.ASCII.GetString bs)
        item.Item("item").Item("price").Value<decimal>()
    let s = Map.toList m
    [ for (underlying, path) in s -> (underlying, price <| match path with | (data,_,_) -> data)]

