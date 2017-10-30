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
open FStar.UInt64

val (+?^): U64.t -> U64.t -> cost (result U64.t) 6
let (+?^) x y = match U64.checked_add x y with
                | Some r -> ET.ret r
                | None -> ET.failw "Overflow Error"


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

type oracleData (n:nat) = {
  underlying: A.t byte n;
  price : U64.t;
  timestamp : U64.t; //Using UInt64 casts outside consensus for now
  nonce : hash
  }

type auditPath (n:nat) = {
  location : U32.t;
  hashes : A.t hash n
  }

unopteq type command =
  | Initialize of outpoint
  | Collateralize of V.t outpoint 2
  | Buy: V.t outpoint 2 -> outputLock -> command
  | Exercise:
           V.t outpoint 3
        -> nUnderlyingAssetBytes : nat
        -> oracleData nUnderlyingAssetBytes
        -> nAuditPathHashes : nat
        -> auditPath nAuditPathHashes
        -> outputLock
        -> command
  | Fail of string

type dcommand (iMsg:inputMsg) = dcmd:command // Command that depends on input message
  { match iMsg with
    | { cmd=0uy; data=(| _, Outpoint pt |) }->
         Initialize? dcmd
    | { cmd=1uy; data=(| _, OutpointVector 2 _ |) } ->
         Collateralize? dcmd
    | { cmd=2uy; data=(| _, Data2 _ _ (OutpointVector 2 _) (OutputLock lk) |) } ->
         Buy? dcmd
    | { cmd=3uy;
        data= (| _, Data4 _ _ _ _
                      _
                      (Data4 _ _ _ _
                        (ByteArray nUnderlyingAssetBytes _)
                        _
                        _
                        _)
                      (Data2 _ _
                        _
                        (HashArray nAuditPathHashes _))
                      _ |) } ->
           Exercise? dcmd
        /\ Exercise?.nUnderlyingAssetBytes dcmd = nUnderlyingAssetBytes
        /\ Exercise?.nAuditPathHashes dcmd = nAuditPathHashes
    | _ -> Fail? dcmd }

val makeCommand : iMsg:inputMsg -> cost (dcommand iMsg) 72
let makeCommand iMsg =
  let open M in
  match iMsg with
  | { cmd=0uy; data=(| _, Outpoint pt |) }->
      ret@Initialize pt
  | { cmd=0uy } ->
      ret@Fail "Bad Initialization data"
  | { cmd=1uy; data=(| _, OutpointVector 2 outpoints |) } ->
      ret@Collateralize outpoints
  | { cmd=1uy } ->
      ret@Fail "Bad Collateralization data"
  | { cmd=2uy; data=(| _, Data2 _ _ (OutpointVector 2 outpoints)
                                    (OutputLock lk) |) } ->
      ret@Buy outpoints lk
  | { cmd=2uy } ->
      ret@Fail "Bad Buy Data"
  | { cmd=3uy; data=
        (| _, Data4 _ _ _ _ (OutpointVector 3 outpoints)
                            (Data4 _ _ _ _
                                (ByteArray n_bytes assetId)
                                (UInt64 price)
                                (UInt64 time)
                                (Hash nonce))
                            (Data2 _ _
                                (UInt32 location)
                                (HashArray n_hashes hashes))
                            (OutputLock lk) |) }
    ->
      ret@Exercise outpoints
                   n_bytes
                   ({underlying=assetId; price=price; timestamp=time; nonce=nonce})
                   n_hashes
                   ({location=location; hashes=hashes})
                   lk
  | { cmd=3uy } ->
      ret@Fail "Bad Exercise Data"
  | _ ->
      ret@Fail "Not implemented"

type state = { tokensIssued : U64.t;
               collateral   : U64.t;
               counter      : U64.t }

val encodeState : state -> cost (inputData 3) 10
let encodeState {
                  tokensIssued=tokensIssued;
                  collateral=collateral;
                  counter=counter
                } =
                  ret @ UInt64Vector 3 V[tokensIssued;collateral;counter]

val decodeState : #n:nat -> inputData n -> cost (result state) 16
let decodeState #n iData =
  let open ET in
  if n <> 3 then autoFailw "Bad data"
  else
    match iData with
    | UInt64Vector 3 V[tk; coll; cter] ->
      ret @ {tokensIssued=tk; collateral=coll; counter=cter}
    | _ -> autoFailw "Bad data"

