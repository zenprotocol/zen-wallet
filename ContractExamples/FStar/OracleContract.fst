module ZenModule

open Zen.Base
open Zen.Cost
open Zen.Types
open Zen.ErrorT

val parse_outpoint: n:nat & inputData n -> cost (result outpoint) 9
let parse_outpoint d = match d with // point-free function syntax doesn't elaborate correctly
  | (| _ , Data2 _ _ (Outpoint o) _ |) -> ret o
  | _ -> failw "Wrong data format in inputmsg"

val parse_data: m:nat & inputData m -> cost (result (n:nat & data n)) 10
let parse_data d = match d with // point-free function syntax doesn't elaborate correctly
  | (| _ , Data2 _ n _ d |) -> ret (| n, d |)
  | _ -> failw "Wrong data format in inputmsg"

val parse_output: hash -> output -> cost (result spend) 12
let parse_output oracleCHash output =
  match output with
  | { lock=ContractLock outputCHash _ _;
      spend=outputSpend }
      -> if outputCHash = oracleCHash
        then ret outputSpend
        else failw "wrong contract lock"
  | _ -> failw "wrong output fomat"

val main: inputMsg -> cost (result transactionSkeleton) 66
let main { data=inputData; contractHash=oracleCHash; utxo=utxo } =
  do outpoint <-- parse_outpoint inputData;
  do data <-- parse_data inputData;
  do outputSpend <-- begin match utxo outpoint with
                     | Some output -> parse_output oracleCHash output
                     | None -> 12 +! failw "could not resolve utxo of outpoint"
                     end;

  let (| inputDataPoints, inputData |) = data in
  let chainedOutputLock = ContractLock oracleCHash 0 Empty in
  let    dataOutputLock = ContractLock oracleCHash inputDataPoints inputData in

  let chainedOutput = { lock=chainedOutputLock; spend=outputSpend } in
  let    dataOutput = { lock=dataOutputLock;
                        spend={asset=oracleCHash; amount=1UL} } in

  ret@Tx [|outpoint|]
         [|chainedOutput; dataOutput|]
         None

val cf: inputMsg -> cost nat 1
let cf _ = ~!66
