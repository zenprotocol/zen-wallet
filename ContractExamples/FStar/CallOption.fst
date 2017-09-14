module CallOption

open Zen.Base
open Zen.Types
open Zen.Wallet

module Crypto = Zen.Crypto

module U64 = FStar.UInt64

assume val numeraire: hash
let price: U64.t = 100UL
assume val ownerPubKey: Crypto.key

assume val msgSig: signature


type state = { tokensIssued : U64.t;
               collateral   : U64.t;
               counter      : U64.t;
               wallet       : wallet }

assume val isAuthenticated: inputMsg -> bool

//val isAuthenticated : inputMsg -> bool
//let isAuthenticated inputMsg =
  //if inputMsg.cmd =

val correctDataForm : inputMsg -> bool
let correctDataForm inputMsg =
  if inputMsg.cmd = 1uy then
    match inputMsg.data with
    | (| _, Data2 _ _ (Hash senderPKHash) (Hash returnPKHash) |) -> true
    | _ -> false
  else false

val isValid: inputMsg -> bool
let isValid inputMsg = isAuthenticated inputMsg &&
                       correctDataForm inputMsg

assume val getState: inputMsg:inputMsg{isValid inputMsg} * wallet -> state

val collateralize: state -> state
let collateralize s = let open U64 in
  let recieveAmount: U64.t = admit() in
  { s with counter   =   s.counter +%^ 1UL;            // increment the counter
           collateral=s.collateral +%^ recieveAmount }


val buy: inputMsg -> state -> option state
let buy inputMsg state = let open U64 in
  match inputMsg.data with
  | (| _, Data2 _ _ (Hash senderPKHash) (Hash returnPKHash) |) ->
    let recieveAmount   = getFundsFrom state.wallet numeraire senderPKHash in
    let tokensPurchased = recieveAmount /^ price in

    let output = { lock=PKLock returnPKHash;
                   spend= { asset =inputMsg.contractHash;
                            amount=tokensPurchased } } in

    Some @ { state with wallet = state.wallet `addOutput` unsafe_coerce output;
                        collateral   = state.collateral   +%^ recieveAmount;
                        tokensIssued = state.tokensIssued +%^ tokensPurchased }
  | _ -> None

assume val exercise     : state -> state

val close: inputMsg -> state -> option state
let close inputMsg state =
  match inputMsg.data with
  | (| _, Hash returnPKHash |) ->
    let output = { lock=PKLock returnPKHash;
                   spend= { asset =numeraire;
                            amount=state.wallet `getFunds` numeraire } } in
    Some @ { state with wallet = state.wallet `addOutput` output;
                        collateral = 0UL }
  | _ -> None

val fail: 'a -> option state
let fail _ = None

val main: inputMsg * wallet -> option state
let main (inputMsg, wallet) = //TODO: Improve pattern match on machine integers.
  if not (isValid inputMsg) then fail inputMsg else
  let cmd = inputMsg.cmd in
  let action = if cmd = 0uy then collateralize >> Some
          else if cmd = 1uy then buy inputMsg
          else if cmd = 2uy then exercise >> Some
          else if cmd = 3uy then close inputMsg
                            else fail in
  let state = getState (inputMsg, wallet) in
  action state
