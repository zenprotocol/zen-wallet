module Zen.Merkle

open Zen.Cost
module A = Zen.Array
module M = FStar.Mul
module C = Zen.Crypto
module U32 = FStar.UInt32


val rootFromAuditPath: #n:nat
  -> C.hash
  -> U32.t
  -> A.t C.hash n
  -> cost (C.hash) n
