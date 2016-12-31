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
    | BlockHeader _ -> "bheader"B
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

//let blockHasher block = blockHeaderHasher block.header
let blockHasher =
    fun block ->
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
    seq {
        let b = ref 0uy
        for i=0 to bs.Length-1 do
            let rem = i % 8
            if rem = 0 && i <> 0 then
                yield !b
                b := 0uy
            if bs.[i] then
                b := !b + byte (1 <<< rem)
        yield !b
    } |> Array.ofSeq


type Digest = {digest: byte[]; isDefault: bool}

let leafHash cTW (wrapper:'T->Hashable) defaultHashes = 
    let typeTag = tag << wrapper <| Unchecked.defaultof<'T>
    let hasher = fun (x:'T) b ->
        innerHashList [cTW; typeTag; serialize x; bitsToBytes b]
    function
    | {data = None; location={height=h}} -> {digest=defaultHashes h; isDefault=true}
    | {data = Some x; location={loc=b}} -> {digest=hasher x b; isDefault=false}

let branchHash defaultHashes = fun branchData dL dR ->
    match branchData.location ,dL, dR with
    | {height=h}, {isDefault=true}, {isDefault=true} ->
        {digest = defaultHashes h; isDefault=true}
    | {height=h;loc=b}, _, _ ->
        {
            digest = innerHashList [dL.digest;dR.digest;bitsToBytes b;toBytes h];
            isDefault=false;
        }

let merkleRoot cTW wrapper =
    let defaultHashes = defaultHash cTW
    cata (leafHash cTW wrapper defaultHashes) (branchHash defaultHashes)                                                 


