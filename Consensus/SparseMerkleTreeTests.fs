module Consensus.SparseMerkleTreeTests

open NUnit.Framework
open NUnit.Framework.Constraints
open System

open Tree
open SparseMerkleTree

let testSize = 256

let emptree = emptyTree'<byte[]> testSize

let onetree = insert emptree (zeroLoc' testSize) [|1uy|]

let midLoc =
    let ret = Array.copy (zeroLoc' testSize)
    ret.SetValue(true, testSize/2)
    ret

let twotree = insert onetree midLoc [|1uy|]

let konst<'T, 'U> = fun (x:'T) (y:'U) -> x

let countleaves tree = cata (konst 1) (fun _ x y -> x+y) tree

let countSomeLeaves tree = cata (fun (x:LocData<'T option>) -> if Option.isNone x.data then 0 else 1) (fun _ x y -> x+y) tree


[<Test>]
let ``One item tree has one non-empty leaf``() =
    Assert.That( countSomeLeaves onetree, Is.EqualTo(1))

[<Test>]
let ``Two item tree has two non-empty leaves``() =
    Assert.That( countSomeLeaves twotree, Is.EqualTo(2))

let nextLoc =
    let ret = Array.copy midLoc
    ret.SetValue(true, testSize - testSize/4)
    ret

let threetree = insert twotree nextLoc [|1uy|]

//let stripLoc<'T> =
//    cata <|
//    (Leaf << (fun (lf:LocData<'T option>) -> if lf.data.IsNone then None else Some <| lf.location.loc)) <|
//    fun br l r -> Branch ((),l,r)

[<Test>]
let ``3 item tree has 3 non-empty leaves``() =
    Assert.That( countSomeLeaves threetree, Is.EqualTo(3))

let toBits (bi:bigint) =
    let mutable b = bi
    let ret = Array.copy (zeroLoc' testSize)
    for i in [testSize - 1.. -1 .. 0] do
        if not b.IsEven then ret.[i] <- true
        b <- b / bigint 2
    ret

let bigSize = 100

let bigtree = List.fold (fun tree n -> insert tree (toBits n) [|1uy|]) emptree [bigint 0 .. bigint (bigSize - 1)]

[<Test>]
let ``Multi-item tree has same number of non-empty leaves``() =
    Assert.That( countSomeLeaves bigtree, Is.EqualTo(bigSize))

let testSerializer = id<byte[]>
let treeconst = "treeconst!!!"B
let testSMT = emptySMT<byte[]>(treeconst)

let firstKey = (Merkle.bitsToBytes zeroLoc)
firstKey.SetValue(63uy,TSize/8 /2)
let firstValue = "the first value"B

let defLocation = {loc=zeroLoc;height=TSize-1}

[<Test>]
let ``Empty SMT has no keys``() = 
    Assert.That(testSMT.kv.Count, Is.Zero)

[<Test>]
let ``Empty SMT has default digest``() = 
    Assert.That(digestSMTOpt testSerializer testSMT defLocation, Is.EqualTo(None))

[<Test>]
let ``Empty SMT has default digest for given height``() = 
    Assert.That(digestSMT testSerializer testSMT defLocation, Is.EqualTo(Merkle.defaultHash treeconst (TSize-1)))

[<Test>]
let ``Inserting one key gives non-default digest``() =
    let toInsert = Map.ofList [(firstKey, Some firstValue)]
    let d = updateSMTOpt testSerializer testSMT testSMT defLocation toInsert
    Assert.That(d, Is.Not.EqualTo(None))

[<Test>]
let ``One item SMT has one key``() =
    Assert.That(testSMT.kv.Count, Is.EqualTo(1))

[<Test>]
let ``Removing item from SMT removes key and changes digest``() =
    let toRemove = Map.ofList [(firstKey,None)]
    let d = updateSMTOpt testSerializer testSMT testSMT defLocation toRemove
    Assert.That(d, Is.EqualTo(None))
    Assert.That(testSMT.kv.Count, Is.EqualTo(0))

//let semp = emptyTree' 2
//let sone = insert semp [|false;false;|] 1
//let stwo = insert sone [|false;true;|] 1
//let sthree = insert stwo [|true;false;|] 1
//let sfour = insert sthree [|true;true;|] 1

//[<Test>]
//let ``Print small trees``() =
//    printfn "none: %A" semp
//    printfn "one: %A" sone
//    printfn "two: %A" stwo
//    printfn "three: %A" sthree
//    printfn "four: %A" sfour
//    Assert.IsTrue(false)