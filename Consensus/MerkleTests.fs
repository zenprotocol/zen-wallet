module Consensus.MerkleTests

open NUnit.Framework
open NUnit.Framework.Constraints
open System


open Consensus.Tree
open Consensus.Merkle

let sha3 = new Org.BouncyCastle.Crypto.Digests.Sha3Digest(256)
type Hex =  Org.BouncyCastle.Utilities.Encoders.Hex

[<Test>]
let ``SHA3 hash of null string matches known value``() =
    sha3.BlockUpdate([||],0,0)
    let result = Array.zeroCreate (sha3.GetDigestSize())
    let hashLength = sha3.DoFinal(result,0)
    let hexResult = Hex.ToHexString(result)
    let knownNullHash = "a7ffc6f8bf1ed76651c14756a061d662f580ff4de43b49fa82d80a4b80f8434a"
    Assert.That(hexResult, Is.EqualTo(knownNullHash))
    Assert.That(hashLength, Is.EqualTo(32))



[<Test>]
let ``Print tree of [1..14]``() =
    let ls = [1..14]
    let tree = complete(ls)
    printfn "%A" tree
    Assert.IsTrue(true)
