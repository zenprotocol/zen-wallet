
module NewCallOption

module V = Zen.Vector
module A = Zen.Array
module O = Zen.Option
module OT = Zen.OptionT
module ET = Zen.ErrorT
module U64 = FStar.UInt64
module Crypto = Zen.Crypto
module M = FStar.Mul

open Zen.Base
open Zen.Types
open Zen.Cost

let numeraire : cost hash 3 =
  Zen.Cost.inc (ret @ Zen.Util.hashFromBase64 "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=") 3

let price : U64.t = 100uL

type pointedOutput = outpoint * output

val tryAddPoint : outpoint -> utxos:utxo -> cost (result pointedOutput) 7
let tryAddPoint pt utxos =
  let open ET in
  Zen.Cost.inc (match utxos pt with
      | Some oput -> ret (pt, oput)
      | None -> failw "Cannot find output in UTXO set")
    7

val tryAddPoints :
  #l:nat -> V.t outpoint l -> utxos:utxo -> cost (result (V.t pointedOutput l)) M.(20 * l + 20)
let rec tryAddPoints #l v utxos =
  let open M in
  let open ET in
  Zen.Cost.inc (match v with
      | V.VNil ->
        let r : cost (result (V.t pointedOutput l)) (20 * l + 1) = Zen.Cost.inc (ret V.VNil) 1 in r
      | V.VCons pt rest ->
        begin match utxos pt with
          | Some oput ->
            do remainder <-- tryAddPoints rest utxos ;
            inc (ret @ V.VCons (pt, oput) remainder) 1
          | None -> 20 * l + 1 +! failw "Cannot find output in UTXO set" end)
    19

unopteq
type command =
  | Initialize of pointedOutput
  | Collateralize of V.t pointedOutput 2
  | Buy : V.t pointedOutput 2 -> outputLock -> command
  | Exercise : V.t pointedOutput 2 -> outputLock -> command






  (*val makeCommand : inputMsg -> cost (result command) 33
  let makeCommand imsg =
    let d : cost (n:nat & inputData n) 3 = Zen.Cost.inc (ret @ imsg.data) 3 in
    Zen.Cost.inc (do (| n , iData |) <-- d ;
        do cmd <-- ret @ imsg.cmd ;
        do utxos <-- ret @ imsg.utxo ;
        let open M in
        let open ET in
        Zen.Cost.inc (match cmd, n with
            | 0uy, 1 ->
              begin match iData with
                | Outpoint pt -> do pointed <-- tryAddPoint pt utxos ; ret @ Initialize pointed
                | _ -> (inc (failw "Bad Initialization data") 7) <: (cost (result command) 7) end
            | _ ->
              let failure : cost (result command) 1 = Zen.Cost.inc (failw "Not implemented") 1 in
              (Zen.Cost.inc (inc failure 4) 2) <: (cost (result command) 7))
          14)
      9*)





val makeCommand : inputMsg -> cost (result command) 33
let makeCommand imsg =
  let d : cost (n:nat & inputData n) 3 = Zen.Cost.inc (ret @ imsg.data) 3 in
  Zen.Cost.inc (do (| n , iData |) <-- d ;
      do cmd <-- ret @ imsg.cmd ;
      do utxos <-- ret @ imsg.utxo ;
      let open M in
      let open ET in
      Zen.Cost.inc (match cmd, n with
          | 0uy, 1 ->
            begin match iData with
              | Outpoint pt -> do pointed <-- tryAddPoint pt utxos ; ret @ Initialize pointed
              | _ -> inc (failw "Bad Initialization data") 7  <: (cost (result command) 7) end
          | _ -> inc (failw "Not implemented") 7  <: (cost (result command) 7))
        14)
    9
