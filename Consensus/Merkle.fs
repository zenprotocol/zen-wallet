module Consensus.Merkle


open MsgPack
open MsgPack.Serialization

open Consensus.Types
open Consensus.Serialization

open Consensus.Tree

// TODO: Make thread-safe
let sha3 = new Org.BouncyCastle.Crypto.Digests.Sha3Digest(256)

type Hashable =
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

//let serialized : Hashable -> byte[] =
//    function
//    | Transaction tx -> serialize tx
//    | OutputLock ol -> serialize ol
//    | _ -> failwith "not implemented"

// TODO: get the tag and return a closure
let taggedHash : ('T -> Hashable) -> 'T -> Hash = 
    fun wrapper item -> innerHashList [tag <| wrapper item; serialize item]

// Example use of taggedHash: partially apply to the Transaction discriminator
let transactionHash = taggedHash Transaction



//type Tree = {
//    constant: byte[];
//    items: Hashable list
//    }
////    with member this.size =
////                    let lazysize:Lazy<int> = TreeHelper.size this
////                    lazysize.Force()

////and TreeHelper() =
////    let lazySize (tree:Tree) =
////        lazy (List.length (tree.items))
////    let lazyRoot (tree:Tree) =
////        lazy ([||]:byte[])
////    static member val private trees = System.Collections.Generic.Dictionary<Tree, Lazy<int> * Lazy<Hash>>()
////    static member private init(tree : Tree) =
////        TreeHelper.trees.[tree] <- (lazySize tree, lazyRoot tree)
////    static member size(tree : Tree) =
////        if not <| TreeHelper.trees.ContainsKey(tree) then
////            ()
////        if (TreeHelper.trees.ContainsKey(tree)) then
////            (fst <| TreeHelper.trees.[tree]) |> ignore
////            lazy 6
////        else
////            lazy 7

//let lazyTreeSize tree =
//    let result = lazy (List.length tree.items)
//    fun () -> result

