module ZenModule

open Zen.Base
open Zen.Cost
open Zen.Types
open Zen.ErrorT

val parse_outpoint: n:nat & inputData n -> cost (result (outpoint * hash)) 12
let parse_outpoint d = match d with // point-free function syntax doesn't elaborate correctly
  | (| _ , Data2 _ _ (Outpoint outpoint) (Hash hash) |) -> ret (outpoint, hash)
  | _ -> failw "Wrong data format in outpoint"


val parse_output: hash -> output -> cost (result spend) 12
let parse_output oracleCHash output =
  match output with
  | { lock=ContractLock outputCHash _ _;
      spend=outputSpend }
      -> if outputCHash = oracleCHash
        then ret outputSpend
        else failw "wrong contract lock"
  | _ -> failw "wrong output fomat"


val main: inputMsg -> cost (result transactionSkeleton) 58
let main { data=inputData; contractHash=oracleCHash; utxo=utxo } =
  do (outpoint, dataHash) <-- parse_outpoint inputData;
  do outputSpend <-- begin match utxo outpoint with
                     | Some output -> parse_output oracleCHash output
                     | None -> 12 +! failw "could not resolve utxo of outpoint"
                     end;

  let (| inputDataPoints, inputData |) = inputData in
  let chainedOutputLock = ContractLock oracleCHash 0 Empty in
  let    dataOutputLock = ContractLock oracleCHash 1 (Hash dataHash) in

  let chainedOutput = { lock=chainedOutputLock; spend=outputSpend } in
  let    dataOutput = { lock=dataOutputLock;
                        spend={asset=oracleCHash; amount=1UL} } in

  ret@Tx [|outpoint|]
         [|chainedOutput; dataOutput|]
         None

val cf: inputMsg -> cost nat 1
let cf _ = ~!58
