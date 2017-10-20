
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

val makeCommand : inputMsg -> cost (result command) 18
let makeCommand imsg =
  Zen.Cost.inc (do cmd <-- ret @ imsg.cmd ;
      do d <-- ret @ imsg.data ;
      let open ET in
      Zen.Cost.inc (match cmd, d with
          | 0uy, _ -> failw "One"
          | 1uy, _ -> failw "Two"
          | 2uy, _ -> failw "Three"
          | _ -> failw "Bad or unknown command")
        10)
    8