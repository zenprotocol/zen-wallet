module Consensus.Utilities

// little-endian, fixed size (for any 8, 16 or 32 bit integral)
let inline toBytes (n: ^T) =
    let u = uint32 n
    let low = byte u
    let mLow = byte (u >>> 8)
    let mHigh = byte (u >>> 16)
    let high = byte (u >>> 24)
    [|low; mLow; mHigh; high|]

let bitsToBytes (bs:bool[]) =
    let ba = System.Collections.BitArray(bs)
    let ret : byte[] = Array.zeroCreate(bs.Length / 8)
    ba.CopyTo(ret,0)
    ret

let bytesToBits (bs:byte[]) = 
    let ba = System.Collections.BitArray(bs)
    let ret : bool[] = Array.zeroCreate(bs.Length*8)
    ba.CopyTo(ret,0)
    ret

