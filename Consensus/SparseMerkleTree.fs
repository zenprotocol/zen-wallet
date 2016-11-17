module Consensus.SparseMerkleTree


open MsgPack
open MsgPack.Serialization

open Consensus.Types
open Consensus.Serialization

open Consensus.Tree
open Consensus.Merkle

let TSize = 256

let zeroLoc' n = Array.zeroCreate<bool> n
let zeroLoc = zeroLoc' TSize

let emptyTree'<'L> n = addLocation (n) <| Branch ((), Leaf (None:'L option), Leaf None)
let emptyTree<'L> = emptyTree' TSize


let rec insert tree k v =
    match tree with
    | Branch ({location=location} as data, left, right) ->
        if k < (rightLocation location).loc then Branch (data, insert left k v, right)
        else Branch (data, left, insert right k v)
    | Leaf {location={loc=b; height=h} as location} when h > 0 ->
        insert
        <| Branch (
            {data=();location=location}, 
            Leaf {data=None; location = leftLocation location}, 
            Leaf {data=None; location = rightLocation location})
        <| k <| v
    | Leaf {data=data; location=location} ->
        if k=location.loc then
            Leaf {data = Some v; location=location}
        else tree 

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

type MerklizedData<'T> = {data:'T; isDefault:bool; digest: Hash option}
type MerklizedLocationData<'T> = LocData<MerklizedData<'T>>

let sparseLeafMerklize cTW (defaultHashes:int->Hash) serializer =
    let hasher = fun x b ->
        innerHashList [cTW; serializer x; bitsToBytes b]
    function
    | {data=None; location={height=h} as location} ->
        Leaf {data={data=None; isDefault=true; digest=Some <| defaultHashes h}; location=location} : FullTree<MerklizedLocationData<_>,_>
    | {data=Some x; location={loc=loc} as location} ->
        Leaf {data={data=Some x; isDefault=false; digest=Some <| hasher x loc}; location=location}


let (|GetDigest|_|) tree =
    match tree with
    | Branch ({data={digest=dig;data=_};location=_}, _, _) -> dig
    | Leaf ({data={digest=dig;data=_};location=_}) -> dig

let (|DefaultDigest|NonDefaultDigest|) tree =
    match tree with
    | Branch ({data={isDefault=true;data=_};location=_}, _, _) -> DefaultDigest
    | Leaf ({data={isDefault=true;data=_};location=_}) -> DefaultDigest
    | _ -> NonDefaultDigest

let eraseDigest (tree:FullTree<MerklizedLocationData<'L>,MerklizedLocationData<_>>)=
    match tree with
    | Leaf ({data=d} as lD) ->
        Leaf {lD with data = {d with digest=None}}
    | Branch ({data=d} as bD, lTree, rTree) ->
        Branch ({bD with data = {d with digest=None}},lTree, rTree)
            
let sparseBranchMerklize (defaultHashes:int->Hash) = fun branchData mlTree mrTree ->
    let eraser = match mlTree, mrTree with
                 | NonDefaultDigest, NonDefaultDigest -> id
                 | _,_ -> eraseDigest
    let digTree = function
               | GetDigest d -> d
               | _ -> failwith "bad tree"
    let b = branchData.location.loc
    let h = branchData.location.height
    let defaultness, dig = match mlTree, mrTree with
                           | DefaultDigest, DefaultDigest ->
                               true, Some <| defaultHashes h
                           | _, _ ->
                               false, Some <| innerHashList [digTree mlTree; digTree mrTree; bitsToBytes b; toBytes h]
    let newBr =
        (
            {
                location=branchData.location;
                data=
                    {
                    data=branchData.data;
                    isDefault=defaultness
                    digest=dig
                    }
            },
            eraser mlTree,
            eraser mrTree
        )
    Branch <| newBr

let merklize cTW serializer =
    let defaultHashes = defaultHash cTW
    cata (sparseLeafMerklize cTW defaultHashes serializer) (sparseBranchMerklize defaultHashes)


type SMT<'V> = {cTW: byte[]; mutable kv: Map<Hash,'V>; digests: Map<Loc,Hash> ref; defaultDigests: int->Hash}

let emptySMT<'V>(cTW) = {cTW=cTW; kv= Map.empty<Hash,'V>; digests = ref Map.empty<Loc,Hash>; defaultDigests = defaultHash cTW}

