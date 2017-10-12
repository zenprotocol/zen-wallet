module ZenModule

open Zen.Base
open Zen.Types
open Zen.Wallet
open Zen.Cost
module      V = Zen.Vector
module    U64 = FStar.UInt64
module      E = Zen.Error
module      O = Zen.Option
module     OT = Zen.OptionT
module     ZT = Zen.Types
module     ET = Zen.ErrorT
module Crypto = Zen.Crypto

let numeraire: cost hash 3 = ret @ Zen.Util.hashFromBase64 "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="

let price: U64.t = 100UL
//assume val ownerPubKey: Crypto.key

//assume val msgSig: signature

type command =
  | Collateralize
  | Buy
  | Exercise
  | Close
  | Fail

val parseCommand: opcode -> cost command 21

//due to an Elaborator issue no wrapping 'ret' can be used
let parseCommand  opcode = ret begin if opcode = 0uy then Collateralize
                                else if opcode = 1uy then Buy
                                else if opcode = 2uy then Exercise
                                else if opcode = 3uy then Close else Fail end


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

val getOutpoints: inputMsg -> cost (result (receiving: outpoint * state: option outpoint)) 20
let getOutpoints { data = (| _, data |); utxo = utxo } =
  match data with
  | Data2 _ _ (Outpoint receiving) (Optional _ state) ->
    let state = match state with
      | Some (Outpoint o) -> OT.some o
      | _ -> OT.none in
    do state <-- state;
    ET.ret (receiving, state)
  | _ -> autoInc (ET.failw "bad data format")


val getState: contractHash:hash -> output -> cost (option state) 31
let getState contractHash output =
  begin match output with //TODO: should use point-free style, need to fix Elaborator bug
    | { spend = { asset = asset; amount = collateral };
         lock = ContractLock cHash _ (Data2 _ _ (UInt64 tokensIssued)
                                               (UInt64 counter)) }
         -> do numeraire <-- numeraire;
            if contractHash <> cHash || asset <> numeraire
            then OT.none
            else OT.some @ { tokensIssued = tokensIssued;
                          collateral   = collateral;
                          counter      = counter }
    | _ -> OT.incNone 3
  end


val makeStateOutput: contractHash:hash -> state -> cost output 25
let makeStateOutput contractHash state =
  let data = ret @ Data2 _ _
                   (UInt64 state.tokensIssued)
                   (UInt64 state.counter) in
  do data <-- data;
  do numeraire <-- numeraire;
  ret @ {  spend = { asset = numeraire; amount = state.collateral };
            lock = ContractLock contractHash _ data }


val makeTx:
     contractHash:hash
  -> outpoint
  -> state
  -> option output
  -> cost transactionSkeleton 42
let makeTx contractHash outpoint state output = let open V in
  do stateOutput <-- makeStateOutput contractHash state;

  begin match output with
    | None        -> ret @ Tx [| outpoint |] [| stateOutput |] None
    | Some output -> ret @ Tx [| outpoint |] [| output; stateOutput |] None
  end



val collateralize:
     s:option state
  -> receiving: output
  -> cost (result state) 32
let collateralize s receiving = let open U64 in
  do numeraire <-- numeraire;
  if receiving.spend.asset <> numeraire then
    autoInc (ET.failw "invalid asset type received")
  else
    let receiveAmount = ret receiving.spend.amount in
    do receiveAmount <-- receiveAmount;
    let state = match s with
      | None -> ret @ { counter = 1UL; collateral = receiveAmount; tokensIssued = 0UL }
      | Some s -> ret @ { s with counter    = s.counter +%^ 1UL;            // increment the counter
                           collateral = s.collateral +%^ receiveAmount } in

    do state <-- state;
    ET.ret state


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


val cost_fn: inputMsg -> cost nat 1
let cost_fn _ = ret 182

val callOption: inputMsg -> cost (result transactionSkeleton) 182
let callOption i = let open ET in
   //TODO: Improve pattern match on machine integers.
  //if not (isValid inputMsg) then fail inputMsg else

  do outpoints <-- getOutpoints i;

  let (receivingOutpoint, stateOutpoint) = outpoints in

  do receivingOutput <-- begin match i.utxo receivingOutpoint with
                      | Some output -> ret output
                      | None -> failw "could not resolve receiving outpoint"
                      end;

  let open Zen.Cost in
  let stateOutput = ret (stateOutpoint `O.bind` i.utxo) in

  do state <-- stateOutput `OT.bind` getState i.contractHash;
  do cmd <-- parseCommand i.cmd;

  let state' = match cmd with
     | Collateralize -> collateralize state receivingOutput
     | Buy
     | Exercise
     | Close
     | Fail -> inc (ET.failw "invalid opcode") 32 in

  let open ET in
  do state' <-- state';

  retT (makeTx i.contractHash receivingOutpoint state' None)


val main: cost mainFunction 4
let main = ret (MainFunc (CostFunc cost_fn) callOption)

(*
let pat = e1 in e2
=
(fun pat -> e2) e1

*)

(*
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
