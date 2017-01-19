module Consensus.Merkle


open MsgPack
open MsgPack.Serialization

open Consensus.Types
open Consensus.Serialization

open Consensus.Tree

// TODO: Make thread-safe
let sha3 = new Org.BouncyCastle.Crypto.Digests.Sha3Digest(256)

type Hashable =
    public
    | Transaction of Transaction
    | OutputLock of OutputLock
    | Spend of Spend
    | Outpoint of Outpoint
    | Output of Output
    | Contract of Contract
    | ExtendedContract of ExtendedContract
    | BlockHeader of BlockHeader
    | Block of Block
    | Hash of Hash

let tag : Hashable -> byte[] =
    function
    | Transaction _ -> "tx"B
    | OutputLock _ -> "ol"B
    | Spend _ -> "sp"B
    | Outpoint _ -> "outpoint"B
    | Output _ -> "output"B
    | Contract _ -> "contract"B
    | ExtendedContract _ -> "contract"B
    | BlockHeader _ -> "block"B
    | Block _ -> "block"B
    | Hash _ -> "hash"B

let innerHash : byte[] -> byte[] =
    fun bs ->
    let res = Array.zeroCreate 32
    sha3.BlockUpdate(bs,0,Array.length bs)
    sha3.DoFinal(res, 0) |> ignore
    res

let innerHashList : seq<byte[]> -> byte[] =
    fun bseq ->
    let res = Array.zeroCreate 32
    Seq.iter (fun bs -> sha3.BlockUpdate(bs,0,Array.length bs)) bseq
    sha3.DoFinal(res,0) |> ignore
    res

let serialize (x : 'a) =
    let serializer = MessagePackSerializer.Get<'a>(context)
    use stream = new System.IO.MemoryStream()
    serializer.Pack(stream, x)
    stream.ToArray()

// Example use of taggedHash: partially apply to the Transaction discriminator
// let transactionHash = taggedHash Transaction

let taggedHash : ('T -> Hashable) -> 'T -> Hash =
    fun wrapper ->
        let aTag = tag <| wrapper Unchecked.defaultof<'T>
        fun item -> innerHashList [ aTag; serialize item]


let transactionHasher = taggedHash Transaction
let outputLockHasher = taggedHash OutputLock
let spendHasher = taggedHash Spend
let outpointHasher = taggedHash Outpoint
let outputHasher = taggedHash Output
let contractHasher = taggedHash Contract
let extendedContractHasher = taggedHash ExtendedContract
let blockHeaderHasher = taggedHash BlockHeader
let blockHasher block =
    blockHeaderHasher block.header
let hashHasher = taggedHash Hash

// Usage: partially apply to cTW and keep a reference as long as
// the tree is needed.
let defaultHash cTW =
    let defaultHashSeq =
        Seq.unfold
        <| fun hashN ->
            let nextHash = innerHashList [hashN; hashN]
            Some (hashN, nextHash)
        <| innerHash cTW
        |> Seq.cache
    fun n -> Seq.item n defaultHashSeq

// little-endian, fixed size (for any 8, 16 or 32 bit integral)
let inline toBytes (n: ^T) =
    let u = uint32 n
    let low = byte u
    let mLow = byte (u >>> 8)
    let mHigh = byte (u >>> 16)
    let high = byte (u >>> 24)
    [|low; mLow; mHigh; high|]

let bitsToBytes (bs:bool[]) =
    let ba = System.Collections.BitArray(bs)
    let ret : byte[] = Array.zeroCreate(bs.Length / 8)
    ba.CopyTo(ret,0)
    ret

let bytesToBits (bs:byte[]) = 
    let ba = System.Collections.BitArray(bs)
    let ret : bool[] = Array.zeroCreate(bs.Length*8)
    ba.CopyTo(ret,0)
    ret

let merkleRoot cTW hasher items =
    let defaultHashes = defaultHash cTW
    let leaves = List.map hasher items
    let rec recursiveRoot innerList height = 
        match List.length innerList with
        | 1 -> innerList.Head
        | n ->
            let pairs =
                if n%2 = 0 then
                    List.chunkBySize 2 innerList
                else
                    List.chunkBySize 2 (innerList @ [defaultHashes height])  // this stinks
            let nextList = List.map (fun p -> innerHashList p) pairs
            recursiveRoot nextList (height+1)
    recursiveRoot leaves 0
