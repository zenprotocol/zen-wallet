module ZenModule

module V = Zen.Vector
module O = Zen.Option
module OT = Zen.OptionT
module ET = Zen.ErrorT

open Zen.Types
open Zen.Cost

val parse_outpoint: n:nat & inputData n -> cost (option outpoint) 5
let parse_outpoint d = match d with // point-free function syntax doesn't elaborate correctly
  | (| _ , Outpoint o |) -> OT.some o
  | _ -> OT.none

val cost_fn: inputMsg -> cost nat 1
let cost_fn _ = ret 45

val main_fn: inputMsg -> cost (result transactionSkeleton) 45
let main_fn i =
  //let open OT in
  do parsed <-- parse_outpoint i.data;
  match parsed with
    | Some outpoint ->
        begin match i.utxo outpoint with
          | Some output ->
            do outpoints <-- ret [| outpoint |];
            do lock <-- ret (PKLock (Zen.Util.hashFromBase64 "AAEECRAZJDFAUWR5kKnE4QAhRGmQueQRQHGk2RBJhME="));
            do connotativeOutput <-- ret ({
              lock = lock;
              spend = output.spend
            });
            do tokenOutput <-- ret ({
              lock = lock;
              spend = {
                asset = i.contractHash;
                amount = 1000UL
              }
            });
            ret (V (Tx outpoints [| tokenOutput; connotativeOutput |] None))
          | None -> ret (Err "Cannot resolve outpoint")
        end
    | None -> ret (Err "Cannot parse outpoint")
