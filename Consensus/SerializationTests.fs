namespace Consensus

open NUnit.Framework
open FsCheck
open FsCheck.NUnit
open Froto.Serialization
open Types


[<TestFixture>]
type SerializationTests() =

    [<Test>]
    member __.NormalTest() = 
        ignore true

    [<Property( Verbose = true )>]
    member __.``Outpoint round-trips cleanly`` (x:Outpoint) =
        x |> Serialize.toArray |> Deserialize.fromArray Outpoint.Default <> x
    
    [<Property(Verbose = true)>]
    member __.``Should fail`` (xs:int[]) =
        xs = Array.rev xs