let splitm (kvs:Map<Hash,'V>) s = 
    let splitter kv =
        (bytesToBits (fst kv)) < s
    let kvlist = Map.toList kvs |> fun l -> (List.takeWhile splitter l, List.skipWhile splitter l)
    match kvlist with
    | (lower, upper) -> (Map.ofList lower, Map.ofList upper)
    

let split (smt:SMT<'V>) s =
    let splitter kv =
        (bytesToBits (fst kv)) < s
    let kvl, kvu =
        let kvlist = Map.toList smt.kv |> fun l -> (List.takeWhile splitter l, List.skipWhile splitter l)
        match kvlist with
        | (lower, upper) -> (Map.ofList lower, Map.ofList upper)
    ({smt with kv=kvl}, {smt with kv=kvu})

let lh serializer cTW b v = innerHashList [cTW; serializer v; bitsToBytes b]
let ih dl dr b h = innerHashList [dl; dr; bitsToBytes b; toBytes h]

let lhopt emp serializer cTW b v =
    match v with
    | Some v -> lh serializer cTW b v
    | None -> emp

let ihopt defdigests dl dr b h =
    match dl, dr with
    | None, None -> None
    | None, Some r ->
        Some <| innerHashList [defdigests h; r; bitsToBytes b; toBytes h]
    | Some l, None ->
        Some <| innerHashList [l; defdigests h; bitsToBytes b; toBytes h]
    | Some l, Some r ->
        Some <| innerHashList [l; r; bitsToBytes b; toBytes h]

let lhopt' serializer cTW b = Option.map (lh serializer cTW b)

let rec digestSMT (serializer:'V->byte[]) ({cTW=cTW;kv=kv;digests=digests;defaultDigests=defaultDigests}:SMT<'V> as smt) (location:Loc) =
    if (!digests).ContainsKey(location) then (!digests).[location]
    elif kv.Count = 0 then defaultDigests location.height
    elif kv.Count = 1 && location.height = 0 then
        lh serializer cTW location.loc kv.[bitsToBytes location.loc]
    else
        let smtl, smtu = split smt <| (rightLocation location).loc
        ih <|
        (digestSMT serializer smtl <| leftLocation location) <|
        (digestSMT serializer smtu <| rightLocation location) <|
        location.loc <| location.height

let rec digestSMTOpt (serializer:'V->byte[]) ({cTW=cTW;kv=kv;digests=digests;defaultDigests=defaultDigests}:SMT<'V> as smt) (location:Loc) =
    if (!digests).ContainsKey(location) then Some <| (!digests).[location]
    elif kv.Count = 0 then None
    elif kv.Count = 1 && location.height = 0 then
        Some <| lh serializer cTW location.loc kv.[bitsToBytes location.loc]
    else
        let smtl, smtu = split smt <| (rightLocation location).loc
        ihopt <|
        defaultDigests <|
        (digestSMTOpt serializer smtl <| leftLocation location) <|
        (digestSMTOpt serializer smtu <| rightLocation location) <|
        location.loc <| location.height

let auditSMT (serializer:'V->byte[]) ({cTW=cTW;kv=kv;digests=digests;defaultDigests=defaultDigests}:SMT<'V> as smt) (location:Loc) =
    let rec innerAuditSMT smt' (k:bool[]) location' =
        if location'.height = 0 then [] else
            let smtl, smtr = split smt' <| (rightLocation location').loc
            if not <| k.[TSize-location'.height] then
                (digestSMT serializer smtr <| rightLocation location') :: (innerAuditSMT smtl k (leftLocation location'))
            else
                (digestSMT serializer smtl <| leftLocation location') :: (innerAuditSMT smtr k (rightLocation location'))
    fun key -> List.toArray <| innerAuditSMT smt (bytesToBits key) location

