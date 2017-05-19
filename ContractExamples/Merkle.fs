module ContractExamples.Merkle

let innerHash = Consensus.Merkle.innerHash

let merkleTree : byte[] [] -> byte[] [] [] = fun xs ->
    let l = xs.Length
    let n = if l <= 2 then 1 else
            Seq.init 32 (fun i -> pown 2 i ) |> Seq.findIndex (fun v -> v >= l)
    printfn "n is %i" n
    let items = Array.create <| pown 2 n <| [||]
    printfn "items.Length is %i" items.Length
    printfn "%O" <| items.[1]
    Array.blit xs 0 items 0 l
    let res = Array.zeroCreate<byte[][]> (n+1)
    res.[0] <- items
    for i = 1 to n do
        let v = res.[i-1]
        res.[i] <- v |> Array.chunkBySize 2 |> Array.map (fun xs -> innerHash (Array.append xs.[0] xs.[1]))
    res

let siblings : uint32 -> byte[] [] [] -> byte[] [] = fun r tree ->
    let sib height = tree.[height].[ (int)((r >>> height) ^^^ 1u) ]
    [| for i in 0 .. tree.Length - 1 do
        yield sib i
    |]

let auditPath r (tree:byte[][][]) = (tree.[0].[(int)r], r, siblings r tree)

let rootFromAuditPath (item, location, hashes) =
    Array.fold
            (fun (v, loc) h ->
                if loc % 2 = 0
                then
                    (innerHash <| Array.append v h, loc >>> 1)
                else
                    (innerHash <| Array.append h v, loc >>> 1))
            (item,location) hashes