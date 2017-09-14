#light "off"
module Zen.Merkle

open Zen.Types.Extracted

module ZArr = Zen.Array.Extracted
module Cost = Zen.Cost.Realized

let private innerHash : byte[] -> byte[] =
    fun bs ->
    let res = Array.zeroCreate 32 in
    let sha3 = new Org.BouncyCastle.Crypto.Digests.Sha3Digest(256) in
    sha3.BlockUpdate(bs,0,Array.length bs);
    sha3.DoFinal(res, 0) |> ignore;
    res

let rootFromAuditPath
    ( _: Prims.nat)
    ( item : hash )
    ( location: Prims.nat )
    ( hashes : ZArr.t<hash, Prims.unit> )
    : Cost.t<hash, Prims.unit> =
        lazy (
            fst <|
            Array.fold
                (fun (v, loc) h ->
                    if loc % 2u = 0u
                    then
                        (innerHash <| Array.append v h, loc >>> 1)
                    else
                        (innerHash <| Array.append h v, loc >>> 1))
                (item,uint32 location) hashes
        )
        |> Cost.C
