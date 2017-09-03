#light "off"
module Zen.OptionT
open Prims
open FStar.Pervasives

let none = (fun ( uu____13  :  Prims.unit ) -> (Zen.Cost.ret FStar.Pervasives.Native.None))


let some = (fun ( x  :  'Auu___360_23 ) -> (Zen.Cost.ret (FStar.Pervasives.Native.Some (x))))


let incNone = (fun ( n  :  Prims.nat ) -> (Zen.Cost.inc (Prims.parse_int "0") (Prims.unsafe_coerce (none ())) n))


let incSome = (fun ( n  :  Prims.nat ) ( x  :  'Auu___362_82 ) -> (Zen.Cost.inc (Prims.parse_int "0") (some x) n))


let lift = (fun ( uu___364_148  :  Prims.nat ) -> (Zen.Cost.liftM uu___364_148 (fun ( _0_2  :  'Auu___363_122 ) -> FStar.Pervasives.Native.Some (_0_2))))


let incLift = (fun ( uu___366_201  :  Prims.nat ) ( n  :  Prims.nat ) ( mx  :  ('Auu___365_171, Prims.unit) Zen.Cost.cost ) -> (Zen.Cost.inc uu___366_201 (lift uu___366_201 mx) n))


let bind = (fun ( uu___369_302  :  Prims.nat ) ( n  :  Prims.nat ) ( mx  :  ('Auu___367_253 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) ( f  :  'Auu___367_253  ->  ('Auu___368_254 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) -> (Zen.Cost.bind n uu___369_302 mx (fun ( uu___370_345  :  'Auu___367_253 FStar.Pervasives.Native.option ) -> (match (uu___370_345) with
| FStar.Pervasives.Native.None -> begin
(incNone n)
end
| FStar.Pervasives.Native.Some (x) -> begin
(f x)
end))))


let bind2 = (fun ( uu___374_467  :  Prims.nat ) ( uu___375_468  :  Prims.nat ) ( uu___376_469  :  Prims.nat ) ( mx  :  ('Auu___371_396 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) ( my  :  ('Auu___372_397 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) ( f  :  'Auu___371_396  ->  'Auu___372_397  ->  ('Auu___373_398 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) -> (bind uu___374_467 (uu___375_468 + uu___376_469) mx (fun ( x  :  'Auu___371_396 ) -> (bind uu___375_468 uu___376_469 my (fun ( y  :  'Auu___372_397 ) -> (f x y))))))


let bind3 = (fun ( uu___381_676  :  Prims.nat ) ( uu___382_677  :  Prims.nat ) ( uu___383_678  :  Prims.nat ) ( uu___384_679  :  Prims.nat ) ( mx  :  ('Auu___377_583 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) ( my  :  ('Auu___378_584 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) ( mz  :  ('Auu___379_585 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) ( f  :  'Auu___377_583  ->  'Auu___378_584  ->  'Auu___379_585  ->  ('Auu___380_586 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) -> (bind uu___381_676 (uu___382_677 + (uu___383_678 + uu___384_679)) mx (fun ( x  :  'Auu___377_583 ) -> (bind uu___382_677 (uu___383_678 + uu___384_679) my (fun ( y  :  'Auu___378_584 ) -> (bind uu___383_678 uu___384_679 mz (fun ( z  :  'Auu___379_585 ) -> (f x y z))))))))


let map = (fun ( uu___387_841  :  Prims.nat ) ( f  :  'Auu___385_805  ->  'Auu___386_806 ) ( mx  :  ('Auu___385_805 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) -> (bind uu___387_841 (Prims.parse_int "0") mx (fun ( x  :  'Auu___385_805 ) -> (some (f x)))))


let ap = (fun ( uu___390_937  :  Prims.nat ) ( uu___391_938  :  Prims.nat ) ( mf  :  (('Auu___388_888  ->  'Auu___389_889) FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) ( mx  :  ('Auu___388_888 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) -> (bind uu___390_937 uu___391_938 mf (fun ( f  :  ('Auu___388_888  ->  'Auu___389_889) ) -> (map uu___391_938 f mx))))


let map2 = (fun ( uu___395_1069  :  Prims.nat ) ( uu___396_1070  :  Prims.nat ) ( f  :  'Auu___392_1014  ->  'Auu___393_1015  ->  'Auu___394_1016 ) ( mx  :  ('Auu___392_1014 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) -> (ap uu___395_1069 uu___396_1070 (map uu___395_1069 f mx)))


let map3 = (fun ( uu___401_1221  :  Prims.nat ) ( uu___402_1222  :  Prims.nat ) ( uu___403_1223  :  Prims.nat ) ( f  :  'Auu___397_1144  ->  'Auu___398_1145  ->  'Auu___399_1146  ->  'Auu___400_1147 ) ( mx  :  ('Auu___397_1144 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) ( my  :  ('Auu___398_1145 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) -> (ap (uu___401_1221 + uu___402_1222) uu___403_1223 (map2 uu___401_1221 uu___402_1222 f mx my)))


let mapBind = (fun ( uu___406_1340  :  Prims.nat ) ( f  :  'Auu___404_1302  ->  'Auu___405_1303 FStar.Pervasives.Native.option ) ( mx  :  ('Auu___404_1302 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) -> (bind uu___406_1340 (Prims.parse_int "0") mx (fun ( x  :  'Auu___404_1302 ) -> (Zen.Cost.ret (f x)))))


let retBind = (fun ( n  :  Prims.nat ) ( x  :  'Auu___407_1388 FStar.Pervasives.Native.option ) -> (bind (Prims.parse_int "0") n (Zen.Cost.ret x)))


let bindLift = (fun ( uu___411_1506  :  Prims.nat ) ( uu___412_1507  :  Prims.nat ) ( mx  :  ('Auu___409_1459 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) ( f  :  'Auu___409_1459  ->  ('Auu___410_1460, Prims.unit) Zen.Cost.cost ) -> (bind uu___412_1507 uu___411_1506 mx (fun ( x  :  'Auu___409_1459 ) -> (lift uu___411_1506 (f x)))))


let bindLift2 = (fun ( uu___416_1650  :  Prims.nat ) ( uu___417_1651  :  Prims.nat ) ( uu___418_1652  :  Prims.nat ) ( mx  :  ('Auu___413_1581 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) ( my  :  ('Auu___414_1582 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) ( f  :  'Auu___413_1581  ->  'Auu___414_1582  ->  ('Auu___415_1583, Prims.unit) Zen.Cost.cost ) -> (bind2 uu___416_1650 uu___417_1651 uu___418_1652 mx my (fun ( x  :  'Auu___413_1581 ) ( y  :  'Auu___414_1582 ) -> (lift uu___418_1652 (f x y)))))


let bindLift3 = (fun ( uu___423_1849  :  Prims.nat ) ( uu___424_1850  :  Prims.nat ) ( uu___425_1851  :  Prims.nat ) ( uu___426_1852  :  Prims.nat ) ( mx  :  ('Auu___419_1758 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) ( my  :  ('Auu___420_1759 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) ( mz  :  ('Auu___421_1760 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) ( f  :  'Auu___419_1758  ->  'Auu___420_1759  ->  'Auu___421_1760  ->  ('Auu___422_1761, Prims.unit) Zen.Cost.cost ) -> (bind3 uu___423_1849 uu___424_1850 uu___425_1851 uu___426_1852 mx my mz (fun ( x  :  'Auu___419_1758 ) ( y  :  'Auu___420_1759 ) ( z  :  'Auu___421_1760 ) -> (lift uu___426_1852 (f x y z)))))


let op_Tilde_Bang_Question = (fun ( uu____1952  :  Prims.unit ) -> some)


let op_Plus_Tilde_Bang = (fun ( uu____1981  :  Prims.unit ) -> incSome)


let op_Tilde_Plus_Bang = (fun ( uu____2010  :  Prims.unit ) -> incSome)


let op_Greater_Greater_Equals : Prims.nat  ->  Prims.unit  ->  Prims.nat  ->  Prims.unit  ->  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost  ->  (obj  ->  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost)  ->  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost = (fun ( uu____2073  :  Prims.nat ) ( uu____2074  :  Prims.unit ) ( uu____2075  :  Prims.nat ) ( uu____2076  :  Prims.unit ) -> (bind uu____2075 uu____2073))


let op_Equals_Less_Less : Prims.nat  ->  Prims.nat  ->  Prims.unit  ->  Prims.unit  ->  (obj  ->  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost)  ->  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost  ->  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost = (fun ( uu____2154  :  Prims.nat ) ( uu____2155  :  Prims.nat ) ( uu____2156  :  Prims.unit ) ( uu____2157  :  Prims.unit ) ( f  :  obj  ->  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) ( x  :  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) -> (bind uu____2154 uu____2155 x f))


let op_Less_Dollar_Greater : Prims.nat  ->  Prims.unit  ->  Prims.unit  ->  (obj  ->  obj)  ->  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost  ->  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost = (fun ( uu____2242  :  Prims.nat ) ( uu____2243  :  Prims.unit ) ( uu____2244  :  Prims.unit ) -> (map uu____2242))


let op_Less_Dollar_Dollar_Greater : Prims.nat  ->  Prims.nat  ->  Prims.unit  ->  Prims.unit  ->  Prims.unit  ->  (obj  ->  obj  ->  obj)  ->  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost  ->  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost  ->  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost = (fun ( uu____2325  :  Prims.nat ) ( uu____2326  :  Prims.nat ) ( uu____2327  :  Prims.unit ) ( uu____2328  :  Prims.unit ) ( uu____2329  :  Prims.unit ) -> (map2 uu____2326 uu____2325))


let op_Less_Dollar_Dollar_Dollar_Greater : Prims.nat  ->  Prims.nat  ->  Prims.nat  ->  Prims.unit  ->  Prims.unit  ->  Prims.unit  ->  Prims.unit  ->  (obj  ->  obj  ->  obj  ->  obj)  ->  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost  ->  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost  ->  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost  ->  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost = (fun ( uu____2444  :  Prims.nat ) ( uu____2445  :  Prims.nat ) ( uu____2446  :  Prims.nat ) ( uu____2447  :  Prims.unit ) ( uu____2448  :  Prims.unit ) ( uu____2449  :  Prims.unit ) ( uu____2450  :  Prims.unit ) -> (map3 uu____2446 uu____2445 uu____2444))


let op_Less_Star_Greater : Prims.nat  ->  Prims.nat  ->  Prims.unit  ->  Prims.unit  ->  ((obj  ->  obj) FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost  ->  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost  ->  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost = (fun ( uu____2531  :  Prims.nat ) ( uu____2532  :  Prims.nat ) ( uu____2533  :  Prims.unit ) ( uu____2534  :  Prims.unit ) -> (ap uu____2532 uu____2531))


let op_Star_Greater : Prims.nat  ->  Prims.unit  ->  Prims.nat  ->  Prims.unit  ->  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost  ->  ((obj  ->  obj) FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost  ->  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost = (fun ( uu____2612  :  Prims.nat ) ( uu____2613  :  Prims.unit ) ( uu____2614  :  Prims.nat ) ( uu____2615  :  Prims.unit ) ( x  :  (obj FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) ( f  :  ((obj  ->  obj) FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost ) -> (ap uu____2612 uu____2614 f x))


let op_Dollar_Greater = (fun ( uu____2707  :  Prims.nat ) ( uu____2708  :  Prims.unit ) ( x  :  (obj, Prims.unit) Zen.Cost.cost ) ( f  :  obj  ->  'Auu____2673 ) -> (Zen.Cost.liftM uu____2707 f x))


let op_Dollar_Dollar_Greater = (fun ( uu___430_2811  :  Prims.nat ) ( uu___431_2812  :  Prims.nat ) ( uu____2813  :  (('Auu___427_2754 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost * ('Auu___428_2755 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost) ) ( f  :  'Auu___427_2754  ->  'Auu___428_2755  ->  'Auu___429_2756 ) -> (match (uu____2813) with
| (mx, my) -> begin
(map2 uu___430_2811 uu___431_2812 f mx my)
end))


let op_Dollar_Dollar_Dollar_Greater = (fun ( uu___436_3010  :  Prims.nat ) ( uu___437_3011  :  Prims.nat ) ( uu___438_3012  :  Prims.nat ) ( uu____3013  :  (('Auu___432_2932 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost * ('Auu___433_2933 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost * ('Auu___434_2934 FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost) ) ( f  :  'Auu___432_2932  ->  'Auu___433_2933  ->  'Auu___434_2934  ->  'Auu___435_2935 ) -> (match (uu____3013) with
| (mx, my, mz) -> begin
(map3 uu___436_3010 uu___437_3011 uu___438_3012 f mx my mz)
end))
