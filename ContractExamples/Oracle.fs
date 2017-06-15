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

let commitments (items: TickerItem seq) (secret: byte[]) =
    let jsonOfTickerItem ({underlying=underlying;price=price;timestamp=timestamp} as item) =
        TickerJsonData.Root(underlying,price,timestamp).JsonValue
    let serializedTickerItem item = jsonOfTickerItem(item).ToString() |> System.Text.Encoding.ASCII.GetBytes
    let nonce (bs:byte[]) = innerHash (Array.append bs secret)
    let leaf (item:TickerItem) =
        let itemJson = jsonOfTickerItem item
        let nonceBytes = nonce (serializedTickerItem item)
        JsonValue.Record [|("item", itemJson);("nonce", JsonValue.String <| System.Convert.ToBase64String nonceBytes) |]
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
        typeof<Map<string,Merkle.AuditPath>>)

let pathToContractData (path:Merkle.AuditPath) =
    let serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof<Merkle.AuditPath>)
    use stream = new System.IO.MemoryStream()
    serializer.WriteObject(stream, path)
    stream.ToArray() |> System.Text.Encoding.ASCII.GetString

let priceTable (m:Map<string,Merkle.AuditPath>) =
    let price (bs:byte[]) =
        let item = ItemJsonData.Parse(System.Text.Encoding.ASCII.GetString bs)
        item.Item.Price
    let s = Map.toList m
    [ for (underlying, path) in s -> (underlying, price <| path.data)]

