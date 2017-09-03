#light "off"
module Zen.Base
open Prims
open FStar.Pervasives

let op_Bar_Greater = (fun ( x  :  'Auu___19_14 ) ( f  :  'Auu___19_14  ->  'Auu___20_15 ) -> (f x))


let op_Less_Bar = (fun ( f  :  'Auu___21_42  ->  'Auu___22_43 ) ( x  :  'Auu___21_42 ) -> (f x))


let op_At = (fun ( f  :  'Auu___23_70  ->  'Auu___24_71 ) ( x  :  'Auu___23_70 ) -> (f x))


let op_Greater_Greater = (fun ( f  :  'Auu___25_104  ->  'Auu___26_105 ) ( g  :  'Auu___26_105  ->  'Auu___27_106 ) ( x  :  'Auu___25_104 ) -> (g (f x)))


let op_Less_Less = (fun ( f  :  'Auu___29_149  ->  'Auu___30_150 ) ( g  :  'Auu___28_148  ->  'Auu___29_149 ) ( x  :  'Auu___28_148 ) -> (f (g x)))


let op_At_Less_Less = (fun ( f  :  'Auu___32_193  ->  'Auu___33_194 ) ( g  :  'Auu___31_192  ->  'Auu___32_193 ) ( x  :  'Auu___31_192 ) -> (f (g x)))


let flip = (fun ( f  :  'Auu___34_237  ->  'Auu___35_238  ->  'Auu___36_239 ) ( x  :  'Auu___35_238 ) ( y  :  'Auu___34_237 ) -> (f y x))




