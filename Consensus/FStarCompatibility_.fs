module Consensus.FStarCompatibility

let FtsToFsArray : Zen.Array.t<'a, 'b> -> 'a array =
  function Zen.ArrayRealized.A a -> a
 
let FsToFstArray : byte array -> Zen.Array.t<FStar.UInt8.t, Prims.unit> =
  Zen.ArrayRealized.A