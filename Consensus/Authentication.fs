module Consensus.Authentication

open Sodium

let sign (msg:byte[]) (key:byte[]) = PublicKeyAuth.SignDetached(msg, key)
let verify signature msg key = PublicKeyAuth.VerifyDetached(signature, msg, key)

