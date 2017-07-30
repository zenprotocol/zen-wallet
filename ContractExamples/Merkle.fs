module ContractExamples.Merkle

let innerHash = Consensus.Merkle.innerHash

let merkleTree : byte[] [] -> byte[] [] [] = fun xs ->
    let l = xs.Length
    if l = 0 then failwith "Empty array"
    let n = if l <= 2 then 1 else
            Seq.init 32 (fun i -> pown 2 i ) |> Seq.findIndex (fun v -> v >= l)
    let initial = Array.create (pown 2 n) [||]
    Array.blit xs 0 initial 0 l
    let gen : byte [] [] -> (byte [] [] * byte [] []) option = fun ar ->
        if ar.Length = 0 then None
        elif ar.Length = 1 then Some (ar,[||])
        else
            Array.chunkBySize 2 ar |>
                Array.map (fun bs -> innerHash <| Array.concat bs) |>
                (fun r -> Some (ar, r))
    let res = Seq.unfold gen initial
    res |> Seq.toArray


let siblings : uint32 -> byte[] [] [] -> byte[] [] = fun r tree ->
    let sib height = tree.[height].[ (int)((r >>> height) ^^^ 1u) ]
    [| for i in 0 .. tree.Length - 2 do
        yield sib i
    |]

//TODO: remove references (in other projects)
//type AuditPath = {data:byte[]; location:uint32; path: byte [] []}

let auditPath r (tree:byte[][][]) = (tree.[0].[(int)r], r, siblings r tree)
                      
let rootFromAuditPath (item, location, hashes) =
    fst <|
    Array.fold
            (fun (v, loc) h ->
                if loc % 2u = 0u
                then
                    (innerHash <| Array.append v h, loc >>> 1)
                else
                    (innerHash <| Array.append h v, loc >>> 1))
            (item,location) hashes
