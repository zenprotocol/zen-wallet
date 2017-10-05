module ZenModule

open Zen.Base
open Zen.Types
open Zen.Wallet
open Zen.Cost

module      V = Zen.Vector
module    U64 = FStar.UInt64
module Crypto = Zen.Crypto

let numeraire: hash = Zen.Util.hashFromBase64 "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="

let price: U64.t = 100UL
//assume val ownerPubKey: Crypto.key

//assume val msgSig: signature

type command =
  | Collateralize
  | Buy
  | Exercise
  | Close
  | Fail

val parseCommand: opcode -> command
let parseCommand  opcode = if opcode = 0uy then Collateralize
                      else if opcode = 1uy then Buy
                      else if opcode = 2uy then Exercise
                      else if opcode = 3uy then Close else Fail

type state = { tokensIssued : U64.t;
               collateral   : U64.t;
               counter      : U64.t }


(*assume val isAuthenticated: inputMsg -> bool

val isAuthenticated : inputMsg -> bool
let isAuthenticated inputMsg =
  if inputMsg.cmd = 0uy || // collateralize
     inputMsg.cmd = 3uy    // close
  then Crypto.verifyInputMsg inputMsg ownerPubKey
  else true

val correctDataForm : inputMsg -> bool
let correctDataForm inputMsg =
  if inputMsg.cmd = 1uy then
    match inputMsg.data with
    | (| _, Data2 _ _ (Hash senderPKHash) (Hash returnPKHash) |) -> true
    | _ -> false
  else false

val isValid: inputMsg -> bool
let isValid inputMsg = isAuthenticated inputMsg &&
                       correctDataForm inputMsg*)



val getOutputs: inputMsg ->
  receiving : option output *
  state     : option output
let getOutputs { data = (| _, data |); utxo = utxo } = match data with
  | Data2 _ _ (Outpoint receiving) (Optional _ state)
    -> let state = match state with
                    | Some (Outpoint o) -> utxo o
                    | _ -> None in
       utxo receiving, state
  | _ -> None, None

val getState: contractHash:hash -> output -> option state
let getState contractHash = function
  | { spend = { asset = asset; amount = collateral };
      lock = ContractLock cHash _ (Data2 _ _ (UInt64 tokensIssued)
                                             (UInt64 counter)) }
     -> if contractHash <> cHash || asset <> numeraire
        then None
        else Some @ { tokensIssued = tokensIssued;
                      collateral   = collateral;
                      counter      = counter }
  | _ -> None

val makeStateOutput: contractHash:hash -> state -> output
let makeStateOutput contractHash state =
  let data = Data2 _ _
                   (UInt64 state.tokensIssued)
                   (UInt64 state.counter) in
  {  spend = { asset = numeraire; amount = state.collateral };
     lock = ContractLock contractHash _ data }


val makeTx:
     contractHash:hash
  -> #l:nat -> V.t outpoint l
  -> state
  -> option output
  -> transactionSkeleton
let makeTx contractHash #_ outpoints state output =
  let stateOutput = V.VCons (makeStateOutput contractHash state) V.VNil in
  match output with
    | None        -> Tx outpoints stateOutput None
    | Some output -> Tx outpoints (V.VCons output stateOutput) None

val collateralize:
     option state
  -> receiving: output
  -> option state * option output
let collateralize s receiving = let open U64 in
  if receiving.spend.asset <> numeraire then None, None
  else
    let receiveAmount = receiving.spend.amount in
    let state = match s with
      | None -> { counter = 1UL; collateral = receiveAmount; tokensIssued = 0UL }
      | Some s -> { s with counter    = s.counter +%^ 1UL;            // increment the counter
                           collateral = s.collateral +%^ receiveAmount } in

    Some state, None

//assume val buy: inputMsg -> state -> option state
(*let buy inputMsg state = let open U64 in
  match inputMsg.data with
  | (| _, Data2 _ _ (Hash senderPKHash) (Hash returnPKHash) |) ->
    let receiveAmount   = getFundsFrom state.wallet numeraire senderPKHash in
    let tokensPurchased = receiveAmount /^ price in

    let output = { lock=PKLock returnPKHash;
                   spend= { asset =inputMsg.contractHash;
                            amount=tokensPurchased } } in

    Some @ { state with wallet = state.wallet `addOutput` unsafe_coerce output;
                        collateral   = state.collateral   +%^ receiveAmount;
                        tokensIssued = state.tokensIssued +%^ tokensPurchased }
  | _ -> None*)

//assume val exercise     : state -> state

//assume val close: inputMsg -> state -> option state
(*let close inputMsg state =
  match inputMsg.data with
  | (| _, Hash returnPKHash |) ->
    let output = { lock=PKLock returnPKHash;
                   spend= { asset =numeraire;
                            amount=state.wallet `getFunds` numeraire } } in
    Some @ { state with wallet = state.wallet `addOutput` output;
                        collateral = 0UL }
  | _ -> None*)

val main: inputMsg -> cost (result transactionSkeleton) 0
let main i = let open Zen.Option in
   //TODO: Improve pattern match on machine integers.
  //if not (isValid inputMsg) then fail inputMsg else

  let receiving, state = getOutputs i in
  let state = state `bind` getState i.contractHash in

  ret (match receiving with
    | None -> Err "could not resolve 'receiving' outpoint"
    | Some receiving ->
      let state', output = match parseCommand i.cmd with
        | Collateralize -> collateralize state receiving
        | Buy
        | Exercise
        | Close
        | Fail -> None, None in
      match state' with
        | None        -> Err "test"
        | Some state' -> V (makeTx i.contractHash V.VNil state' output))