val findRoot : #n:nat -> inputData n -> #nAuditPathHashes:nat -> (path:auditPath nAuditPathHashes)
    -> cost (result hash) M.(n * 384 + 1064 + nAuditPathHashes)
let findRoot #n iData #nAuditPathHashes {location=location;hashes=hashes} =
  let inputHash = hashData iData in
  let optRoot = OT.bindLift inputHash (function h -> rootFromAuditPath h location hashes) in
  do optRoot' <-- optRoot;
  ret @ O.maybe (E.failw "Can't hash input data") (E.ret) optRoot'

val initializeTx : utxo -> hash -> outpoint -> cost (result transactionSkeleton) 56
let initializeTx utxo cHash outpoint =
    let open ET in
    do numeraire <-- retT numeraire;
    do (pt, oput) <-- tryAddPoint outpoint utxo;
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
      ret @ Tx V[pt] V[dataOutput] None
    else
    autoFailw "Can't initialize with this asset."

val collateralizeTx :
    utxo
    -> hash
    -> outpoint
    -> outpoint
    -> cost (result transactionSkeleton) 130
let collateralizeTx utxo cHash outpoint0 outpoint1 =
    let open ET in
    do numeraire <-- retT numeraire;
    do (pt0, dataOutput)     <-- tryAddPoint outpoint0 utxo;
    do (pt1, newFundsOutput) <-- tryAddPoint outpoint1 utxo;
    if dataOutput.spend.asset <> numeraire || newFundsOutput.spend.asset <> numeraire
    then autoFailw "Can't use these asset types for Collateralize" else
    match dataOutput.lock, newFundsOutput.lock with
    | ContractLock h1 3 currentStateData, ContractLock h2 _ _ ->
        if h1 <> cHash || h2 <> cHash
        then autoFailw "Locked to wrong contract" else begin
        do  currentState <-- decodeState currentStateData;
        do newCollateral <-- currentState.collateral +?^ newFundsOutput.spend.amount;
        let newState = { tokensIssued = currentState.tokensIssued;
                         collateral = newCollateral;
                         counter = currentState.counter +%^ 1UL } in  // We actually prefer modular arithmetic for the counter
        do newStateData <-- retT @ encodeState newState;
        let newDataOutputLock = ContractLock cHash 3 newStateData in
        let newDataOutput = { lock=newDataOutputLock;
                              spend={asset=numeraire;amount=newCollateral} } in
        ret @ Tx V[pt0; pt1]
                 V[newDataOutput]
                 None
        end
    | _,_ -> autoFailw "Inputs not locked to this contract!"

val buyTx :
    utxo
    -> hash
    -> outpoint
    -> outpoint
    -> outputLock
    -> cost (result transactionSkeleton) 147
let buyTx utxo cHash outpoint0 outpoint1 lk =
    let open ET in
    do numeraire <-- retT numeraire;
    do (pt0, dataOutput)     <-- tryAddPoint outpoint0 utxo;
    do (pt1, purchaseOutput) <-- tryAddPoint outpoint0 utxo;
    if dataOutput.spend.asset <> numeraire || purchaseOutput.spend.asset <> numeraire
    then autoFailw "Can't buy with these assets." else
    match dataOutput.lock, purchaseOutput.lock with
    | ContractLock h1 3 currentStateData, ContractLock h2 _ _ ->
        if h1 <> cHash || h2 <> cHash
        then autoFailw "Locked to wrong contract" else begin
        do currentState <-- decodeState currentStateData;
        do newCollateral <--currentState.collateral +?^ purchaseOutput.spend.amount;
        let newTokens = purchaseOutput.spend.amount /^ price in   //downwards rounding
        do tokensIssued <-- currentState.tokensIssued +?^ newTokens;
        let newState = { tokensIssued = tokensIssued;
                         collateral = newCollateral;
                         counter = currentState.counter } in        //TODO: return to sender with insufficient collateral
        do newStateData <-- retT @ encodeState newState;
        let newDataOutputLock = ContractLock cHash 3 newStateData in
        let newDataOutput = { lock=newDataOutputLock;
                              spend={asset=numeraire;amount=newCollateral} } in
        let buyersOutput = { lock=lk; spend={asset=cHash;amount=newTokens} } in
        ret @ Tx V[pt0; pt1]
                 V[newDataOutput; buyersOutput]
                 None
        end
    | _,_ -> autoFailw "Incorrect data or purchase lock"

