module ContractExamples.Oracle

open FSharp.Data
let innerHash = Consensus.Merkle.innerHash

type TickerItem = {underlying:string; price:decimal;timestamp:int64}

[<Literal>]
let tickerSample =
    """{"underlying":"GOOG","price":123.12,"timestamp":12312312311}""" 
type TickerJsonData = JsonProvider<tickerSample, SampleIsList=false>

let commitments (items: TickerItem seq) (secret: byte[]) =
    let toJson ({underlying=underlying;price=price;timestamp=timestamp} as item) =
        TickerJsonData.Root(underlying,price,timestamp).JsonValue.ToString() |> System.Text.Encoding.ASCII.GetBytes
    let nonce (bs:byte[]) = innerHash (Array.append bs secret)
    let leafData = [|
        for item in items ->
            Array.append (toJson item) (nonce (toJson item))
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