#light "off"
module Zen.Crypto

type hash = Zen.Array.t<FStar.UInt8.t, Prims.unit>
type signature = Zen.Array.t<FStar.UInt8.t, Prims.unit>
type key = Zen.Array.t<FStar.UInt8.t, Prims.unit>

let sha2_256 (_:Prims.nat)
  (Zen.Array.Realized.A a:Zen.Array.t<FStar.UInt8.byte, Prims.unit>)
  : Zen.Cost.cost<Zen.Array.t<FStar.UInt8.byte, Prims.unit>,Prims.unit> =
  let sha = System.Security.Cryptography.SHA256.Create() in
  Microsoft.FSharp.Collections.Array.map (Microsoft.FSharp.Core.Operators.byte) a
  |> sha.ComputeHash
  |> Microsoft.FSharp.Collections.Array.map int
  |> Zen.Array.Realized.A |> Zen.Cost.ret

let sha2_512 (_:Prims.nat)
  (Zen.Array.Realized.A a:Zen.Array.t<FStar.UInt8.byte, Prims.unit>)
  : Zen.Cost.cost<Zen.Array.t<FStar.UInt8.byte, Prims.unit>, Prims.unit> =
  let sha = System.Security.Cryptography.SHA512.Create() in
  Microsoft.FSharp.Collections.Array.map (Microsoft.FSharp.Core.Operators.byte) a
  |> sha.ComputeHash
  |> Microsoft.FSharp.Collections.Array.map int
  |> Zen.Array.Realized.A |> Zen.Cost.ret

let sign (_:Prims.nat)
  (Zen.Array.Realized.A msg:Zen.Array.t<FStar.UInt8.byte, Prims.unit>)
  (Zen.Array.Realized.A key:Zen.Array.t<FStar.UInt8.byte, Prims.unit>)
  : Zen.Cost.cost<Zen.Array.t<FStar.UInt8.byte, Prims.unit>,Prims.unit> =
  let msg' = msg |> Microsoft.FSharp.Collections.Array.map byte in
  let key' = key |> Microsoft.FSharp.Collections.Array.map byte in
  Sodium.PublicKeyAuth.SignDetached(msg', key')
  |> Microsoft.FSharp.Collections.Array.map int
  |> Zen.Array.Realized.A |> Zen.Cost.ret

let verify (_:Prims.nat)
  (Zen.Array.Realized.A  msg:Zen.Array.t<FStar.UInt8.byte, Prims.unit>)
  (Zen.Array.Realized.A sign:Zen.Array.t<FStar.UInt8.byte, Prims.unit>)
  (Zen.Array.Realized.A  key:Zen.Array.t<FStar.UInt8.byte, Prims.unit>)
  : Prims.bool =
  let msg'  = msg  |> Microsoft.FSharp.Collections.Array.map byte in
  let sign' = sign |> Microsoft.FSharp.Collections.Array.map byte in
  let key'  = key  |> Microsoft.FSharp.Collections.Array.map byte in
  Sodium.PublicKeyAuth.VerifyDetached(sign', msg', key')