val restOfEx :
          hash -> pointedOutput -> pointedOutput -> pointedOutput
          -> U64.t
          -> outputLock
          -> cost (result transactionSkeleton) 125
let restOfEx cHash
             (pt1,dataOutput) (pt2,tokenOutput) (pt3,oracleOutput)
             spot
             payoffOutputLock =
    let open ET in
    let open U64 in
    do numeraire <-- retT numeraire;
    if dataOutput.spend.asset <> numeraire || tokenOutput.spend.asset <> cHash
    then autoFailw "Can't exercise with these assets." else
    match dataOutput.lock, tokenOutput.lock with
    | ContractLock h1 3 currentStateData, ContractLock h2 _ _ ->
        if h1 <> cHash || h2 <> cHash
        then autoFailw "Locked to wrong contract" else
        if spot <=^ strike
        then autoFailw "Spot price lower than strike." else begin
        let payoff = spot -^ strike in
        do totalPayoff <-- O.maybe
                            (autoFailw "Overflow")
                            (ET.ret)
                            (tokenOutput.spend.amount *?^ payoff);
        do currentState <-- decodeState currentStateData;
        let newCollateral = currentState.collateral -%^ totalPayoff in
        let newState = { tokensIssued = currentState.tokensIssued -%^ tokenOutput.spend.amount; //TODO: modular
                         collateral = newCollateral;
                         counter = currentState.counter } in
        //TODO: return to sender with insufficient collateral
        do newStateData <-- retT @ encodeState newState;
        let newDataOutputLock = ContractLock cHash 3 newStateData in
        let newDataOutput = { lock=newDataOutputLock;
                              spend={asset=numeraire;amount=newCollateral} } in
        let  payoffOutput = { lock=payoffOutputLock;
                              spend={asset=numeraire;amount=totalPayoff} } in
        ret @ Tx V[pt1; pt2]
                 V[newDataOutput; payoffOutput]
                 None
        end
    | _, _ -> autoFailw "Incorrect data or exercise(token) lock"

val exerciseTx :
          utxo -> hash
          -> outpoint -> outpoint -> outpoint
          -> #nUnderlyingAssetBytes:nat
          -> d:oracleData nUnderlyingAssetBytes
          -> #nAuditPathHashes:nat
          -> (path:auditPath nAuditPathHashes)
          -> outputLock
          -> cost (result transactionSkeleton)
                   M.((nUnderlyingAssetBytes+3) * 384 + 1292 + nAuditPathHashes)

let exerciseTx
          utxo
          cHash
          outpoint0 outpoint1 outpoint2
          #nUnderlyingAssetBytes
          { underlying=underlyingBytes;
            price=price;
            timestamp=timestamp;
            nonce=nonce }
          #nAuditPathHashes
          auditPath
          payoffOutputLock =
    let open ET in
    do (pt0, dataOutput)   <-- tryAddPoint outpoint0 utxo;
    do (pt1, tokenOutput)  <-- tryAddPoint outpoint1 utxo;
    do (pt2, oracleOutput) <-- tryAddPoint outpoint2 utxo;
    let dataToHash =
      (Data4 nUnderlyingAssetBytes 1 1 1
        (ByteArray nUnderlyingAssetBytes underlyingBytes)
        (UInt64 price)
        (UInt64 timestamp)
        (Hash nonce)) in
    // Validate data about the underlying against the Oracle's commitment
    do expectedRoot <-- findRoot dataToHash auditPath;
    match oracleOutput with
    | { lock=(ContractLock oHash 1 (Hash mroot));
        spend={asset=oHash2} } ->
          do oracleHash <-- retT @ oracleHash;
          if oHash <> oracleHash then autoFailw "Incorrect oracle ID" else
          if oHash2 <> oracleHash then autoFailw "Wrong oracle asset type" else
          if not (expectedRoot = mroot) then autoFailw "wrong root" else
          if nUnderlyingAssetBytes <> 32
          then autoFailw "Limitation: symbol must be 32 bytes." else begin
          do underlyingSymbol <-- retT @ underlyingSymbol;
          if underlyingBytes <> underlyingSymbol
          then autoFailw "Trying to use the wrong underlying!" else
          restOfEx cHash
                   (pt0,dataOutput) (pt1,tokenOutput) (pt2,oracleOutput)
                   price
                   payoffOutputLock
          end
    | _ -> autoFailw "Bad oracle output format"

