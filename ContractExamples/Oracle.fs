module ContractExamples.Oracle

open FSharp.Data
let innerHash = Consensus.Merkle.innerHash

type TickerItem = {underlying:string; price:decimal;timestamp:int64}

[<Literal>]
let tickerSample =
    """{"underlying":"GOOG","price":123.12,"timestamp":12312312311}""" 
type TickerJsonData = JsonProvider<tickerSample, SampleIsList=false>

[<Literal>]
let itemSample =
    """{
        "item":{"underlying":"GOOG","price":123.12,"timestamp":12312312311},
        "nonce":"a32543452521452"
        }""" 
type ItemJsonData = JsonProvider<itemSample, SampleIsList=false>

[<Literal>]
let rawSample =
    """{
        "auditPath": {
            "data": "5654aaoeuaoe52345234OUEA",
            "location": 3242433330,
            "path": ["5uejaeuao","axydd5454","aoeu43333","aoeuajk324","aekka444"]
        },
        "outpoint": "5ab534AAAOEUAAOEAA"
    }"""
type RawJsonData = JsonProvider<rawSample,SampleIsList=false>

let commitments (items: TickerItem seq) (secret: byte[]) =
    let jsonOfTickerItem ({underlying=underlying;price=price;timestamp=timestamp} as item) =
        ItemJsonData.Item(underlying,price,timestamp)
    let serializedTickerItem item = jsonOfTickerItem(item).JsonValue.ToString() |> System.Text.Encoding.ASCII.GetBytes
    let nonceB (bs:byte[]) = innerHash (Array.append bs secret)
    let leaf (item:TickerItem) =
        let itemJson = jsonOfTickerItem item
        let nonceBytes = nonceB (serializedTickerItem item)
        let nonce = System.Convert.ToBase64String nonceBytes
        ItemJsonData.Root(itemJson,nonce)
    let leafData = [|
        for item in items ->
            (leaf item).JsonValue.ToString() |> System.Text.Encoding.ASCII.GetBytes
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
        typeof<Map<string,Merkle.AuditPath>>)

let pathToTypedJson (path:Merkle.AuditPath) =
    let (data, loc, pa) = 
        (System.Convert.ToBase64String path.data, int64 path.location, Array.map (System.Convert.ToBase64String) path.path)
    RawJsonData.AuditPath(data, loc, pa)

let pathData = pathToTypedJson >> (fun d -> d.JsonValue.ToString())

let rawDataTypedJson (path:Merkle.AuditPath, outpoint:Consensus.Types.Outpoint) =
    let opnt = Consensus.Merkle.serialize outpoint |> System.Convert.ToBase64String
    RawJsonData.Root(pathToTypedJson path, opnt)

let rawData = rawDataTypedJson >> (fun d -> d.JsonValue.ToString())

let priceTable (m:Map<string,Merkle.AuditPath>) =
    let price (bs:byte[]) =
        let item = ItemJsonData.Parse(System.Text.Encoding.ASCII.GetString bs)
        item.Item.Price
    let s = Map.toList m
    [ for (underlying, path) in s -> (underlying, price <| path.data)]

