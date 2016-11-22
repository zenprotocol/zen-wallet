module Consensus.SparseMerkleTree


open MsgPack
open MsgPack.Serialization

open Consensus.Types
open Consensus.Serialization

open Consensus.Tree
open Consensus.Merkle

let TSize = 256
let BSize = TSize/8


type BLocation = {h:int;b:byte[]}

let baseLocation = {h = TSize; b = Array.zeroCreate<byte> BSize}
let zeroLoc' n = Array.zeroCreate<bool> n
let zeroLoc = zeroLoc' TSize

let splitindex h = 
    let j = TSize - h
    let bnum = j / 8
    let m = 7 - (j % 8)
    let ret = Array.zeroCreate<byte> (BSize)
    ret.SetValue(1uy <<< m,bnum)
    ret

let bitAt h (k:byte[]) =
    let j = TSize - h
    let bnum = j / 8
    let m = 7 - (j % 8)
    k.[bnum] &&& (1uy <<< m) <> 0uy

let left {h=h;b=b} = {h=h-1;b=b}
let right {h=h;b=b} =
    let ir b h =
        let ret = Array.copy b
        let j = TSize - h
        let bnum = j/8
        let m = 7 - (j%8)
        ret.SetValue(b.[bnum] ||| (1uy <<< m), bnum)
        ret
    {h=h-1;b = ir b h}

let indexOf n = ((TSize - n)/8,7-((TSize - n)%8))

type SMT<'V> = {cTW: byte[]; mutable kv: Map<Hash,'V>; mutable digests: Map<BLocation,Hash>; defaultDigests: int->Hash; serializer:'V->byte[]}

let emptySMT<'V> = fun cTW serializer -> {cTW = cTW; kv = Map.empty<Hash,'V>; digests = Map.empty<BLocation,Hash>; defaultDigests = defaultHash cTW; serializer=serializer}

let splitlist kvl s =
    let comp = fun (k,v) -> k < s
    (List.takeWhile comp kvl, List.skipWhile comp kvl)

let splitmap kv s =
    let llist, rlist = splitlist (Map.toList kv) s
    (Map.ofList llist, Map.ofList rlist)

let lHash cTW b sv = innerHashList [cTW; sv; b]
let optLeafHash cTW b = Option.map (lHash cTW b)

let iHash dl dr {h=h;b=b} = innerHashList [dl; dr; b; toBytes h]
let optInnerHash defaultDigests dl dr ({h=h;b=b} as location) =
    match dl, dr with
    | None, None -> None
    | None, Some r ->
        Some <| iHash (defaultDigests (h-1)) r location
    | Some l, None ->
        Some <| iHash l (defaultDigests (h-1)) location
    | Some l, Some r ->
        Some <| iHash l r location



let rec digestOpt cTW (kv:Map<Hash,Hash>) (digests:Map<BLocation,Hash>) (ddigests:int->Hash) ({h=h;b=b} as location) =
    if digests.ContainsKey(location) then Some <| digests.[location]
    elif kv.Count = 0 then None
    elif kv.Count = 1 && h = 0 then
        let (k,v) = (Map.toList kv).[0]
        if k <> b then
            printfn "key not localised" // debug printf
            None
        else
            Some <| lHash cTW b v
    else
        let rloc = right location
        let lloc = left location
        let kvl, kvr = splitmap kv <| rloc.b
        optInnerHash ddigests <|
        (digestOpt cTW kvl digests ddigests lloc ) <|
        (digestOpt cTW kvr digests ddigests rloc ) <|
        location

let digestSMTOpt {cTW=cTW; kv=kv; digests=digests; defaultDigests=defaultDigests; serializer=serializer} location =
    let mkv = kv |> Map.map (fun _ y -> serializer y)
    digestOpt cTW mkv digests defaultDigests location

let fromOpt<'V> f (smt:SMT<'V>) location =
    let optResult = f smt location
    match optResult with
    | None -> smt.defaultDigests location.h
    | Some r -> r

let digestSMT<'V> = fromOpt<'V> digestSMTOpt


let optCache (digests : Map<BLocation,byte[]> byref) defaultDigests location lroot rroot =
    let rloc = right location
    let lloc = left location
    let ret = optInnerHash defaultDigests lroot rroot location
    match lroot, rroot with
    | Some l, Some r ->
        digests <- digests.Add (lloc,l)
        digests <- digests.Add (rloc,r)
    | _ , _ ->
        digests <- digests.Remove lloc
        digests <- digests.Remove rloc
    ret

let rec optUpdate cTW (splitkv : Map<Hash,Hash>) (digests : Map<BLocation,Hash> byref) ddigests ({h=h;b=b} as location) (kvs:Map<Hash,Hash option>) =
    if h=0 then
        optLeafHash cTW b (splitkv.TryFind b)    
    else    // Update digests
        let rloc = right location
        let lloc = left location
        let kvsl, kvsr = splitmap kvs rloc.b
        let splitkvl, splitkvr = splitmap splitkv rloc.b
        let lroot, rroot =
            match kvsl.Count, kvsr.Count with
            | 0,0 -> 
                digestOpt cTW splitkvl digests ddigests lloc, digestOpt cTW splitkvr digests ddigests rloc
            | _, 0 ->
                optUpdate cTW splitkvl &digests ddigests lloc kvsl, digestOpt cTW splitkvr digests ddigests rloc
            | 0, _ ->
                digestOpt cTW splitkvl digests ddigests lloc, optUpdate cTW splitkvr &digests ddigests rloc kvsr
            | _, _ ->
                optUpdate cTW splitkvl &digests ddigests lloc kvsl, optUpdate cTW splitkvr &digests ddigests rloc kvsr
        optCache &digests ddigests location lroot rroot

let optUpdateSMT ({cTW=cTW; kv=kv; digests=digests; defaultDigests=defaultDigests; serializer=serializer} as smt) location kvs =
    let updateKV = fun k mv ->
        match mv with
        | None -> smt.kv <- smt.kv.Remove k
        | Some v ->
            smt.kv <- smt.kv.Add (k, v)
    Map.iter updateKV kvs
    let kvHashed = Map.map (fun _ y -> serializer y) smt.kv
    let ret = optUpdate cTW kvHashed &smt.digests defaultDigests location kvs
    ret

let updateSMT smt location kvs = fromOpt (fun s l -> optUpdateSMT s l kvs) smt location

let rec optFindRoot cTW defaultDigests (path: Hash option list) k v location =
    if location.h = 0 then optLeafHash cTW location.b v
    elif not <| bitAt location.h k then
        optInnerHash defaultDigests (optFindRoot cTW defaultDigests path k v (left location)) (path.[TSize-location.h]) location
    else
        optInnerHash defaultDigests (path.[TSize-location.h]) (optFindRoot cTW defaultDigests path k v (right location)) location

let rec optAudit cTW kv digests ddigests location key =
    if location.h = 0 then []
    elif not <| bitAt location.h key then
        let kvl, kvr = splitmap kv (right location).b
        (digestOpt cTW kvr digests ddigests (right location) ) :: optAudit cTW kvl digests ddigests (right location) key
    else
        let kvl, kvr = splitmap kv (right location).b
        (digestOpt cTW kvl digests ddigests (left location)) :: optAudit cTW kvr digests ddigests (left location) key

let optAuditSMT smt key  =
    let location = baseLocation
    let kvm = Map.map (fun _ y -> smt.serializer y) smt.kv
    optAudit smt.cTW kvm smt.digests smt.defaultDigests location key 
