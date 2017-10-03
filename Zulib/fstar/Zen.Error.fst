module Zen.Error
open Zen.Base

val return(#a:Type): a -> result a
let return(#_) = V

val raise: exn -> result 'a
let raise e = E e

val failwith: string -> result 'a
let failwith msg = Err msg

val bind(#a #b:Type): result a -> (a -> result b) -> result b
let bind #_ #_ mx f =
  match mx with
  | V x -> f x
  | E e -> raise e
  | Err msg -> failwith msg

val map(#a #b:Type): (a -> b) -> result a -> result b
let map #_ #_ f mx =
  bind mx (return << f)

val ap(#a #b:Type): result (a -> b) -> result a -> result b
let ap #_ #_ mf mx =
  bind mf (λ f -> map f mx)

val join(#a:Type): result (result a) -> result a
let join(#_) = function
  | V mx -> mx
  | E e -> raise e
  | Err msg -> failwith msg

val bind2(#a #b #c:Type):
  result a -> result b -> (a -> b -> result c) -> result c
let bind2 #_ #_ #_ mx my f =
  mx `bind` (λ x ->
  my `bind` (λ y -> f x y))

val bind3(#a #b #c #d:Type):
  result a -> result b -> result c -> (a -> b -> c -> result d) -> result d
let bind3 #_ #_ #_ #_ mx my mz f =
  mx `bind` (λ x ->
  my `bind` (λ y ->
  mz `bind` (λ z -> f x y z)))

val map2(#a #b #c:Type):
  (a -> b -> c) -> result a -> result b -> result c
let map2 #_ #_ #_ f = map f >> ap

val map3(#a #b #c #d:Type):
  (a -> b -> c -> d) -> result a -> result b -> result c -> result d
let map3 #_ #_ #_ #_ f mx my mz =
  f `map` mx `ap` my `ap` mz

val (>=>) (#a #b #c:Type):
  (a -> result b) -> (b -> result c) -> (a -> result c)
let (>=>) #_ #_ #_ f g =
  λ x -> bind (f x) g

val (<=<) (#a #b #c:Type):
  (b -> result c) -> (a -> result b) -> (a-> result c)
let (<=<) #_ #_ #_ f g = g >=> f