let rec findRootOpt serializer cTW defaultHashes (path: Hash option []) key v ({height=height;loc=loc} as location) =
    if height = 0 then lhopt' serializer cTW loc v
    elif not <| (bytesToBits key).[TSize-height] then
        ihopt defaultHashes (findRootOpt serializer cTW defaultHashes path key v (leftLocation location)) path.[TSize-height] loc height
    else
        ihopt defaultHashes (path.[TSize-height]) (findRootOpt serializer cTW defaultHashes path key v (rightLocation location) ) loc height

let findRoot serializer cTW (path: Hash option [] ) key v location =
    let defaultHashes = defaultHash cTW
    findRootOpt serializer cTW defaultHashes path key v location


let cache {defaultDigests=defaultDigests} height loc lroot rroot =
    let ret = ih lroot rroot loc height
    failwith "not implemented"

let cacheOpt rootSmt ({kv=kv; defaultDigests=defaultDigests} as smt) height loc lroot rroot =
    let location = {height=height;loc=loc}
    let rlocation = rightLocation location
    let llocation = leftLocation location
    let ret = ihopt defaultDigests lroot rroot loc height
    match lroot, rroot with
    | Some l, Some r ->
        smt.digests := smt.digests.Value.Add (llocation,l)
        smt.digests := smt.digests.Value.Add (rlocation,r)
    | _ , _ ->
        smt.digests := smt.digests.Value.Remove llocation
        smt.digests := smt.digests.Value.Remove rlocation
    ret


let auditSMTOpt (serializer:'V->byte[]) ({cTW=cTW;kv=kv;digests=digests;defaultDigests=defaultDigests}:SMT<'V> as smt) (location:Loc) =
    let rec innerAuditSMTOpt smt' (k:bool[]) location' =
        if location'.height = 0 then [] else
            let smtl, smtr = split smt' <| (rightLocation location').loc
            if not <| k.[TSize-location'.height] then
                (digestSMTOpt serializer smtr <| rightLocation location') :: (innerAuditSMTOpt smtl k (leftLocation location'))
            else
                (digestSMTOpt serializer smtl <| leftLocation location') :: (innerAuditSMTOpt smtr k (rightLocation location'))
    fun key -> List.toArray <| innerAuditSMTOpt smt (bytesToBits key) location

let rec updateSMT (serializer:'V->byte[]) ({cTW=cTW;kv=kv;digests=digests;defaultDigests=defaultDigests}:SMT<'V> as smt) ({height=height;loc=loc} as location:Loc) (kvs:Map<Hash,'V>) =
    failwith "not implemented"

let rec updateSMTOpt (serializer:'V->byte[]) (rootSmt : SMT<'V>) ({cTW=cTW;kv=kv;digests=digests;defaultDigests=defaultDigests}:SMT<'V> as smt) ({height=height;loc=loc} as location:Loc) (kvs:Map<Hash,'V option>) =
    if height=0 then
        let newval = Option.bind id (kvs.TryFind <| bitsToBytes loc)
        match newval with
        | Some v -> rootSmt.kv <- rootSmt.kv.Add (bitsToBytes loc, v)
        | None -> rootSmt.kv <- rootSmt.kv.Remove (bitsToBytes loc)
        lhopt' serializer cTW loc newval
    else
        let kvl, kvu = splitm kvs (rightLocation location).loc
        let smtl, smtu = split smt (rightLocation location).loc
        match kvl.Count, kvu.Count with
        | 0, 0 -> digestSMTOpt serializer smt location
        | 0, _ ->
            cacheOpt rootSmt smt height loc <|
            digestSMTOpt serializer smtl (leftLocation location) <|
            updateSMTOpt serializer rootSmt smtu (rightLocation location) kvs 
        | _, 0 ->
            cacheOpt rootSmt smt height loc <|
            updateSMTOpt serializer rootSmt smtl (leftLocation location) kvs <|
            digestSMTOpt serializer smtu (rightLocation location)
        | _, _ ->
            cacheOpt rootSmt smt height loc <|
            updateSMTOpt serializer rootSmt smtl (leftLocation location) kvl <|
            updateSMTOpt serializer rootSmt smtu (rightLocation location) kvu

