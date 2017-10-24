module Zen.Types.Serialized.Realized

open Zen.Cost
open Zen.Types.Extracted

module M = FStar.Mul

val sha3_256: #n:nat
  -> data n
  -> cost (option hash) M.(n*384 + 1050)
