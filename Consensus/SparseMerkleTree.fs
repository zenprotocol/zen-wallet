module Consensus.SparseMerkleTree


open MsgPack
open MsgPack.Serialization

open Consensus.Types
open Consensus.Serialization

open Consensus.Tree
open Consensus.Merkle

let zeroLoc = Array.zeroCreate<bool> 256

//let emptyTree = Branch