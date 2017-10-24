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

let numeraire: cost hash 3 = ret @ Zen.Util.hashFromBase64 "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="

let price: U64.t = 100UL

type pointedOutput = outpoint * output

val tryAddPoint: outpoint -> utxos:utxo -> cost (result pointedOutput) 7
let tryAddPoint pt utxos =
  let open ET in
  match utxos pt with
  | Some oput -> ret (pt, oput)
  | None -> failw "Cannot find output in UTXO set"

val tryAddPoints: #l:nat ->      //TODO: use tryAddPoint
                V.t outpoint l ->
                utxos:utxo ->
                cost (result (V.t pointedOutput l)) M.((17*l + 17))
let rec tryAddPoints #l v utxos =
  let open M in
  let open ET in
    match v with
    | V.VNil -> let r:cost (result (V.t pointedOutput l)) (17*l) = ret V.VNil in
      r
    | V.VCons pt rest ->
        match utxos pt with
        | Some oput ->
          do remainder <-- tryAddPoints rest utxos;
          ret @ V.VCons (pt, oput) remainder
        | None ->
          (17 * l) +! failw "Cannot find output in UTXO set"

unopteq type command =
  | Initialize of pointedOutput
  | Collateralize of V.t pointedOutput 2
  | Buy: (V.t pointedOutput 2) -> outputLock -> command
  | Exercise: (V.t pointedOutput 2) -> outputLock -> command

val makeCommand : inputMsg -> cost (result command) 44
let makeCommand {cmd=cmd; data=iData; utxo=utxos} =
  let open M in
  let open ET in
  match cmd with
  | 0uy -> begin match iData with
           | (| 1, Outpoint pt |) ->
             do pointed <-- tryAddPoint pt utxos;
             incRet 7 (Initialize pointed)
           | (| 1, _ |) -> incFailw 14 "Bad Initialization data"
           | (| 2, OutpointVector _ [| outpoint0; outpoint1 |] |) ->
             do pointedOutput0 <-- tryAddPoint outpoint0 utxos;
             do pointedOutput1 <-- tryAddPoint outpoint1 utxos;
             ret @ Collateralize [| pointedOutput0; pointedOutput1 |]
           | (| 2, _ |) -> incFailw 14 "Bad Collateralization data"
           | _ -> incFailw 14 "Bad Initialization/Collateralization data"
           end
  | 1uy -> begin match iData with
           | (|3, Data2 _ _ (OutpointVector _ [| outpoint0; outpoint1 |] )
                            (OutputLock lk) |) ->
             do pointedOutput0 <-- tryAddPoint outpoint0 utxos;
             do pointedOutput1 <-- tryAddPoint outpoint1 utxos;
             ret @ Buy [| pointedOutput0; pointedOutput1 |] lk
           | _ -> incFailw 14 "Bad Buy Data"
           end
  | 2uy -> begin match iData with
           | (|3, Data2 _ _ (OutpointVector _ [| outpoint0; outpoint1 |] )
                            (OutputLock lk) |) ->
             do pointedOutput0 <-- tryAddPoint outpoint0 utxos;
             do pointedOutput1 <-- tryAddPoint outpoint1 utxos;
             ret @ Exercise [| pointedOutput0; pointedOutput1 |] lk
           | _ -> incFailw 14 "Bad Exercise Data"
           end
  | _ ->  incFailw 14 "Not implemented"


type state = { tokensIssued : U64.t;
               collateral   : U64.t;
               counter      : U64.t }

val encodeState : state -> cost (inputData 3) 10
let encodeState {
                  tokensIssued=tokensIssued;
                  collateral=collateral;
                  counter=counter
                } =
                  ret @ UInt64Vector 3 [| tokensIssued;collateral;counter |]

val decodeState : #n:nat -> inputData n -> cost (result state) 16
let decodeState #n iData =
  let open ET in
  if n <> 3 then autoFailw "Bad data"
  else
    match iData with
    | UInt64Vector 3 [| tk; coll; cter |] ->
      ret @ {tokensIssued=tk; collateral=coll; counter=cter}
    | _ -> autoFailw "Bad data"

