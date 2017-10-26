module Zen.Merkle

open Zen.Cost
open Zen.Types.Extracted

module  A = Zen.Array
module  M = FStar.Mul
module  C = Zen.Crypto

val getDataHash:
     #n:nat
  -> data n
  -> Cost.t (C.hash) n

val getRootFromAuditPath:
     #n:nat
  -> C.hash
  -> nat
  -> A.t C.hash n
  -> cost (C.hash) n
