
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
  #l:nat -> V.t outpoint l -> utxos:utxo -> cost (result (V.t pointedOutput l)) M.(17 * l + 17)
let rec tryAddPoints #l v utxos =
  let open M in
  let open ET in
  Zen.Cost.inc (match v with
      | V.VNil -> let r : cost (result (V.t pointedOutput l)) (17 * l) = ret V.VNil in r
      | V.VCons pt rest ->
        begin match utxos pt with
          | Some oput ->
            do remainder <-- tryAddPoints rest utxos ;
            ret @ V.VCons (pt, oput) remainder
          | None -> 17 * l +! failw "Cannot find output in UTXO set" end)
    17

unopteq
type command =
  | Initialize of pointedOutput
  | Collateralize of V.t pointedOutput 2
  | Buy : V.t pointedOutput 2 -> outputLock -> command
  | Exercise : V.t pointedOutput 2 -> outputLock -> command

val makeCommand : inputMsg -> cost (result command) 40
let makeCommand imsg =
  Zen.Cost.inc (let d : cost (n:nat & inputData n) 0 = ret @ imsg.data in
      do (| n , iData |) <-- d ;
      do cmd <-- ret @ imsg.cmd ;
      do utxos <-- ret @ imsg.utxo ;
      let open M in
      let open ET in
      Zen.Cost.inc (match cmd, n with
          | 0uy, 1 ->
            begin match iData with
              | Outpoint pt -> do pointed <-- tryAddPoint pt utxos ; ret @ Initialize pointed
              | _ -> autoFailw "Bad Initialization data" end
          | 0uy, 2 ->
            begin match iData with
              | OutpointVector 2 v -> autoFailw "Coll"
              | _ -> autoFailw "Bad Collateralization data" end
          | 1uy, 2 ->
            begin match iData with
              | Data2 _ _ (OutpointVector 2 v) (OutputLock lk) -> autoFailw "Buy"
              | _ -> autoFailw "Bad Buy Data" end
          | 2uy, 2 ->
            begin match iData with
              | Data2 _ _ (OutpointVector 2 v) (OutputLock lk) -> autoFailw "Exercise"
              | _ -> autoFailw "Bad Exercise Data" end
          | _ -> autoFailw "Not implemented")
        21)
    12










































val main : inputMsg -> cost (result transactionSkeleton) 1
let main iM = Zen.Cost.inc (ET.failw "Not implemented") 1

val cf : inputMsg -> cost nat 1
let cf _ = Zen.Cost.inc ~!1 1
val mainFunction : Zen.Types.mainFunction
let mainFunction = Zen.Types.MainFunc (Zen.Types.CostFunc cf) main