val createTx : hash -> command -> cost (result transactionSkeleton) 123
let createTx cHash cmd =
  do numeraire <-- numeraire;
  let open ET in
  let open U64 in
  match cmd with
  | Initialize (pt, oput) ->
      if oput.spend.asset = numeraire
      then      // Initialize with data output
        let initialState : state =
          {
            tokensIssued=0UL;
            collateral=oput.spend.amount;
            counter=0UL;
          } in
        do initialStateData <-- inc (retT @ encodeState initialState) 6;
        let dataOutputLock = ContractLock cHash 3 initialStateData in
        let dataOutput = {lock=dataOutputLock;spend=oput.spend} in
        autoRet @ Tx [| pt |] [| dataOutput |] None
        (*failw "Init"*)
      else autoFailw "Can't initialize with this asset."
  | Collateralize [| (pt1,dataOutput); (pt2,newFundsOutput) |] ->
    if dataOutput.spend.asset = numeraire && newFundsOutput.spend.asset = numeraire
    then
      begin match dataOutput.lock, newFundsOutput.lock with
      | ContractLock cHash 3 currentStateData, ContractLock cHash _ _ ->
          do currentState <-- decodeState currentStateData;
          // TODO: avoid modular addition!!
          let newCollateral = currentState.collateral +%^ newFundsOutput.spend.amount in
          let newState = {
            tokensIssued = currentState.tokensIssued;
            collateral = newCollateral;
            counter = currentState.counter +%^ 1UL;   // We actually prefer modular arithmetic for the counter
          } in
          do newStateData <-- retT @ encodeState newState;
          let newDataOutputLock = ContractLock cHash 3 newStateData in
          let newDataOutput =
            {
              lock=newDataOutputLock;
              spend={asset=numeraire;amount=newCollateral}
            } in
          ret @ Tx
                  [| pt1; pt2 |]
                  [| newDataOutput |]
                  None
      | _,_ -> autoFailw "Inputs not locked to this contract!"
      end
    else autoFailw "Can't use these asset types for Collateralize"
  | Buy [| (pt1, dataOutput); (pt2, purchaseOutput) |] lk ->
    if dataOutput.spend.asset = numeraire && purchaseOutput.spend.asset = numeraire
    then
    begin match dataOutput.lock, purchaseOutput.lock with
    | ContractLock cHash 3 currentStateData, ContractLock cHash _ _ ->
        do currentState <-- decodeState currentStateData;
        // TODO: avoid modular addition!!
        let newCollateral = currentState.collateral +%^ purchaseOutput.spend.amount in
        let newTokens = purchaseOutput.spend.amount /^ price in   //downwards rounding
        let newState = {
          tokensIssued = currentState.tokensIssued +%^ newTokens; //TODO: modular
          collateral = newCollateral +%^ newCollateral;
          counter = currentState.counter;
        } in        //TODO: return to sender with insufficient collateral
        do newStateData <-- retT @ encodeState newState;
        let newDataOutputLock = ContractLock cHash 3 newStateData in
        let newDataOutput =
          {
            lock=newDataOutputLock;
            spend={asset=numeraire;amount=newCollateral}
          } in
        let buyersOutput =
          {
            lock=lk;
            spend={asset=cHash;amount=newTokens}
          } in
        ret @ Tx
                [| pt1; pt2 |]
                [| newDataOutput; buyersOutput |]
                None
    | _,_ -> autoFailw "Inputs not locked to this contract!"
    end
    else autoFailw "Can't buy with these assets."
  | Exercise [| pntd; pntd' |] lk -> autoFailw "Exercise"


val main: inputMsg -> cost (result transactionSkeleton) 172
let main iM =
  let open ET in
  do comm <-- makeCommand iM;
  createTx (iM.contractHash) comm

val cf: inputMsg -> cost nat 1
let cf _ = ~!172
