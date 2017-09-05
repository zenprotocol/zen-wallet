module Consensus.TemporarySerializationTests
// Until FSCheck 2.10.0, we're stuck with an NUnit version issue.
// This module does property checks *without* NUnit syntax.

open NUnit.Framework
open FsCheck
//open FsCheck.NUnit
open Froto.Serialization
open Types

type ArbitraryModifiers =
    static member ArrSeg() =
        Arb.from<byte[]>
        |> Arb.convert System.ArraySegment (fun s -> s.Array.[s.Offset .. s.Offset+s.Count-1])

[<Test>]
let ``Outpoint round-trips``() =
    let rtrip (p:Outpoint) =
        p
        |> Serialize.toArray
        |> Deserialize.fromArray Outpoint.Default
        |> (fun x -> x = p)
    Check.QuickThrowOnFailure rtrip

[<Test>]
let ``Spend round-trips``() =
    let rtrip (s:Spend) =
        s
        |> Serialize.toArray
        |> Deserialize.fromArray Spend.Default
        |> (fun x -> x = s)
    Check.QuickThrowOnFailure rtrip

[<Test>]
let ``Output lock round-trips``() =
    Arb.register<ArbitraryModifiers>() |> ignore

    let serializes (l:OutputLockP) =
        try
            let larr = Serialize.toArray l
            true
        with
        | _ -> false
    let rtrip (l:OutputLockP) =
        let larr = Serialize.toArray l
        let res = Deserialize.fromArray OutputLockP.Default larr
        l = res
    let ``round trips if serializes`` l = serializes l ==> lazy(rtrip l)
    Check.QuickThrowOnFailure ``round trips if serializes``

[<Test>]
let ``Output round-trips``() =
    Arb.register<ArbitraryModifiers>() |> ignore

    let serializes (l:OutputP) =
        try
            let larr = Serialize.toArray l
            true
        with
        | _ -> false
    let rtrip (l:OutputP) =
        l
        |> Serialize.toArray
        |> Deserialize.fromArray OutputP.Default
        |> (fun x ->
                if x <> l then printfn "fail: %A, %A" x l
                x = l)
    let ``round trips if serializes`` l = serializes l ==> lazy(rtrip l)
    Check.One (
        {Config.QuickThrowOnFailure with MaxTest = 1000},
        ``round trips if serializes``
        )
    //Check.QuickThrowOnFailure ``round trips if serializes``