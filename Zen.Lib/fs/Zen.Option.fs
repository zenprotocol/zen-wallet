#light "off"
module Zen.Option
open Prims
open FStar.Pervasives

let maybe = (fun ( y  :  'Auu___53_19 ) ( f  :  'Auu___52_18  ->  'Auu___53_19 ) ( uu___54_36  :  'Auu___52_18 FStar.Pervasives.Native.option ) -> (match (uu___54_36) with
| FStar.Pervasives.Native.None -> begin
y
end
| FStar.Pervasives.Native.Some (x) -> begin
(f x)
end))


let fromOption = (fun ( x  :  'Auu___55_52 ) ( uu___56_62  :  'Auu___55_52 FStar.Pervasives.Native.option ) -> (match (uu___56_62) with
| FStar.Pervasives.Native.None -> begin
x
end
| FStar.Pervasives.Native.Some (y) -> begin
y
end))


let bind = (fun ( mx  :  'Auu___57_80 FStar.Pervasives.Native.option ) ( f  :  'Auu___57_80  ->  'Auu___58_81 FStar.Pervasives.Native.option ) -> (match (mx) with
| FStar.Pervasives.Native.Some (x) -> begin
(f x)
end
| FStar.Pervasives.Native.None -> begin
FStar.Pervasives.Native.None
end))


let map = (fun ( f  :  'Auu___59_120  ->  'Auu___60_121 ) ( mx  :  'Auu___59_120 FStar.Pervasives.Native.option ) -> (bind mx (fun ( x  :  'Auu___59_120 ) -> FStar.Pervasives.Native.Some ((f x)))))


let op_Greater_Greater_Question = (fun ( f  :  'Auu___61_163  ->  'Auu___62_164 FStar.Pervasives.Native.option ) ( g  :  'Auu___62_164  ->  'Auu___63_165 FStar.Pervasives.Native.option ) ( x  :  'Auu___61_163 ) -> (bind (f x) g))


let ap = (fun ( mf  :  ('Auu___64_214  ->  'Auu___65_215) FStar.Pervasives.Native.option ) ( mx  :  'Auu___64_214 FStar.Pervasives.Native.option ) -> (bind mf (fun ( f  :  ('Auu___64_214  ->  'Auu___65_215) ) -> (map f mx))))


let join = (fun ( mmx  :  'Auu___66_252 FStar.Pervasives.Native.option FStar.Pervasives.Native.option ) -> (bind mmx (fun ( x  :  'Auu___66_252 FStar.Pervasives.Native.option ) -> x)))




