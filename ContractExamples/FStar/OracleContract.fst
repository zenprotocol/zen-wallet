module OracleContract

open Zen.Base
open Zen.Cost
open Zen.Types
open Zen.ErrorT

val parse_outpoint: n:nat & inputData n -> cost (result outpoint) 5
let parse_outpoint d = match d with // point-free function syntax doesn't elaborate correctly
  | (| _ , Outpoint o |) -> ret o
  | _ -> failw "Wrong data format in outpoint"

val parse_output: output -> cost (result (n:nat & data:data n & spend)) 10
let parse_output output =
  match output with
  | { lock=ContractLock outputCHash n data;
      spend = outputSpend }
      ->  ret (| n, data, outputSpend |)
  | _ -> failw "wrong output fomat"

val main: inputMsg -> cost (result transactionSkeleton) 62
let main { data=inputData; contractHash=oracleCHash; utxo=utxo } =
  do outpoint <-- parse_outpoint inputData;
  do parsed_output <-- begin match utxo outpoint with
                       | Some output -> parse_output output
                       | None -> 10 +! failw "could not resolve utxo of outpoint"
                       end;
  let (| outputDataPoints, outputData, outputSpend |) = parsed_output in
  do oracleCLock <-- ret @ ContractLock oracleCHash outputDataPoints outputData;
  do connotativeOutput <-- ret @ { lock=oracleCLock; spend=outputSpend };

  let pk = "AAEECRAZJDFAUWR5kKnE4QAhRGmQueQRQHGk2RBJhME=" in
  do returnLock  <-- ret @ PKLock (Zen.Util.hashFromBase64 pk);
  do returnSpend <-- ret @ { asset=oracleCHash; amount=0UL};
  do dataOutput  <-- ret @ { lock=returnLock; spend=returnSpend };

  ret @ Tx [|outpoint|]
           [|dataOutput; connotativeOutput|]
           None

val cf: inputMsg -> cost nat 1
let cf _ = ~!62
