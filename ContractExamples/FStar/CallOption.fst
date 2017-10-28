module ZenModule

module V = Zen.Vector
module A = Zen.Array
module O = Zen.Option
module OT = Zen.OptionT
module E = Zen.Error
module ET = Zen.ErrorT
module U64 = FStar.UInt64
module Crypto = Zen.Crypto
module M = FStar.Mul
module U32 = FStar.UInt32

open Zen.Base
open Zen.Types
open Zen.Cost
open Zen.Merkle
open Zen.Types.Serialized.Realized


let numeraire: cost hash 3 = ret @ Zen.Util.hashFromBase64 "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="

// "price" is the premium, in kalapas
let price: U64.t = 100UL

// strike in u64 is real strike * 1000, rounded down
let strike: U64.t = 1000000UL

let oracleHash: cost hash 3 = ret @ Zen.Util.hashFromBase64 "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="

// No string -> byte arrays, yet. So 32 byte arrays to represent
// the underlying, i.e. stuff like "AAPL", "MSFT", etc. To use:
// take string, cast to byte array, pad to 32 bytes, base64 encode,
// pass in here.
// The example decodes to "AAPL", followed by 28 zero bytes.
let underlyingSymbol = ret @ Zen.Util.hashFromBase64
"QUFQTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="

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

type oracleData = {
                    underlying: (n:nat & A.t byte n);
                    price : U64.t;
                    timestamp : U64.t; //Using UInt64 casts outside consensus for now
                    nonce : hash
                  }

type auditPath =  {
                    location : U32.t;
                    hashes : (n:nat & A.t hash n)
                  }

unopteq type command =
  | Initialize of pointedOutput
  | Collateralize of V.t pointedOutput 2
  | Buy: (V.t pointedOutput 2) -> outputLock -> command
  | Exercise:
           (V.t pointedOutput 3)
        -> oracleData
        -> auditPath
        -> outputLock
        -> command

val makeCommand : inputMsg -> cost (result command) 92
let makeCommand {cmd=cmd; data=iData; utxo=utxos} =
  let open M in
  let open ET in
  match cmd with
  | 0uy -> begin match iData with
           | (| 1, Outpoint pt |) ->
             do pointed <-- tryAddPoint pt utxos;
             incRet 14 (Initialize pointed)
           | (| 1, _ |) -> incFailw 21 "Bad Initialization data"
           | (| 2, OutpointVector _ [| outpoint0; outpoint1 |] |) ->
             do pointedOutput0 <-- tryAddPoint outpoint0 utxos;
             do pointedOutput1 <-- tryAddPoint outpoint1 utxos;
             incRet 7 (Collateralize [| pointedOutput0; pointedOutput1 |])
           | (| 2, _ |) -> incFailw 21 "Bad Collateralization data"
           | _ -> incFailw 21 "Bad Initialization/Collateralization data"
           end
  | 1uy -> begin match iData with
           | (|3, Data2 _ _ (OutpointVector _ [| outpoint0; outpoint1 |] )
                            (OutputLock lk) |) ->
             do pointedOutput0 <-- tryAddPoint outpoint0 utxos;
             do pointedOutput1 <-- tryAddPoint outpoint1 utxos;
             incRet 7 (Buy [| pointedOutput0; pointedOutput1 |] lk)
           | _ -> incFailw 21 "Bad Buy Data"
           end
  | 2uy -> begin match iData with
           | (| _,
                Data4 _ _ _ _
                  (OutpointVector _ [| outpoint0; outpoint1; outpoint2 |])
                  (Data4 _ _ _ _
                    (ByteArray n_bytes assetId)
                    (UInt64 price)
                    (UInt64 time)
                    (Hash nonce))
                  (Data2 _ _
                    (UInt32 location)
                    (HashArray n_hashes hashes))
                  (OutputLock lk)
              |) ->
                let oracleData = { underlying = (| n_bytes, assetId |);
                                   price=price;
                                   timestamp=time;
                                   nonce=nonce } in
                let auditPath = { location=location;
                                  hashes = (| n_hashes, hashes |) } in
                do pointedOutput0 <-- tryAddPoint outpoint0 utxos;
                do pointedOutput1 <-- tryAddPoint outpoint1 utxos;
                do pointedOutput2 <-- tryAddPoint outpoint2 utxos;
                ret@Exercise [| pointedOutput0; pointedOutput1; pointedOutput2 |]
                             oracleData
                             auditPath
                             lk
           | _ -> incFailw 21 "Bad Exercise Data"
           end
  | _ ->  incFailw 21 "Not implemented"


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

val findRoot : #n:nat -> inputData n -> (path:auditPath)
    -> cost (result hash) M.((n * 384 + 1065 + dfst path.hashes))
let findRoot #n iData {location=location;hashes=hashes} =
  let inputHash = sha3_256 iData in
  let optRoot = OT.bindLift inputHash (function h -> rootFromAuditPath h location (dsnd hashes)) in
  do optRoot' <-- optRoot;
  ret @ O.maybe (E.failw "Can't hash input data") (E.ret) optRoot'

