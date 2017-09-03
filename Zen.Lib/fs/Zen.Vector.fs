#light "off"
module Zen.Vector
open Prims
open FStar.Pervasives
type ('Aa, 'dummyV0) vector =
| VNil
| VCons of Prims.nat * 'Aa * ('Aa, Prims.unit) vector


let uu___is_VNil = (fun ( uu____70  :  Prims.nat ) ( projectee  :  ('Aa, Prims.unit) vector ) -> (match (projectee) with
| VNil -> begin
true
end
| uu____82 -> begin
false
end))


let uu___is_VCons = (fun ( uu____123  :  Prims.nat ) ( projectee  :  ('Aa, Prims.unit) vector ) -> (match (projectee) with
| VCons (l, hd, tl) -> begin
true
end
| uu____146 -> begin
false
end))


let __proj__VCons__item__l = (fun ( uu____193  :  Prims.nat ) ( projectee  :  ('Aa, Prims.unit) vector ) -> (match (projectee) with
| VCons (l, hd, tl) -> begin
l
end))


let __proj__VCons__item__hd = (fun ( uu____253  :  Prims.nat ) ( projectee  :  ('Aa, Prims.unit) vector ) -> (match (projectee) with
| VCons (l, hd, tl) -> begin
hd
end))


let __proj__VCons__item__tl = (fun ( uu____317  :  Prims.nat ) ( projectee  :  ('Aa, Prims.unit) vector ) -> (match (projectee) with
| VCons (l, hd, tl) -> begin
tl
end))


type ('Aa, 'Auu____349) t =
('Aa, 'Auu____349) vector


let isEmpty = (fun ( uu___293_393  :  Prims.nat ) ( uu___294_394  :  ('Auu___292_370, Prims.unit) vector ) -> (match (uu___294_394) with
| VNil -> begin
(Zen.Cost.incRet (Prims.parse_int "3") true)
end
| VCons (uu____405, uu____406, uu____407) -> begin
(Zen.Cost.incRet (Prims.parse_int "3") false)
end))


let hd = (fun ( uu___296_456  :  Prims.nat ) ( uu____457  :  ('Auu___295_431, Prims.unit) vector ) -> (match (uu____457) with
| VCons (uu____469, hd, uu____471) -> begin
(Zen.Cost.incRet (Prims.parse_int "2") hd)
end))


let tl = (fun ( uu___298_526  :  Prims.nat ) ( uu____527  :  ('Auu___297_495, Prims.unit) vector ) -> (match (uu____527) with
| VCons (uu____539, uu____540, tl) -> begin
(Zen.Cost.incRet (Prims.parse_int "2") tl)
end))


let rec nth = (fun ( uu___300_605  :  Prims.nat ) ( uu____606  :  ('Auu___299_575, Prims.unit) vector ) ( uu___301_607  :  Prims.nat ) -> (match (uu____606) with
| VCons (uu____621, hd1, tl1) -> begin
(match (uu___301_607) with
| _0_4 when (_0_4 = (Prims.parse_int "0")) -> begin
(Zen.Cost.incRet (Prims.parse_int "4") hd1)
end
| i -> begin
(Zen.Cost.inc ((Prims.parse_int "4") * ((i - (Prims.parse_int "1")) + (Prims.parse_int "1"))) (nth uu____621 tl1 (i - (Prims.parse_int "1"))) (Prims.parse_int "4"))
end)
end))


let rec append = (fun ( uu___303_722  :  Prims.nat ) ( uu___304_723  :  Prims.nat ) ( v1  :  ('Auu___302_677, Prims.unit) vector ) ( v2  :  ('Auu___302_677, Prims.unit) vector ) -> (match (v1) with
| VNil -> begin
(Zen.Cost.incRet (Prims.parse_int "4") v2)
end
| VCons (uu____752, hd1, tl1) -> begin
(Zen.Cost.inc ((Prims.parse_int "4") * (uu____752 + (Prims.parse_int "1"))) (Zen.Cost.liftM ((Prims.parse_int "4") * (uu____752 + (Prims.parse_int "1"))) (fun ( _0_5  :  ('Auu___302_677, Prims.unit) vector ) -> VCons ((uu____752 + uu___304_723), hd1, _0_5)) (append uu____752 uu___304_723 tl1 v2)) (Prims.parse_int "4"))
end))


let op_At_At : Prims.nat  ->  Prims.nat  ->  Prims.unit  ->  (obj, Prims.unit) vector  ->  (obj, Prims.unit) vector  ->  ((obj, Prims.unit) vector, Prims.unit) Zen.Cost.cost = (fun ( uu____865  :  Prims.nat ) ( uu____866  :  Prims.nat ) ( uu____867  :  Prims.unit ) -> (append uu____866 uu____865))


let rec flatten = (fun ( uu___306_939  :  Prims.nat ) ( uu___307_940  :  Prims.nat ) ( uu___308_941  :  (('Auu___305_897, Prims.unit) vector, Prims.unit) vector ) -> (match (uu___308_941) with
| VNil -> begin
(Zen.Cost.autoRet (((Prims.parse_int "4") * (uu___306_939 + (Prims.parse_int "1"))) * (uu___307_940 + (Prims.parse_int "1"))) VNil)
end
| VCons (uu____988, hd1, tl1) -> begin
(Zen.Cost.bind ((Prims.parse_int "4") * (uu___306_939 + (Prims.parse_int "1"))) (((Prims.parse_int "4") * (uu___306_939 + (Prims.parse_int "1"))) * (uu____988 + (Prims.parse_int "1"))) (flatten uu___306_939 uu____988 tl1) (append uu___306_939 (uu___306_939 * uu____988) hd1))
end))


let rec init = (fun ( uu___310_1134  :  Prims.nat ) ( l  :  Prims.nat ) ( f  :  Prims.nat  ->  ('Auu___309_1093, Prims.unit) Zen.Cost.cost ) -> (match (l) with
| _0_6 when (_0_6 = (Prims.parse_int "0")) -> begin
(Zen.Cost.incRet (Prims.parse_int "2") VNil)
end
| uu____1163 -> begin
(Zen.Cost.ap ((uu___310_1134 * (l - (Prims.parse_int "1"))) + (Prims.parse_int "2")) uu___310_1134 (Zen.Cost.liftM uu___310_1134 (fun ( _0_7  :  'Auu___309_1093 ) ( _0_8  :  ('Auu___309_1093, Prims.unit) vector ) -> VCons ((l - (Prims.parse_int "1")), _0_7, _0_8)) (f (Prims.parse_int "0"))) (init uu___310_1134 (l - (Prims.parse_int "1")) (fun ( x  :  Prims.nat ) -> (f (x + (Prims.parse_int "1"))))))
end))


let rec map = (fun ( l  :  Prims.nat ) ( n  :  Prims.nat ) ( f  :  'Auu___311_1265  ->  ('Ab, Prims.unit) Zen.Cost.cost ) ( uu___312_1319  :  ('Auu___311_1265, Prims.unit) vector ) -> (match (uu___312_1319) with
| VNil -> begin
(Zen.Cost.incRet (Prims.parse_int "2") VNil)
end
| VCons (uu____1350, hd1, tl1) -> begin
(Zen.Cost.inc (n + (((uu____1350 * n) + (uu____1350 * (Prims.parse_int "2"))) + (Prims.parse_int "2"))) (Zen.Cost.ap (((uu____1350 * n) + (uu____1350 * (Prims.parse_int "2"))) + (Prims.parse_int "2")) n (Zen.Cost.liftM n (fun ( _0_9  :  'Ab ) ( _0_10  :  ('Ab, Prims.unit) vector ) -> VCons (uu____1350, _0_9, _0_10)) (f hd1)) (map uu____1350 n f tl1)) (Prims.parse_int "2"))
end))


let rec foldl = (fun ( uu___315_1539  :  Prims.nat ) ( uu___316_1540  :  Prims.nat ) ( f  :  'Auu___313_1490  ->  'Auu___314_1491  ->  ('Auu___313_1490, Prims.unit) Zen.Cost.cost ) ( acc  :  'Auu___313_1490 ) ( uu___317_1543  :  ('Auu___314_1491, Prims.unit) vector ) -> (match (uu___317_1543) with
| VNil -> begin
(Zen.Cost.incRet (Prims.parse_int "2") acc)
end
| VCons (uu____1568, hd1, tl1) -> begin
(Zen.Cost.inc (uu___316_1540 + (((uu___316_1540 + (Prims.parse_int "2")) * uu____1568) + (Prims.parse_int "2"))) (Zen.Cost.bind (((uu___316_1540 + (Prims.parse_int "2")) * uu____1568) + (Prims.parse_int "2")) uu___316_1540 (f acc hd1) (fun ( acc'  :  'Auu___313_1490 ) -> (foldl uu____1568 uu___316_1540 f acc' tl1))) (Prims.parse_int "2"))
end))


let countWhere = (fun ( uu___319_1684  :  Prims.nat ) ( uu___320_1685  :  Prims.nat ) ( f  :  'Auu___318_1642  ->  (Prims.bool, Prims.unit) Zen.Cost.cost ) -> (foldl uu___319_1684 uu___320_1685 (fun ( acc  :  Prims.nat ) ( hd1  :  'Auu___318_1642 ) -> (Zen.Cost.liftM uu___320_1685 (fun ( b  :  Prims.bool ) -> (match (b) with
| true -> begin
(acc + (Prims.parse_int "1"))
end
| uu____1727 -> begin
acc
end)) (f hd1))) (Prims.parse_int "0")))


let rec zip = (fun ( uu___323_1800  :  Prims.nat ) ( v1  :  ('Auu___321_1753, Prims.unit) vector ) ( v2  :  ('Auu___322_1754, Prims.unit) vector ) -> (match (v1) with
| VNil -> begin
(Zen.Cost.incRet (Prims.parse_int "3") VNil)
end
| VCons (uu____1837, hd1, tl1) -> begin
(match (v2) with
| VCons (uu____1866, hd2, tl2) -> begin
(Zen.Cost.inc (((Prims.parse_int "3") * uu____1837) + (Prims.parse_int "3")) (Zen.Cost.liftM (((Prims.parse_int "3") * uu____1837) + (Prims.parse_int "3")) (fun ( _0_11  :  (('Auu___321_1753 * 'Auu___322_1754), Prims.unit) vector ) -> VCons (uu____1837, ((hd1), (hd2)), _0_11)) (zip uu____1837 tl1 tl2)) (Prims.parse_int "3"))
end)
end))


let rec sortedBy = (fun ( a92  :  Prims.nat ) ( a93  :  Prims.nat ) ( a94  :  'Auu___324_1956  ->  (Prims.int, Prims.unit) Zen.Cost.cost ) ( a95  :  ('Auu___324_1956, Prims.unit) vector ) -> ((Prims.unsafe_coerce (fun ( uu___325_1992  :  Prims.nat ) ( uu___326_1993  :  Prims.nat ) ( f  :  'Auu___324_1956  ->  (Prims.int, Prims.unit) Zen.Cost.cost ) ( uu___327_1995  :  ('Auu___324_1956, Prims.unit) vector ) -> ())) a92 a93 a94 a95))


let rec uniqueBy = (fun ( a96  :  Prims.nat ) ( a97  :  Prims.nat ) ( a98  :  'Auu___328_2054  ->  (Prims.int, Prims.unit) Zen.Cost.cost ) ( a99  :  ('Auu___328_2054, Prims.unit) vector ) -> ((Prims.unsafe_coerce (fun ( uu___329_2090  :  Prims.nat ) ( uu___330_2091  :  Prims.nat ) ( f  :  'Auu___328_2054  ->  (Prims.int, Prims.unit) Zen.Cost.cost ) ( uu___331_2093  :  ('Auu___328_2054, Prims.unit) vector ) -> ())) a96 a97 a98 a99))


let sortBy = (fun ( a100  :  Prims.nat ) ( a101  :  Prims.nat ) ( a102  :  'Aa  ->  (Prims.int, Prims.unit) Zen.Cost.cost ) ( a103  :  ('Aa, Prims.unit) vector ) -> ((Prims.unsafe_coerce (fun ( l  :  Prims.nat ) ( n  :  Prims.nat ) ( f  :  'Aa  ->  (Prims.int, Prims.unit) Zen.Cost.cost ) ( uu____2207  :  ('Aa, Prims.unit) vector ) -> (failwith "Not yet implemented:sortBy"))) a100 a101 a102 a103))


let mkUnique = (fun ( a104  :  Prims.nat ) ( a105  :  Prims.nat ) ( a106  :  'Aa  ->  (Prims.int, Prims.unit) Zen.Cost.cost ) ( a107  :  ('Aa, Prims.unit) vector ) -> ((Prims.unsafe_coerce (fun ( l  :  Prims.nat ) ( n  :  Prims.nat ) ( f  :  'Aa  ->  (Prims.int, Prims.unit) Zen.Cost.cost ) ( v  :  ('Aa, Prims.unit) vector ) -> (failwith "Not yet implemented:mkUnique"))) a104 a105 a106 a107))


let of_t = (fun ( x  :  'Auu___332_2392 ) -> (Zen.Cost.incRet (Prims.parse_int "2") (VCons ((Prims.parse_int "0"), x, VNil))))


let of_t2 = (fun ( uu____2443  :  ('Auu___333_2423 * 'Auu___333_2423) ) -> (match (uu____2443) with
| (x, y) -> begin
(Zen.Cost.incRet (Prims.parse_int "3") (VCons ((Prims.parse_int "1"), x, VCons ((Prims.parse_int "0"), y, VNil))))
end))


let of_t3 = (fun ( uu____2487  :  ('Auu___334_2465 * 'Auu___334_2465 * 'Auu___334_2465) ) -> (match (uu____2487) with
| (x, y, z) -> begin
(Zen.Cost.incRet (Prims.parse_int "4") (VCons ((Prims.parse_int "2"), x, VCons ((Prims.parse_int "1"), y, VCons ((Prims.parse_int "0"), z, VNil)))))
end))
