module Consensus.BlockValidation

open System
open MsgPack
open MsgPack.Serialization

open Consensus.Utilities
open Consensus.Types
open Consensus.Serialization
open Consensus.Tree
open Consensus.Merkle
open Consensus.TransactionValidation

let blocksPerUpdatePeriod = 4032
let expectedSecs = (float)blocksPerUpdatePeriod * Consensus.ChainParameters.blockInterval

let secsToTimespan (s:float<ChainParameters.sec>) = TimeSpan.FromSeconds(float s)
let expectedTimeSpan = secsToTimespan expectedSecs


let bigIntToBytes (bidiff:bigint) = Array.rev <| bidiff.ToByteArray()

let bytesToBigInt (diff:byte[]) = bigint (Array.rev diff)

let compressedToBigInt (pdiff:uint32) =
    let exp = int ((pdiff &&& 0xff000000u) >>> 24)
    let significand = (pdiff &&& 0x00ffffffu) + 1u //leads to ..fffff ending
    (bigint significand) * (pown 2I exp) - 1I //fixme

//let compressedToBytes pdiff = pdiff |> compressedToBigInt |> bigIntToBytes

let compressDifficulty bigdiff =
    if bigdiff > pown 2I 256 then 0xff000000u elif bigdiff <= 0I then 0x0u
    else
        let mutable exp = 0u
        let mutable bd = bigdiff
        while bd >= 2I do
            exp <- exp + 1u
            bd <- bd >>> 1
        0u //TODO
   

let target bigdiff = (pown 2I 256) / bigdiff |> bigIntToBytes

type Difficulty = {compressed:uint32; uncompressed:byte[]; big:bigint; target:byte[]}
    with
    static member create(compressed:uint32) =
        let big = compressedToBigInt compressed in
        let ucmp = bigIntToBytes big in
        {compressed=compressed; uncompressed=ucmp; big=big; target=target big}
    static member create(ucmp:byte[]) =
        let big= bytesToBigInt ucmp in
        {compressed=compressDifficulty big; uncompressed=ucmp; big=big; target=target big}
    static member create(big:bigint) =
        let ucmp = bigIntToBytes big in
        {compressed=compressDifficulty big; uncompressed=ucmp; big=big; target=target big}

let nextDifficulty {big=bigDiff} (timeDelta:TimeSpan) =
    let udiff = bigDiff * bigint expectedTimeSpan.Ticks / bigint timeDelta.Ticks
    let nextBigDiff = min (bigDiff * 4I) (max (bigDiff / 4I) udiff)
    Difficulty.create nextBigDiff


let checkHeader (header:BlockHeader) =
    // No additional checks until first soft-fork
    true

let totalWork (oldTotal:double) currentDiff =
    oldTotal + double (compressedToBigInt currentDiff)

let checkPOW (header:BlockHeader) consensusDifficulty =
    if header.pdiff <> consensusDifficulty.compressed then false
    else
        let blockID = blockHeaderHasher header
        blockID <= consensusDifficulty.target

let transactionMR cTW txs =
    merkleRoot cTW transactionHasher txs

let checkTransactionMerkleRoot block =
    let cTW = block.header.parent
    let txs = block.transactions
    block.header.txMerkleRoot = transactionMR cTW txs

let checkWitnessMerkleRoot block =
    //stub
    true

let checkContractMerkleRoot block =
    //stub
    true

type BlockContext = {difficulty: Difficulty}

let validateBlock blockcontext (block:Block) =
   checkHeader block.header &&
   checkPOW block.header blockcontext.difficulty &&
   checkTransactionMerkleRoot block &&
   checkWitnessMerkleRoot block &&
   checkContractMerkleRoot block