val initializeTx : hash -> pointedOutput -> cost (result transactionSkeleton) 45
let initializeTx cHash (pt, oput) =
      (do numeraire <-- numeraire;
      let open ET in
      let open U64 in
      if oput.spend.asset = numeraire
      then      // Initialize with data output
        let initialState : state =
          {
            tokensIssued=0UL;
            collateral=oput.spend.amount;
            counter=0UL;
          } in
        do initialStateData <-- retT @ encodeState initialState;
        let dataOutputLock = ContractLock cHash 3 initialStateData in
        let dataOutput = {lock=dataOutputLock;spend=oput.spend} in
        ret @ Tx [| pt |] [| dataOutput |] None
      else
      autoFailw "Can't initialize with this asset.")


val collateralizeTx :
    hash
    -> pointedOutput
    -> pointedOutput
    -> cost (result transactionSkeleton) 102
let collateralizeTx cHash (pt1,dataOutput) (pt2,newFundsOutput) =
      do numeraire <-- numeraire;
      let open ET in
      let open U64 in
      if dataOutput.spend.asset = numeraire && newFundsOutput.spend.asset = numeraire
      then
        begin match dataOutput.lock, newFundsOutput.lock with
        | ContractLock h1 3 currentStateData, ContractLock h2 _ _ ->
            if h1 <> cHash || h2 <> cHash
            then autoFailw "Locked to wrong contract"
            else begin
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
            end
        | _,_ -> autoFailw "Inputs not locked to this contract!"
        end
      else autoFailw "Can't use these asset types for Collateralize"


val buyTx :
    hash
    -> pointedOutput
    -> pointedOutput
    -> outputLock
    -> cost (result transactionSkeleton) 112
