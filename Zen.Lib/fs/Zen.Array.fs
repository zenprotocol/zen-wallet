#light "off"
module Zen.Array
open Prims
open FStar.Pervasives

type ('Aa, 'An) t =
('Aa, 'An) Zen.Array.Realized.array


let op_Array_Access : Prims.nat  ->  Prims.unit  ->  (obj, Prims.unit) Zen.Array.Realized.array  ->  Prims.nat  ->  (obj, Prims.unit) Zen.Cost.cost = (fun ( uu____62  :  Prims.nat ) ( uu____63  :  Prims.unit ) -> (Zen.Array.Realized.at uu____62))


let elem = (fun ( a138  :  Prims.nat ) ( a139  :  Prims.nat ) ( a140  :  ('Auu___570_84, Prims.unit) Zen.Array.Realized.array ) -> ((Prims.unsafe_coerce (fun ( uu___571_114  :  Prims.nat ) ( i  :  Prims.nat ) ( arr  :  ('Auu___570_84, Prims.unit) Zen.Array.Realized.array ) -> ((fun ( uu____131  :  Prims.unit ) -> (Zen.Array.Realized.at uu___571_114)) () (Prims.unsafe_coerce arr) i))) a138 a139 a140))


let tryGet = (fun ( l  :  Prims.nat ) ( i  :  Prims.nat ) ( arr  :  ('Auu___572_155, Prims.unit) Zen.Array.Realized.array ) -> (match ((i < l)) with
| true -> begin
(Zen.OptionT.incLift (Prims.parse_int "2") (Prims.parse_int "2") (Prims.unsafe_coerce ((fun ( uu____212  :  Prims.unit ) -> (Zen.Array.Realized.at l)) () (Prims.unsafe_coerce arr) i)))
end
| uu____221 -> begin
(Zen.OptionT.incNone (Prims.parse_int "4"))
end))
