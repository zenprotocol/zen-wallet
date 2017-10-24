
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

val makeCommand : inputMsg -> cost (result command) 44
let makeCommand { cmd = cmd ; data = iData ; utxo = utxos } =
  let open M in
  let open ET in
  Zen.Cost.inc (match cmd with
      | 0uy ->
        begin match iData with
          | (| 1 , Outpoint pt |) ->
            do pointed <-- tryAddPoint pt utxos ;
            incRet 7 (Initialize pointed)
          | (| 1 , _ |) -> incFailw 14 "Bad Initialization data"
          | (| 2 , OutpointVector _ [|outpoint0 ; outpoint1|] |) ->
            do pointedOutput0 <-- tryAddPoint outpoint0 utxos ;
            do pointedOutput1 <-- tryAddPoint outpoint1 utxos ;
            ret @ Collateralize [|pointedOutput0; pointedOutput1|]
          | (| 2 , _ |) -> incFailw 14 "Bad Collateralization data"
          | _ -> incFailw 14 "Bad Initialization/Collateralization data" end
      | 1uy ->
        begin match iData with
          | (| 3 , Data2 _ _ (OutpointVector _ [|outpoint0 ; outpoint1|]) (OutputLock lk) |) ->
            do pointedOutput0 <-- tryAddPoint outpoint0 utxos ;
            do pointedOutput1 <-- tryAddPoint outpoint1 utxos ;
            ret @ Buy [|pointedOutput0; pointedOutput1|] lk
          | _ -> incFailw 14 "Bad Buy Data" end
      | 2uy ->
        begin match iData with
          | (| 3 , Data2 _ _ (OutpointVector _ [|outpoint0 ; outpoint1|]) (OutputLock lk) |) ->
            do pointedOutput0 <-- tryAddPoint outpoint0 utxos ;
            do pointedOutput1 <-- tryAddPoint outpoint1 utxos ;
            ret @ Exercise [|pointedOutput0; pointedOutput1|] lk
          | _ -> incFailw 14 "Bad Exercise Data" end
      | _ -> incFailw 14 "Not implemented")
    30


type state = { tokensIssued:U64.t; collateral:U64.t; counter:U64.t }

val encodeState : state -> cost (inputData 3) 10
let encodeState { tokensIssued = tokensIssued ; collateral = collateral ; counter = counter } =
  Zen.Cost.inc (ret @ UInt64Vector 3 [|tokensIssued; collateral; counter|]) 10

val decodeState : #n:nat -> inputData n -> cost (result state) 16
let decodeState #n iData =
  let open ET in
  Zen.Cost.inc (if n <> 3
      then autoFailw "Bad data"
      else
        match iData with
        | UInt64Vector 3 [|tk ; coll ; cter|] ->
          ret @ { tokensIssued = tk; collateral = coll; counter = cter }
        | _ -> autoFailw "Bad data")
    16

val createTx : hash -> command -> cost (result transactionSkeleton) 123
let createTx cHash cmd =
  Zen.Cost.inc (do numeraire <-- numeraire ;
      let open ET in
      let open U64 in
      Zen.Cost.inc (match cmd with
          | Initialize (pt, oput) ->
            if oput.spend.asset = numeraire
            then
              let initialState : state =
                { tokensIssued = 0uL; collateral = oput.spend.amount; counter = 0uL }
              in
              do initialStateData <-- inc (retT @ encodeState initialState) 6 ;
              let dataOutputLock = ContractLock cHash 3 initialStateData in
              let dataOutput = { lock = dataOutputLock; spend = oput.spend } in
              autoRet @ Tx [|pt|] [|dataOutput|] None
            else autoFailw "Can't initialize with this asset."
          | Collateralize [|pt1, dataOutput ; pt2, newFundsOutput|] ->
            if dataOutput.spend.asset = numeraire && newFundsOutput.spend.asset = numeraire
            then
              match dataOutput.lock, newFundsOutput.lock with
              | ContractLock cHash 3 currentStateData, ContractLock cHash _ _ ->
                do currentState <-- decodeState currentStateData ;
                let newCollateral = currentState.collateral +%^ newFundsOutput.spend.amount in
                let newState =
                  {
                    tokensIssued = currentState.tokensIssued;
                    collateral = newCollateral;
                    counter = currentState.counter +%^ 1uL
                  }
                in
                do newStateData <-- retT @ encodeState newState ;
                let newDataOutputLock = ContractLock cHash 3 newStateData in
                let newDataOutput =
                  {
                    lock = newDataOutputLock;
                    spend = { asset = numeraire; amount = newCollateral }
                  }
                in
                ret @ Tx [|pt1; pt2|] [|newDataOutput|] None
              | _, _ -> autoFailw "Inputs not locked to this contract!"
            else autoFailw "Can't use these asset types for Collateralize"
          | Buy [|pt1, dataOutput ; pt2, purchaseOutput|] lk ->
            if dataOutput.spend.asset = numeraire && purchaseOutput.spend.asset = numeraire
            then
              match dataOutput.lock, purchaseOutput.lock with
              | ContractLock cHash 3 currentStateData, ContractLock cHash _ _ ->
                do currentState <-- decodeState currentStateData ;
                let newCollateral = currentState.collateral +%^ purchaseOutput.spend.amount in
                let newTokens = purchaseOutput.spend.amount /^ price in
                let newState =
                  {
                    tokensIssued = currentState.tokensIssued +%^ newTokens;
                    collateral = newCollateral +%^ newCollateral;
                    counter = currentState.counter
                  }
                in
                do newStateData <-- retT @ encodeState newState ;
                let newDataOutputLock = ContractLock cHash 3 newStateData in
                let newDataOutput =
                  {
                    lock = newDataOutputLock;
                    spend = { asset = numeraire; amount = newCollateral }
                  }
                in
                let buyersOutput = { lock = lk; spend = { asset = cHash; amount = newTokens } } in
                ret @ Tx [|pt1; pt2|] [|newDataOutput; buyersOutput|] None
              | _, _ -> autoFailw "Inputs not locked to this contract!"
            else autoFailw "Can't buy with these assets."
          | Exercise [|pntd ; pntd'|] lk -> autoFailw "Exercise")
        93)
    1


val main : inputMsg -> cost (result transactionSkeleton) 1
let main iM = Zen.Cost.inc (ET.failw "Not implemented") 1

val cf : inputMsg -> cost nat 1
let cf _ = Zen.Cost.inc ~!1 1
val mainFunction : Zen.Types.mainFunction
let mainFunction = Zen.Types.MainFunc (Zen.Types.CostFunc cf) main