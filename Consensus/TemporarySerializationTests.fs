module Consensus.TemporarySerializationTests
// Until FSCheck 2.10.0, we're stuck with an NUnit version issue.
// This module does property checks *without* NUnit syntax.

open NUnit.Framework
open FsCheck
//open FsCheck.NUnit
open Froto.Serialization
open Types

let filteredInnerGen (toFilter:Set<int32>) =
    let fieldGen = Arb.generate<int32> |> Gen.filter (fun i -> not <| Set.contains i toFilter )
    let lgen = Gen.zip fieldGen Arb.generate<uint64>
    let igen = Gen.zip fieldGen Arb.generate<uint32>
    let ldgen = Gen.zip fieldGen Arb.generate<byte[]>
    let selectgen = Gen.frequency <|
                    [
                        (1, gen{return 0});
                        (1, gen{return 1});
                        (1, gen{return 2});
                        (3, gen{return 3})
                    ]
    gen {
        let! choice = selectgen
        match choice with
        | 0 -> return! Gen.map V lgen
        | 1 -> return! Gen.map F32 igen
        | 2 -> return! Gen.map F64 lgen
        | 3 -> return! Gen.map LD ldgen
    }

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
     //static member Transaction() =
         //let igen = Gen.listOf<Outpoint>
         //let witgen = Gen.listOf<byte[]>
         //let ogen = Gen.listOf<OutputP>

[<Test>]
let ``Outpoint round-trips``() =
    let rtrip (p:Outpoint) =
        p
        |> toArray
        |> Deserialize.fromArray Outpoint.Default
        |> (fun x -> x = p)
    Check.QuickThrowOnFailure rtrip

[<Test>]
let ``Spend round-trips``() =
    let rtrip (s:Spend) =
        s
        |> toArray
        |> Deserialize.fromArray Spend.Default
        |> (fun x -> x = s)
    Check.QuickThrowOnFailure rtrip

[<Test>]
let ``Output lock round-trips``() =
    Arb.register<ArbitraryModifiers>() |> ignore

    let serializes (l:OutputLockP) =
        try
            let larr = toArray l
            true
        with
        | _ -> false
    let rtrip (l:OutputLockP) =
        let larr = toArray l
        let res = Deserialize.fromArray OutputLockP.Default larr
        l = res
    let ``round trips if serializes`` l = serializes l ==> lazy(rtrip l)
    Check.QuickThrowOnFailure ``round trips if serializes``

[<Test>]
let ``Output round-trips``() =
    Arb.register<ArbitraryModifiers>() |> ignore

    let serializes (l:OutputP) =
        try
            let larr = toArray l
            true
        with
        | _ -> false
    let rtrip (l:OutputP) =
        l
        |> toArray
        |> Deserialize.fromArray OutputP.Default
        |> (fun x ->
                if x <> l then printfn "fail: %A, %A" x l
                x = l)
    let ``round trips if serializes`` l = serializes l ==> lazy(rtrip l)
    Check.One (
        {Config.QuickThrowOnFailure with MaxTest = 250},
        ``round trips if serializes``
        )
    //Check.QuickThrowOnFailure ``round trips if serializes``

[<Test>]
let ``Contract round-trips``() =
    Arb.register<ArbitraryModifiers>() |> ignore

    let serializes (c:ContractP) =
        try
            let carr = toArray c
            true
        with
        | _ -> false
    let rtrip (c:ContractP) =
        c
        |> toArray
        |> Deserialize.fromArray ContractP.Default
        |> fun x -> x = c
    let ``round trips if serializes`` c = serializes c ==> lazy(rtrip c)
    Check.One (
        {Config.QuickThrowOnFailure with MaxTest = 250},
        ``round trips if serializes``
        )
    //Check.QuickThrowOnFailure ``round trips if serializes``

[<Test>]
let ``Transaction round-trips``() =
    Arb.register<ArbitraryModifiers>() |> ignore

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
    let ``round trips if serializes`` tx = serializes tx ==> lazy(rtrip tx)
    Check.One (
        {Config.QuickThrowOnFailure with MaxTest = 100},
        ``round trips if serializes``
        )