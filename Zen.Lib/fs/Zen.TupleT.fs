#light "off"
module Zen.TupleT
open Prims
open FStar.Pervasives

let join = (fun ( uu___400_66  :  Prims.nat ) ( uu___401_67  :  Prims.nat ) ( uu____68  :  (('Auu___398_21, Prims.unit) Zen.Cost.cost * ('Auu___399_22, Prims.unit) Zen.Cost.cost) ) -> (match (uu____68) with
| (mx, my) -> begin
(Zen.Cost.ap uu___400_66 uu___401_67 (Zen.Cost.liftM uu___401_67 (fun ( _0_2  :  'Auu___398_21 ) ( _0_3  :  'Auu___399_22 ) -> ((_0_2), (_0_3))) mx) my)
end))


let join3 = (fun ( uu___405_241  :  Prims.nat ) ( uu___406_242  :  Prims.nat ) ( uu___407_243  :  Prims.nat ) ( uu____244  :  (('Auu___402_178, Prims.unit) Zen.Cost.cost * ('Auu___403_179, Prims.unit) Zen.Cost.cost * ('Auu___404_180, Prims.unit) Zen.Cost.cost) ) -> (match (uu____244) with
| (mx, my, mz) -> begin
(Zen.Cost.ap uu___407_243 (uu___405_241 + uu___406_242) (Zen.Cost.ap uu___406_242 uu___405_241 (Zen.Cost.liftM uu___405_241 (fun ( _0_4  :  'Auu___402_178 ) ( _0_5  :  'Auu___403_179 ) ( _0_6  :  'Auu___404_180 ) -> ((_0_4), (_0_5), (_0_6))) mx) my) mz)
end))


let join4 = (fun ( uu___412_502  :  Prims.nat ) ( uu___413_503  :  Prims.nat ) ( uu___414_504  :  Prims.nat ) ( uu___415_505  :  Prims.nat ) ( uu____506  :  (('Auu___408_421, Prims.unit) Zen.Cost.cost * ('Auu___409_422, Prims.unit) Zen.Cost.cost * ('Auu___410_423, Prims.unit) Zen.Cost.cost * ('Auu___411_424, Prims.unit) Zen.Cost.cost) ) -> (match (uu____506) with
| (mw, mx, my, mz) -> begin
(Zen.Cost.ap uu___415_505 ((uu___412_502 + uu___413_503) + uu___414_504) (Zen.Cost.ap uu___414_504 (uu___412_502 + uu___413_503) (Zen.Cost.ap uu___413_503 uu___412_502 (Zen.Cost.liftM uu___412_502 (fun ( _0_7  :  'Auu___408_421 ) ( _0_8  :  'Auu___409_422 ) ( _0_9  :  'Auu___410_423 ) ( _0_10  :  'Auu___411_424 ) -> ((_0_7), (_0_8), (_0_9), (_0_10))) mw) mx) my) mz)
end))


let mapJoin = (fun ( uu___418_768  :  Prims.nat ) ( mf  :  'Auu___416_730  ->  ('Auu___417_731, Prims.unit) Zen.Cost.cost ) ( x  :  ('Auu___416_730 * 'Auu___416_730) ) -> (join uu___418_768 uu___418_768 (Zen.Tuple.map mf x)))


let mapJoin3 = (fun ( uu___421_864  :  Prims.nat ) ( mf  :  'Auu___419_822  ->  ('Auu___420_823, Prims.unit) Zen.Cost.cost ) ( x  :  ('Auu___419_822 * 'Auu___419_822 * 'Auu___419_822) ) -> (join3 uu___421_864 uu___421_864 uu___421_864 (Zen.Tuple.map3 mf x)))


let mapJoin4 = (fun ( uu___424_970  :  Prims.nat ) ( mf  :  'Auu___422_924  ->  ('Auu___423_925, Prims.unit) Zen.Cost.cost ) ( x  :  ('Auu___422_924 * 'Auu___422_924 * 'Auu___422_924 * 'Auu___422_924) ) -> (join4 uu___424_970 uu___424_970 uu___424_970 uu___424_970 (Zen.Tuple.map4 mf x)))


let bind = (fun ( uu___427_1113  :  Prims.nat ) ( uu___428_1114  :  Prims.nat ) ( uu___429_1115  :  Prims.nat ) ( uu____1116  :  (('Auu___425_1043, Prims.unit) Zen.Cost.cost * ('Auu___425_1043, Prims.unit) Zen.Cost.cost) ) ( mf  :  'Auu___425_1043  ->  ('Auu___426_1044, Prims.unit) Zen.Cost.cost ) -> (match (uu____1116) with
| (mx, my) -> begin
(((Zen.Cost.bind uu___429_1115 uu___427_1113 mx mf)), ((Zen.Cost.bind uu___429_1115 uu___428_1114 my mf)))
end))


let bind3 = (fun ( uu___432_1339  :  Prims.nat ) ( uu___433_1340  :  Prims.nat ) ( uu___434_1341  :  Prims.nat ) ( uu___435_1342  :  Prims.nat ) ( uu____1343  :  (('Auu___430_1246, Prims.unit) Zen.Cost.cost * ('Auu___430_1246, Prims.unit) Zen.Cost.cost * ('Auu___430_1246, Prims.unit) Zen.Cost.cost) ) ( mf  :  'Auu___430_1246  ->  ('Auu___431_1247, Prims.unit) Zen.Cost.cost ) -> (match (uu____1343) with
| (mx, my, mz) -> begin
(((Zen.Cost.bind uu___435_1342 uu___432_1339 mx mf)), ((Zen.Cost.bind uu___435_1342 uu___433_1340 my mf)), ((Zen.Cost.bind uu___435_1342 uu___434_1341 mz mf)))
end))


let bind4 = (fun ( uu___436_1636  :  Prims.nat ) ( uu___437_1637  :  Prims.nat ) ( uu___438_1638  :  Prims.nat ) ( uu___439_1639  :  Prims.nat ) ( uu___440_1640  :  Prims.nat ) ( uu____1641  :  (('Aa, Prims.unit) Zen.Cost.cost * ('Aa, Prims.unit) Zen.Cost.cost * ('Aa, Prims.unit) Zen.Cost.cost * ('Aa, Prims.unit) Zen.Cost.cost) ) ( mf  :  'Aa  ->  ('Ab, Prims.unit) Zen.Cost.cost ) -> (match (uu____1641) with
| (mw, mx, my, mz) -> begin
(((Zen.Cost.bind uu___440_1640 uu___436_1636 mw mf)), ((Zen.Cost.bind uu___440_1640 uu___437_1637 mx mf)), ((Zen.Cost.bind uu___440_1640 uu___438_1638 my mf)), ((Zen.Cost.bind uu___440_1640 uu___439_1639 mz mf)))
end))


let bindJoin = (fun ( uu___443_1915  :  Prims.nat ) ( uu___444_1916  :  Prims.nat ) ( uu___445_1917  :  Prims.nat ) ( mt  :  (('Auu___441_1851, Prims.unit) Zen.Cost.cost * ('Auu___441_1851, Prims.unit) Zen.Cost.cost) ) ( mf  :  'Auu___441_1851  ->  ('Auu___442_1852, Prims.unit) Zen.Cost.cost ) -> (join (uu___444_1916 + uu___445_1917) (uu___443_1915 + uu___445_1917) (bind uu___443_1915 uu___444_1916 uu___445_1917 mt mf)))


let bindJoin3 = (fun ( uu___448_2096  :  Prims.nat ) ( uu___449_2097  :  Prims.nat ) ( uu___450_2098  :  Prims.nat ) ( uu___451_2099  :  Prims.nat ) ( mt  :  (('Auu___446_2015, Prims.unit) Zen.Cost.cost * ('Auu___446_2015, Prims.unit) Zen.Cost.cost * ('Auu___446_2015, Prims.unit) Zen.Cost.cost) ) ( mf  :  'Auu___446_2015  ->  ('Auu___447_2016, Prims.unit) Zen.Cost.cost ) -> (join3 (uu___448_2096 + uu___451_2099) (uu___449_2097 + uu___451_2099) (uu___450_2098 + uu___451_2099) (bind3 uu___448_2096 uu___449_2097 uu___450_2098 uu___451_2099 mt mf)))


let bindJoin4 = (fun ( uu___454_2324  :  Prims.nat ) ( uu___455_2325  :  Prims.nat ) ( uu___456_2326  :  Prims.nat ) ( uu___457_2327  :  Prims.nat ) ( uu___458_2328  :  Prims.nat ) ( mt  :  (('Auu___452_2226, Prims.unit) Zen.Cost.cost * ('Auu___452_2226, Prims.unit) Zen.Cost.cost * ('Auu___452_2226, Prims.unit) Zen.Cost.cost * ('Auu___452_2226, Prims.unit) Zen.Cost.cost) ) ( mf  :  'Auu___452_2226  ->  ('Auu___453_2227, Prims.unit) Zen.Cost.cost ) -> (join4 (uu___454_2324 + uu___458_2328) (uu___455_2325 + uu___458_2328) (uu___456_2326 + uu___458_2328) (uu___457_2327 + uu___458_2328) (bind4 uu___454_2324 uu___455_2325 uu___456_2326 uu___457_2327 uu___458_2328 mt mf)))




