module Consensus.SerializationTests

open NUnit.Framework
open FsCheck
open FsCheck.NUnit
open Froto.Serialization
open Types

let filteredInnerGen (toFilter:Set<int32>) =
    let fieldGen = Arb.generate<int32> |> Gen.filter (fun i -> not <| Set.contains i toFilter )
    let lgen = Gen.zip fieldGen Arb.generate<uint64>
    let igen = Gen.zip fieldGen Arb.generate<uint32>
    let ldgen = Gen.zip fieldGen Arb.generate<byte[]>
    Gen.frequency <|
            [
                (1, Gen.map V lgen);
                (1, Gen.map F32 igen);
                (1, Gen.map F64 lgen);
                (3, Gen.map LD ldgen)
            ]

let filteredInnerShrink (toFilter:Set<int32>) =
        Arb.shrink<InnerField>
        >> Seq.filter (fun i -> not <| Set.contains i.FieldNum toFilter)

let rec filteredInnerListShrink (toFilter:Set<int32>) =
        fun l ->
            match l with
            | [] -> Seq.empty
            | x::xs -> 
                seq {
                    yield xs
                    for xs' in filteredInnerListShrink toFilter xs -> x::xs'
                    for x' in filteredInnerShrink toFilter x -> x'::xs
                }

type ArbitraryModifiers =
    static member ArrSeg() =
        Arb.from<byte[]>
        |> Arb.convert System.ArraySegment (fun s -> s.Array.[s.Offset .. s.Offset+s.Count-1])
    static member OutputLock() =
         let hashgen = Arb.generate<byte> |> Gen.listOfLength 32 |> Gen.map Array.ofList
         let hashshrink (h:byte[]) =
             seq { for i in 0 .. Array.length h do
                       let v = h.[i]
                       for nv in Arb.shrink v do
                           let next = Array.copy h
                           next.[i] <- nv
                           yield next
                 }
         let feegen = Gen.constant FeeLockP
         let pkgen = Gen.map PKLockP hashgen
         let sacgen = hashgen |> Gen.optionOf |> Gen.map ContractSacrificeLockP
         let congen = Gen.zip hashgen Arb.generate<byte[]> |> Gen.map ContractLockP
         let othgen = Gen.listOf (filteredInnerGen <| set [1;3;5;7]) |> Gen.map OtherLockP
         let outputgen = Gen.oneof [feegen;pkgen;sacgen;congen;othgen;]
         let outputshrink = fun (oput:OutputLockP) ->
             match oput with
             | OtherLockP innerList ->
                 filteredInnerListShrink <| set [1;3;5;7] <| innerList |> Seq.map OtherLockP
             | FeeLockP -> Seq.empty
             | PKLockP h -> hashshrink h |> Seq.map PKLockP
             | ContractSacrificeLockP None -> Seq.empty
             | ContractSacrificeLockP (Some h) ->
                 seq {
                     yield! hashshrink h |> Seq.map (ContractSacrificeLockP << Some)
                     yield ContractSacrificeLockP None
                 }
             | ContractLockP (h,bs) ->
                 seq {
                     yield! hashshrink h |> Seq.map (fun h' -> ContractLockP (h',bs))
                     yield! Arb.shrink bs |> Seq.map (fun bs -> ContractLockP(h,bs))
                 }
         Arb.fromGenShrink (outputgen,outputshrink)
    static member Contract() =
         let v0gen = Gen.map (fun (c:ContractP) -> {c with version=0u; _unknownFields=[]}) (Arb.Default.Derive<ContractP>().Generator)
         let vhgen =
             (Arb.Default.Derive<ContractP>().Generator)
             |> Gen.filter (fun (c:ContractP) -> c.version <> 0u)
             |> Gen.map (fun (c:ContractP) ->
                 {c with _unknownFields = List.filter (fun f -> not <| List.contains f.FieldNum [1;2;3]) c._unknownFields})
         let contractGen = Gen.oneof [v0gen;vhgen]
         let contractShrinker =
             Arb.Default.Derive<ContractP>().Shrinker
             >> Seq.filter (fun (c:ContractP) ->
                 (c.version <> 0u && not <| List.exists (fun (f:InnerField)-> List.contains f.FieldNum [1;2;3]) c._unknownFields )
                 || c._unknownFields.IsEmpty)
         Arb.fromGenShrink (contractGen, contractShrinker)

    static member Transaction() =
         let v0gen = Gen.map (fun (tx:TransactionP) -> {tx with version=0u; _unknownFields=[]}) (Arb.Default.Derive<TransactionP>().Generator)
         let vhgen =
             (Arb.Default.Derive<TransactionP>().Generator)
             |> Gen.filter (fun (tx:TransactionP) -> tx.version <> 0u)
             |> Gen.map (fun (tx:TransactionP) ->
                 {tx with _unknownFields = List.filter (fun f -> not <| List.contains f.FieldNum [1;2;3;4;5]) tx._unknownFields})
         let transactionGen = Gen.oneof [v0gen;vhgen]
         let transactionShrinker =
             Arb.Default.Derive<TransactionP>().Shrinker
             >> Seq.filter (fun (tx:TransactionP) ->
                 (tx.version <> 0u && not <| List.exists (fun (f:InnerField)-> List.contains f.FieldNum [1;2;3;4;5]) tx._unknownFields )
                 || tx._unknownFields.IsEmpty)
         Arb.fromGenShrink (transactionGen, transactionShrinker)

[<OneTimeSetUp>]
let setup = fun () ->
    Arb.register<ArbitraryModifiers>() |> ignore

[<Property>]
let ``Outpoint round-trips``(p:Outpoint) =
        p = (p |> toArray |> Deserialize.fromArray Outpoint.Default)

[<Property>]
let ``Spend round-trips``(s:Spend) =
        s = (s |> toArray |> Deserialize.fromArray Spend.Default)

[<Property>]
let ``Output lock round-trips``(l:OutputLockP) =
    let serializes (l:OutputLockP) =
        try
            let larr = toArray l
            true
        with
        | _ -> false
    let rtrip (l:OutputLockP) =
        l = (l |> toArray |> Deserialize.fromArray OutputLockP.Default)
    serializes l ==> lazy(rtrip l)

[<Property>]
let ``Output round-trips``(l:OutputP) =
    let serializes (l:OutputP) =
        try
            let larr = toArray l
            true
        with
        | _ -> false
    let rtrip (l:OutputP) =
        l = (l |> toArray |> Deserialize.fromArray OutputP.Default)
    serializes l ==> lazy(rtrip l)

[<Property(MaxTest = 250)>]
let ``Contract round-trips``(cn) =
    let serializes (c:ContractP) =
        try
            let carr = toArray c
            true
        with
        | _ -> false
    let rtrip (c:ContractP) =
        c = (c |> toArray |> Deserialize.fromArray ContractP.Default)
    serializes cn ==> lazy(rtrip cn)

[<Property>]
let ``Transaction round-trips``(t) =
    let serializes (tx:TransactionP) =
        try
            let txarr = toArray tx
            true
        with
        | _ -> false
    let rtrip (tx:TransactionP) =
        tx
        |> toArray
        |> Deserialize.fromArray TransactionP.Default
        |> fun x -> x = tx
    serializes t ==> lazy(rtrip t)

            