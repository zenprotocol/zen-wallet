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

  //do cmd <-- ret @ imsg.cmd;
  (*let open ET in
    match n with
    //| (0uy, (|1, Outpoint pt|))  -> failw "One"
    | 1  -> failw "Two"
    | 2  -> failw "Three"
    | _         -> failw "Bad or unknown command"*)
  (*match (cmd, d) with
  (*| (0uy, (| 2, OutpointVector 2 v |)) ->
      map Initialize @ tryAddPoints v imsg.utxo*)
  | (0uy, (| _, OutpointVector 2 v |)) ->
    failw "Not done"
      (*map Collateralize @ tryAddPoints v imsg.utxo*)
  | (1uy, (| _, OutpointVector 2 v |)) ->
    failw "Not done"
      (*map Buy @ tryAddPoints v imsg.utxo*)
  | (2uy, (| _, OutpointVector 2 v |)) ->
    failw "Not done"
      (*map Exercise @ tryAddPoints v imsg.utxo*)
  | _ -> failw "Unknown command"*)

(*
type command : nat -> Type =
  | Initialize : v:pointedOutput -> v:pointedOutput -> command 2
  | Collateralize : v:pointedOutput -> v:pointedOutput -> v:pointedOutput -> command 3
  | Buy : v:pointedOutput -> v: pointedOutput -> v:pointedOutput -> command 3
  | Exercise : v:pointedOutput -> v:pointedOutput -> v:pointedOutput -> command 3

type state = { tokensIssued : U64.t;
               collateral   : U64.t;
               counter      : U64.t }

val parseWitnessMessage : i:inputMsg -> cost (result command l) 0
let parseWitnessMessage i = let open ET in
  match (i.cmd,i.data) with
  | (0uy, OutpointVector 2 [| o1; o2 |]) -> ret @ Initialize o1 o2
  | (0uy, OutpointVector 3 [| o1; o2; o3 |]) -> ret @ Collateralize o1 o2 o3
  | (1uy, OutpointVector 3 [| o1; o2; o3 |]) -> ret @ Buy o1 o2 o3
  | (2uy, OutpointVector 3 [| o1; o2; o3 |]) -> ret @ Exercise o1 o2 o3
  | _       -> failw "Invalid witness command"*)

val main: inputMsg -> cost (result transactionSkeleton) 1
let main iM = ET.failw "Not implemented"

val cf: inputMsg -> cost nat 1
let cf _ = ~!1
