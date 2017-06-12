module ContractExamples.MerkleTests
open System
open NUnit.Framework
open NUnit.Framework.Constraints

let testData = [|[|0uy;1uy;2uy|];[|22uy;33uy;44uy|];[|90uy; 182uy|]|]

[<Test>]
let ``Merkle tree non-empty``()=
    let mt = Merkle.merkleTree testData
    Assert.That(mt,Is.Not.Length.EqualTo 0)

[<Test>]
let ``Siblings non-empty``()=
    let mt = Merkle.merkleTree testData
    let sibs = Merkle.siblings 0u mt
    Assert.That(sibs, Is.Not.Length.EqualTo 0)

[<Test>]
let ``Audit path verifies``()=
    let mt = Merkle.merkleTree testData
    let audit = Merkle.auditPath 0u mt
    let root = Merkle.rootFromAuditPath audit
    let root2 = Array.get <| Array.last mt <| 0
    Assert.That(root, Is.EquivalentTo root2)

let largeRandomArray = Array.zeroCreate<byte> (32*10_000)
Random(1234).NextBytes largeRandomArray
let largeTestData = Array.chunkBySize 32 largeRandomArray
let randomIndex = Random(1234).Next() % 10000

[<Test>]
let ``Audit path verifies for big data``()=
    let mt = Merkle.merkleTree largeTestData
    let audit = Merkle.auditPath (uint32 randomIndex) mt
    let root = Merkle.rootFromAuditPath audit
    let root2 = Array.get <| Array.last mt <| 0
    Assert.That(root, Is.EquivalentTo root2)