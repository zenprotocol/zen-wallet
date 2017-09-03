#light "off"
module Zen.Tuple
open Prims
open FStar.Pervasives

let curry = (fun ( f  :  ('Auu___45_20 * 'Auu___46_21)  ->  'Auu___47_22 ) ( x  :  'Auu___45_20 ) ( y  :  'Auu___46_21 ) -> (f ((x), (y))))


let uncurry = (fun ( f  :  'Auu___48_67  ->  'Auu___49_68  ->  'Auu___50_69 ) ( uu____87  :  ('Auu___48_67 * 'Auu___49_68) ) -> (match (uu____87) with
| (x, y) -> begin
(f x y)
end))


let swap = (fun ( uu____122  :  ('Auu___51_109 * 'Auu___52_110) ) -> (match (uu____122) with
| (x, y) -> begin
((y), (x))
end))


let curry3 = (fun ( f  :  ('Auu___53_154 * 'Auu___54_155 * 'Auu___55_156)  ->  'Auu___56_157 ) ( x  :  'Auu___53_154 ) ( y  :  'Auu___54_155 ) ( z  :  'Auu___55_156 ) -> (f ((x), (y), (z))))


let uncurry3 = (fun ( f  :  'Auu___57_215  ->  'Auu___58_216  ->  'Auu___59_217  ->  'Auu___60_218 ) ( uu____241  :  ('Auu___57_215 * 'Auu___58_216 * 'Auu___59_217) ) -> (match (uu____241) with
| (x, y, z) -> begin
(f x y z)
end))


let curry4 = (fun ( f  :  ('Auu___61_288 * 'Auu___62_289 * 'Auu___63_290 * 'Auu___64_291)  ->  'Auu___65_292 ) ( w  :  'Auu___61_288 ) ( x  :  'Auu___62_289 ) ( y  :  'Auu___63_290 ) ( z  :  'Auu___64_291 ) -> (f ((w), (x), (y), (z))))


let uncurry4 = (fun ( f  :  'Auu___66_363  ->  'Auu___67_364  ->  'Auu___68_365  ->  'Auu___69_366  ->  'Auu___70_367 ) ( uu____395  :  ('Auu___66_363 * 'Auu___67_364 * 'Auu___68_365 * 'Auu___69_366) ) -> (match (uu____395) with
| (w, x, y, z) -> begin
(f w x y z)
end))


let map = (fun ( f  :  'Auu___71_431  ->  'Auu___72_432 ) ( uu____451  :  ('Auu___71_431 * 'Auu___71_431) ) -> (match (uu____451) with
| (x, y) -> begin
(((f x)), ((f y)))
end))


let map3 = (fun ( f  :  'Auu___73_476  ->  'Auu___74_477 ) ( uu____500  :  ('Auu___73_476 * 'Auu___73_476 * 'Auu___73_476) ) -> (match (uu____500) with
| (x, y, z) -> begin
(((f x)), ((f y)), ((f z)))
end))


let map4 = (fun ( f  :  'Auu___75_529  ->  'Auu___76_530 ) ( uu____557  :  ('Auu___75_529 * 'Auu___75_529 * 'Auu___75_529 * 'Auu___75_529) ) -> (match (uu____557) with
| (w, x, y, z) -> begin
(((f w)), ((f x)), ((f y)), ((f z)))
end))


let op_Bar_Star_Greater = (fun ( x  :  'Auu___77_595 ) ( uu____619  :  (('Auu___77_595  ->  'Auu___78_596) * ('Auu___77_595  ->  'Auu___79_597)) ) -> (match (uu____619) with
| (f, g) -> begin
(((f x)), ((g x)))
end))


let op_Bar_Star_Star_Greater = (fun ( x  :  'Auu___80_668 ) ( uu____700  :  (('Auu___80_668  ->  'Auu___81_669) * ('Auu___80_668  ->  'Auu___82_670) * ('Auu___80_668  ->  'Auu___83_671)) ) -> (match (uu____700) with
| (f, g, h) -> begin
(((f x)), ((g x)), ((h x)))
end))


let op_Bar_Star_Star_Star_Greater = (fun ( x  :  'Auu___84_765 ) ( uu____805  :  (('Auu___84_765  ->  'Auu___85_766) * ('Auu___84_765  ->  'Auu___86_767) * ('Auu___84_765  ->  'Auu___87_768) * ('Auu___84_765  ->  'Auu___88_769)) ) -> (match (uu____805) with
| (f1, f2, f3, f4) -> begin
(((f1 x)), ((f2 x)), ((f3 x)), ((f4 x)))
end))




