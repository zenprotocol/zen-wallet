module ZenModule

module V = Zen.Vector
module O = Zen.Option

open Zen.Types
open Zen.Cost

val parse_outpoint: n:nat & inputData n -> option outpoint
let parse_outpoint = function
  | (| _ , Outpoint o |) -> Some o
  | _ -> None

(*
TODO:
val failWith(#a:Type): string -> result a
let failWith(#_) = Err

exception FAIL1
*)

val main: inputMsg -> cost (result transactionSkeleton) 0
let main i =
  let open O in

  let resTx = match parse_outpoint i.data with
    | Some outpoint ->
        begin match i.utxo outpoint with
          | Some output ->
            let outpoints = V.VCons outpoint V.VNil in
            let outputs = V.VCons output V.VNil in
            let tokenOutput = {
              lock = PKLock (Zen.Util.hashFromBase64 "AAEECRAZJDFAUWR5kKnE4QAhRGmQueQRQHGk2RBJhME=");
                spend = {
                  asset = i.contractHash;
                  amount = 1000UL
                }
            } in
            let outputs = V.VCons tokenOutput outputs in
            V (Tx outpoints outputs None)
          | None -> Err "Cannot resolve outpoint"
        end
    | None -> Err "Cannot parse outpoint" in

  ret resTx
