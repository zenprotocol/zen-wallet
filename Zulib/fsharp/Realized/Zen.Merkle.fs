module Zen.Merkle

open System
open Zen.Types.Extracted

module ZArr = Zen.Array.Extracted
module Cost = Zen.Cost.Realized
module sha3 = Zen.Sha3.Realized

let private serialize = function
    // Oracle data structure
    | Data4 (_, _, _, _,
      ByteArray (_, underlyingBytes),
      UInt64 price,
      UInt64 timestamp,
      Hash nonce) ->
        [ underlyingBytes; BitConverter.GetBytes price; BitConverter.GetBytes timestamp ]
        |> List.fold (fun acc elem -> Array.append elem acc) [||]
        |> Some
    | _ -> None

let getDataHash
    ( _ : Prims.nat)
    data
    : Cost.t<hash option, Prims.unit> =
        lazy (
            match serialize data with
            | None -> None
            | Some bytes -> bytes |> sha3.get256 |> Some
//TODO: use Option.bind
//            serialize data
//            |> Option.bind (fun bytes -> bytes |> sha3.get256 |> Some)
        )
        |> Cost.C

let getRootFromAuditPath
    ( _: Prims.nat)
    ( item : hash )
    ( location: Prims.nat )
    ( hashes : ZArr.t<hash, Prims.unit> )
    : Cost.t<hash, Prims.unit> =
        lazy (
            fst <|
            Array.fold
                (fun (v, loc) h ->
                    if loc % 2u = 0u
                    then
                        (sha3.get256 <| Array.append v h, loc >>> 1)
                    else
                        (sha3.get256 <| Array.append h v, loc >>> 1))
                (item,uint32 location) hashes
        )
        |> Cost.C
