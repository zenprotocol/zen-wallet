module TokenSale

//open Zen.Base
open Zen.Cost
open Consensus.Types
open Zen.OptionT
module T = Zen.Tuple
module Opt = Zen.Option
module U64 = FStar.UInt64
module Vec = Zen.Vector

(**
  commands: 0 -> initialise
            1 -> buy
*)
type command =
  | Initialize
  | Buy
  | Fail
assume val zen: hash
assume val token: hash
assume val ownerlock: hash
assume val price: n:U64.t{n <> 0uL}

val mkspend: hash -> U64.t -> spend
let mkspend asset amount = {asset=asset; amount=amount}

val mkoutput: outputLock -> spend -> output
let mkoutput lock spend = {lock=lock; spend=spend}

val parse_commands: opcode -> cost command 5
let parse_commands opcode = incRet 5 (if opcode = 0uy then Initialize
                                 else if opcode = 1uy then Buy else Fail)

val parse_outpoint: n:nat & inputData n -> cost (option outpoint) 4
let parse_outpoint = function
  | (| 2, Data2 _ _ (Outpoint outPoint) _ |) -> incSome 4 outPoint
  | _ -> incNone 4

val parse_outputLock: n:nat & inputData n -> cost (option outputLock) 4
let parse_outputLock = function
  | (| 2, Data2 _ _ _ (OutputLock outputLock)|) -> incSome 4 outputLock
  | _ -> incNone 4

val totalInputZen: utxo -> outpoint -> cost (option U64.t) 5
let totalInputZen utxo outpoint = match utxo outpoint with
  | Some output -> if output.spend.asset = zen
                 then incSome 5 output.spend.amount else incNone 5
  | None -> incNone 5

val issueTokens: U64.t -> cost U64.t 2
let issueTokens zen = incRet 2 U64.(zen /^ price)

val mkTx: n:nat -> Vec.t outpoint 2 -> Vec.t output n -> cost transactionSkeleton 2
let mkTx n returnOutpoints outputs =
  incRet 2 (Tx 2 returnOutpoints n outputs 0 Empty)

val main: inputMsg -> cost (option transactionSkeleton) 37
let main inputMsg =
  let outPoint   = parse_outpoint inputMsg.data in
  let outputLock = parse_outputLock inputMsg.data in
  let output0 = inputMsg.lastTx `Opt.bind` inputMsg.utxo in
  let inputZen = totalInputZen inputMsg.utxo =<< outPoint in
  let returnOutpoints = bindLift2 (~!inputMsg.lastTx) outPoint (T.curry Vec.of_t2) in
  let tokensIssued = inputZen `bindLift` issueTokens in
  let returnZen = mkoutput (PKLock ownerlock) <$> (mkspend zen <$> inputZen) in
  let tokenSpend = mkspend inputMsg.contractHash <$> tokensIssued in
  let outputTokens = mkoutput <$> outputLock <*> tokenSpend in
  match output0 with
  | None ->
    let outputs = bindLift2 returnZen outputTokens (T.curry Vec.of_t2) in
    1 +! (bindLift2 returnOutpoints outputs (mkTx 2))
  | Some output0 ->
    let outputs = bindLift2 returnZen outputTokens ((T.curry3 Vec.of_t3) output0) in
    (bindLift2 returnOutpoints outputs (mkTx 3))
(*)
      Cow after pulling an all-nighter

        (__)       (----------)
        (--) . . . ( *>YAWN<* )
  /------\/        (----------)
 /|     ||
* ||----||