let buyTx cHash (pt1, dataOutput) (pt2, purchaseOutput) lk =
      do numeraire <-- numeraire;
      let open ET in
      let open U64 in
      if dataOutput.spend.asset = numeraire && purchaseOutput.spend.asset = numeraire
      then
      begin match dataOutput.lock, purchaseOutput.lock with
      | ContractLock h1 3 currentStateData, ContractLock h2 _ _ ->
          if h1 <> cHash || h2 <> cHash
          then autoFailw "Locked to wrong contract"
          else begin
          do currentState <-- decodeState currentStateData;
          // TODO: avoid modular addition!!
          let newCollateral = currentState.collateral +%^ purchaseOutput.spend.amount in
          let newTokens = purchaseOutput.spend.amount /^ price in   //downwards rounding
          let newState = {
            tokensIssued = currentState.tokensIssued +%^ newTokens; //TODO: modular
            collateral = newCollateral;
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
          end
      | _,_ -> autoFailw "Incorrect data or purchase lock"
      end
      else autoFailw "Can't buy with these assets."


type exTestCost (n:nat) (path:auditPath) =
  cost (result transactionSkeleton)
  (match path with
    | { hashes = (| m , _ |) } ->
      let open M in (n + 3) * 384 + 1073 + m)

val restOfEx :
          hash -> pointedOutput -> pointedOutput -> pointedOutput
          -> U64.t
          -> outputLock
          -> cost (result transactionSkeleton) 124
let restOfEx
        cHash
        (pt1,dataOutput) (pt2,tokenOutput) (pt3,oracleOutput)
        spot
        payoffOutputLock =
        begin
          do numeraire <-- numeraire;
          let open ET in
          let open U64 in
          if dataOutput.spend.asset <> numeraire || tokenOutput.spend.asset <> cHash
          then autoFailw "Can't exercise with these assets."
          else
          match dataOutput.lock, tokenOutput.lock with
          | ContractLock h1 3 currentStateData, ContractLock h2 _ _ ->
              if h1 <> cHash || h2 <> cHash
              then autoFailw "Locked to wrong contract"
              else if spot <=^ strike then autoFailw "Spot price lower than strike."
              else
              let payoff = spot -^ strike in
              begin
              do totalPayoff <-- O.maybe
                                  (autoFailw "Overflow")
                                  (ET.ret)
                                  (tokenOutput.spend.amount *?^ payoff);
                do currentState <-- decodeState currentStateData;
                let newCollateral = currentState.collateral -%^ totalPayoff in
                let newState = {
                  tokensIssued = currentState.tokensIssued -%^ tokenOutput.spend.amount; //TODO: modular
                  collateral = newCollateral;
                  counter = currentState.counter;
                } in
                //TODO: return to sender with insufficient collateral
                do newStateData <-- retT @ encodeState newState;
                let newDataOutputLock = ContractLock cHash 3 newStateData in
                let newDataOutput =
                  {
                    lock=newDataOutputLock;
                    spend={asset=numeraire;amount=newCollateral}
                  } in
                let payoffOutput =
                  {
                    lock=payoffOutputLock;
                    spend={asset=numeraire;amount=totalPayoff}
                  } in
                ret @ Tx
                        [| pt1; pt2 |]
                        [| newDataOutput; payoffOutput |]
                        None
              end
          | _, _ -> autoFailw "Incorrect data or exercise(token) lock"
         end

val exerciseTx :
          hash -> pointedOutput -> pointedOutput -> pointedOutput
          -> (d:oracleData)
          -> (path:auditPath)
          -> outputLock
          -> cost (result transactionSkeleton)
                  (
                    match d, path with
                    | {underlying=(|n, _|)}, {hashes=(|m, _|)} -> M.((n+3) * 384 + 1262 + m)
                  )

let exerciseTx
          cHash
          (pt1,dataOutput) (pt2,tokenOutput) (pt3,oracleOutput)
          {
                underlying = (| n, underlyingBytes |);
                price=price;
                timestamp=timestamp;
                nonce=nonce
          }
          auditPath
          payoffOutputLock =
          begin
          let open ET in
          let dataToHash  =
            (Data4 n 1 1 1
              (ByteArray n underlyingBytes)
              (UInt64 price)
              (UInt64 timestamp)
              (Hash nonce)) in
          // Validate data about the underlying against the Oracle's commitment
          do expectedRoot <-- (
            findRoot
                                dataToHash
                                auditPath
                              ) ;
          match oracleOutput with
          | {
              lock=(ContractLock oHash 1 (Hash mroot));
              spend={asset=oHash2}
            } ->
            do oracleHash <-- retT @ oracleHash;
            if oHash <> oracleHash then autoFailw "Incorrect oracle ID"
            else if oHash2 <> oracleHash then autoFailw "Wrong oracle asset type"
            else
            if not (expectedRoot = mroot) then
              autoFailw "wrong root"
            else if n <> 32 then autoFailw "Limitation: symbol must be 32 bytes."
            else begin
            do underlyingSymbol <-- retT @ underlyingSymbol;
            if underlyingBytes <> underlyingSymbol then
              autoFailw "Trying to use the wrong underlying!"
            else
              restOfEx
                cHash
                (pt1,dataOutput) (pt2,tokenOutput) (pt3,oracleOutput)
                price
                payoffOutputLock
            end
          | _ -> autoFailw "Bad oracle output format"
          end

type createTxInnerCost (cmdRes:result command) =
  cost (result transactionSkeleton)
          (match cmdRes with
            | V c -> (match c with
              | Initialize _ -> 45
              | Collateralize _ -> 102
              | Buy _ _ -> 112
              | Exercise _ { underlying = (| n , _ |) } { hashes = (| m , _ |) } _ ->
                  M.(((n+3) * 384 + 1262 + m)))
            | _ -> 0
          )

type createTxCost (cmdRes:result command) =
  cost (result transactionSkeleton)
        ((match cmdRes with
          | V c -> (match c with
            | Initialize _ -> 45
            | Collateralize _ -> 102
            | Buy _ _ -> 112
            | Exercise _ { underlying = (| n , _ |) } { hashes = (| m , _ |) } _ ->
                M.(((n+3) * 384 + 1262 + m)))
          | _ -> 0
        ) + 26)

type createTxPlusK (cmdRes:result command) (k:nat) =
    cost  (result transactionSkeleton)
          ((match cmdRes with
            | V c -> (match c with
              | Initialize _ -> 45
              | Collateralize _ -> 102
              | Buy _ _ -> 112
              | Exercise _ { underlying = (| n , _ |) } { hashes = (| m , _ |) } _ ->
                  M.(((n+3) * 384 + 1262 + m)))
            | _ -> 0
          ) + k)

val createTx :
      hash
      -> cmdRes:result command
      -> createTxPlusK cmdRes 26
let createTx cHash cmdRes =
match cmdRes with
| V c -> (match c with
  | Initialize pointed ->
      initializeTx cHash pointed <: createTxPlusK cmdRes 0
  | Collateralize [| pointed; pointed'|] ->
      collateralizeTx cHash pointed pointed' <: createTxPlusK cmdRes 0
  | Buy [| ptd; ptd' |] lk ->
      buyTx cHash ptd ptd' lk <: createTxPlusK cmdRes 0
  | Exercise [| ptd; ptd'; ptd'' |] d path lk ->
      exerciseTx cHash ptd ptd' ptd'' d path lk <: createTxPlusK cmdRes 0
  )
| _ -> ET.autoFailw "Bad command" <: createTxPlusK cmdRes 0

val main' : (iM:inputMsg) -> createTxPlusK (force (makeCommand iM)) 123
let main' iM =
  do cmdRes <-- makeCommand iM;
  do tx <-- createTx iM.contractHash cmdRes;
  ret tx

assume val main: inputMsg -> cost (result transactionSkeleton) 241

val cf: inputMsg -> cost nat 1
let cf _ = ~!241
