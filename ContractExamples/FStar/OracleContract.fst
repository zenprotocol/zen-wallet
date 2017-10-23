module OracleContract

open Zen.Base
open Zen.Cost
open Zen.Types
open Zen.ErrorT

val parse_outpoint: n:nat & inputData n -> cost (result outpoint) 5
let parse_outpoint d = match d with // point-free function syntax doesn't elaborate correctly
  | (| _ , Outpoint o |) -> ret o
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


val main: inputMsg -> cost (result transactionSkeleton) 50
let main { data=inputData; contractHash=oracleCHash; utxo=utxo } =
  do outpoint <-- parse_outpoint inputData;
  do outputSpend <-- begin match utxo outpoint with
                     | Some output -> parse_output oracleCHash output
                     | None -> 12 +! failw "could not resolve utxo of outpoint"
                     end;

  let (| inputDataPoints, inputData |) = inputData in
  let chainedOutputLock = ContractLock oracleCHash 0 Empty in
  let    dataOutputLock = ContractLock oracleCHash inputDataPoints inputData in

  let chainedOutput = { lock=chainedOutputLock; spend=outputSpend } in
  let    dataOutput = { lock=dataOutputLock;
                        spend={asset=oracleCHash; amount=0UL} } in

  ret@Tx [|outpoint|]
         [|chainedOutput; dataOutput|]
         None

val cf: inputMsg -> cost nat 1
let cf _ = ~!50
