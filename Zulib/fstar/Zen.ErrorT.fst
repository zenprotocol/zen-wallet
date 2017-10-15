module Zen.ErrorT

open Zen.Cost
open Zen.Base

module E = Zen.Error

val fail: exn -> cost (result 'a) 0
let fail e = ret (E.fail e)

val failw: string -> cost (result 'a) 0
let failw msg = ret (E.failw msg)

val ret(#a:Type): a -> cost (result a) 0
let ret(#_) x = ret (V x)

val bind(#a #b:Type)(#m #n:nat):
  cost (result a) m
  -> (a -> cost (result b) n)
  -> cost (result b) (m+n)
let bind #_ #_ #_ #n mx f =
  mx >>= (function
  | V x -> f x
  | E e -> fail e `inc` n
  | Err msg -> failw msg `inc` n)

val map(#a #b:Type)(#n:nat): (a -> b) -> cost (result a) n
  -> cost (result b) n
let map #_ #_ #_ f mx =
  bind mx (ret << f)

val ap(#a #b:Type)(#m #n:nat): cost (result (a->b)) m -> cost (result a) n
  -> cost (result b) (n+m)
let ap #_ #_ #_ #_ mf mx =
  do f <-- mf;
  f `map` mx
