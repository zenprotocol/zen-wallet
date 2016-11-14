module Consensus.SparseMerkleTree


open MsgPack
open MsgPack.Serialization

open Consensus.Types
open Consensus.Serialization

open Consensus.Tree
open Consensus.Merkle

let zeroLoc = Array.zeroCreate<bool> 256

let emptyTree<'L> = addLocation 255 <| Branch ((), Leaf (None:'L option), Leaf None)


let rec insert tree k v =
    match tree with
    | Branch ({location={loc=b; height=h}} as data, left, right) ->
        if k <= b then Branch (data, insert left k v, right)
        else Branch (data, left, insert right k v)
    | Leaf {location={loc=b; height=h} as location} when h > 0 ->
        insert
        <| Branch (
            {data=();location=location}, 
            Leaf {data=None; location = leftLocation location}, 
            Leaf {data=None; location = rightLocation location})
        <| k <| v
    | Leaf {location=location} -> Leaf {data = Some v; location=location}

let remove = 
    let rec innerRem = fun tree k ->
        match tree with
        | Leaf ({location={loc=loc; height=height} as location})
             when loc=k && height=0 ->
                 Leaf {data=None;location=location}
        | Leaf _ -> tree
        | Branch ({location={loc=b}} as br, lTree, rTree) ->
            if k <= b then Branch (br, innerRem lTree k, rTree)
            else Branch (br, lTree, innerRem rTree k)

    fun tree k -> normalize <| innerRem tree k

type MerklizedData<'T> = {data:'T; isDefault:bool; digest: Hash}
type MerklizedLocationData<'T> = LocData<MerklizedData<'T>>

let sparseLeafMerklize cTW (defaultHashes:int->Hash) =
    let hasher = fun x b ->
        innerHashList [cTW; x; bitsToBytes b]
    function
    | {data=None; location={height=h} as location} ->
        Leaf {data={data=None; isDefault=true; digest=defaultHashes h}; location=location} : FullTree<MerklizedLocationData<byte[] option>,_>
    | {data=Some x; location={loc=loc} as location} ->
        Leaf {data={data=Some x; isDefault=false; digest=hasher x loc}; location=location}
    