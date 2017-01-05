module Consensus.SparseMerkleTreeTests

open NUnit.Framework
open NUnit.Framework.Constraints
open System

open Tree
open SparseMerkleTree

let testSize = 256

let testSerializer = id<byte[]>
let treeconst = "treeconst!!!"B
let testSMT = emptySMT<byte[]> treeconst testSerializer

let firstKey = (Merkle.bitsToBytes zeroLoc)
firstKey.SetValue(61uy,TSize/8 /2)
let firstValue = "the first value"B

let defLocation = {loc=zeroLoc;height=TSize}

[<Test>]
let ``Empty SMT has no keys``() = 
    Assert.That(testSMT.kv.Count, Is.Zero)

[<Test>]
let ``Empty SMT has default digest``() = 
    Assert.That(digestSMTOpt testSMT baseLocation, Is.EqualTo(None))

[<Test>]
let ``Empty SMT has default digest for given height``() = 
    Assert.That(digestSMT testSMT baseLocation, Is.EqualTo(Merkle.defaultHash treeconst (TSize)))

let smtOne = emptySMT<byte[]> treeconst testSerializer
let toInsert = Map.ofList [(firstKey, Some firstValue)]
let oneDigest = optUpdateSMT smtOne baseLocation toInsert

[<Test>]
let ``Inserting one key gives non-default digest``() =
    Assert.That(oneDigest, Is.Not.EqualTo(None))

[<Test>]
let ``One item SMT has one key``() =
    Assert.That(smtOne.kv.Count, Is.EqualTo(1))

[<Test>]
let ``Removing item from SMT removes key and changes digest``() =
    let smt = emptySMT<byte[]> treeconst testSerializer
    let toInsert = Map.ofList [(firstKey, Some firstValue)]
    ignore <| optUpdateSMT smt baseLocation toInsert
    let toRemove = Map.ofList [(firstKey,None)]
    let d = optUpdateSMT smt baseLocation toRemove
    Assert.That(d, Is.EqualTo(None))
    Assert.That(smt.kv.Count, Is.EqualTo(0))

let randomKeys size =
    let rnd = System.Random(796650)
    Seq.initInfinite (fun _ ->
        let buffer = Array.zeroCreate size
        rnd.NextBytes buffer
        buffer)

[<Test>]
let ``Adding 5 items results in 5 keys``() =
    let smt2 = emptySMT<byte[]> treeconst testSerializer
    let aval = "a value"B
    let toInsertSeq =
        (Seq.take 5) <| (Seq.map (fun k -> (k, Some aval)) (randomKeys (BSize)))
    let toInserta = Map.ofSeq toInsertSeq
    let d = optUpdateSMT smt2 baseLocation toInserta
    printfn "SMT with 5 elements has %d cached digests" smt2.digests.Count
    printfn "toInsert count is %d" toInserta.Count
    printfn "toInsert keys: %A" <| Seq.map fst toInsertSeq
    printfn "kv has %d key-val pairs" <| smt2.kv.Count
    printfn "kv keys are %A" <| List.map fst (Map.toList smt2.kv)
    Assert.That(d, Is.Not.EqualTo(None))
    Assert.That(smt2.kv.Count, Is.EqualTo(5))

[<Test>]
let ``Adding 1000 items results in 1000 keys``() =
    let smt = emptySMT<byte[]> treeconst testSerializer
    let aval = "a value"B
    let toInsertSeq =
        (Seq.take 1000) <| (Seq.map (fun k -> (k, Some aval)) (randomKeys (BSize)))
    let toInserta = Map.ofSeq toInsertSeq
    let d = optUpdateSMT smt baseLocation toInserta
    printfn "SMT with 1000 elements has %d cached digests" smt.digests.Count
    printfn "toInsert count is %d" toInserta.Count
    printfn "toInsert keys: %A" <| Seq.map fst toInsertSeq
    printfn "kv has %d key-val pairs" <| smt.kv.Count
    printfn "kv keys are %A" <| List.map fst (Map.toList smt.kv)
    Assert.That(d, Is.Not.EqualTo(None))
    Assert.That(smt.kv.Count, Is.EqualTo(1000))

let smt = emptySMT<byte[]> treeconst testSerializer
let aval = "a value"B
let toInsertSeq =
    (Seq.take 1000) <| (Seq.map (fun k -> (k, Some aval)) (randomKeys (BSize)))
let toInserta = Map.ofSeq toInsertSeq
let d = optUpdateSMT smt baseLocation toInserta
let aKey = smt.kv |> Map.toList |> List.item 200 |> fst

[<Test>]
let ``Audit path generated for existing key``() =
    let auditPath = optAuditSMT smt aKey
    Assert.That(auditPath.Length, Is.EqualTo(TSize))

[<Test>]
let ``Audit path for existing key verifies against SMT``() =
    let auditPath = optAuditSMT smt aKey
    let calculatedRoot = optFindRoot smt.cTW smt.defaultDigests auditPath aKey (Some aval) baseLocation
    Assert.That(calculatedRoot, Is.EqualTo(d))