val cf': inputMsg -> nat -> cost nat 58
let cf' iMsg k = let open M in
  ret (k +
  begin match iMsg with
  | { cmd = 0uy ; data = (| _ , Outpoint pt |) } -> 56
  | { cmd = 1uy ; data = (| _ , OutpointVector 2 _ |) } -> 130
  | { cmd = 2uy ; data = (| _ , Data2 _ _ (OutpointVector 2 _) (OutputLock lk) |) } -> 147
  | { cmd = 3uy ;
      data=(| _, Data4 _ _ _ _
                   _
                   (Data4 _ _ _ _ (ByteArray nUnderlyingAssetBytes _) _ _ _)
                   (Data2 _ _ _ (HashArray nAuditPathHashes _))
                   _ |) } ->
                     (nUnderlyingAssetBytes + 3) * 384 + 1292 + nAuditPathHashes
  | _ -> 0
  end)

val cf : i:inputMsg -> res:cost nat 60
let cf i = cf' i 105

val cf_lemma: i:inputMsg -> Lemma (let open M in
   force (cf i) == 105 +
   begin match i with
   | { cmd = 0uy ; data = (| _ , Outpoint pt |) } -> 56
   | { cmd = 1uy ; data = (| _ , OutpointVector 2 _ |) } -> 130
   | { cmd = 2uy ; data = (| _ , Data2 _ _ (OutpointVector 2 _) (OutputLock lk) |) } -> 147
   | { cmd = 3uy ;
       data=(| _, Data4 _ _ _ _
                    _
                    (Data4 _ _ _ _ (ByteArray nUnderlyingAssetBytes _) _ _ _)
                    (Data2 _ _ _ (HashArray nAuditPathHashes _))
                    _ |) } ->
                      (nUnderlyingAssetBytes + 3) * 384 + 1292 + nAuditPathHashes
   | _ -> 0
   end)
let cf_lemma = function
  | { cmd = 0uy ; data = (| _ , Outpoint pt |) } -> ()
  | { cmd = 1uy ; data = (| _ , OutpointVector 2 _ |) } -> ()
  | { cmd = 2uy ; data = (| _ , Data2 _ _ (OutpointVector 2 _) (OutputLock lk) |) } -> ()
  | { cmd = 3uy ;
      data=(| _, Data4 _ _ _ _
                   _
                   (Data4 _ _ _ _ (ByteArray nUnderlyingAssetBytes _) _ _ _)
                   (Data2 _ _ _ (HashArray nAuditPathHashes _))
                   _ |) } -> ()
  | _ -> ()

val main : i:inputMsg -> cost (result transactionSkeleton) (force (cf i))
let main i = let open M in
  cf_lemma i;
  do command <-- makeCommand i ;
  match command with
  | Initialize pointed ->
      initializeTx i.utxo i.contractHash pointed
      <: cost (result transactionSkeleton)
         (match i with
          | { cmd = 0uy ; data = (| _ , Outpoint pt |) } -> 56
          | { cmd = 1uy ; data = (| _ , OutpointVector 2 _ |) } -> 130
          | { cmd = 2uy ; data = (| _ , Data2 _ _ (OutpointVector 2 _) (OutputLock lk) |) } -> 147
          | { cmd = 3uy ;
              data=(| _, Data4 _ _ _ _
                           _
                           (Data4 _ _ _ _ (ByteArray nUnderlyingAssetBytes _) _ _ _)
                           (Data2 _ _ _ (HashArray nAuditPathHashes _))
                           _ |) } ->
                             (nUnderlyingAssetBytes + 3) * 384 + 1292 + nAuditPathHashes
          | _ -> 0)
  | Collateralize V[pointed ; pointed'] ->
      collateralizeTx i.utxo i.contractHash pointed pointed'
  | Buy V[ptd ; ptd'] lk ->
      buyTx i.utxo i.contractHash ptd ptd' lk
  | Exercise V[ptd ; ptd' ; ptd''] nUnderlyingAssetBytes d nAuditPathHashes path lk ->
      exerciseTx i.utxo i.contractHash ptd ptd' ptd'' d path lk
  | Fail msg ->
      ET.failw msg
