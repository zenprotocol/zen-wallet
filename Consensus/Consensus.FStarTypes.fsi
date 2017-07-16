module Consensus.FStarTypes

open Consensus.Types

type hash = byte[]

type lockCore = {version: uint32; lockData: byte[] list}
type outputLock =
  | CoinbaseLock of lockCore
  | FeeLock of lockCore
  | ContractSacrificeLock of lockCore
  | PKLock of pkHash: hash
  | ContractLock of contractHash : hash * data : byte[]
  | HighVLock of lockCore : lockCore * typeCode : int

val lockVersion : (outputLock -> uint32)

val typeCode : (outputLock -> int)
val lockData : (outputLock -> byte[] list)

type spend = {asset: hash; amount: uint64}

type output = {lock: outputLock; spend: spend}

type outpoint = {txHash: hash; index: uint32}

type witness = byte[]

type contract = {code: byte[]; bounds: byte[]; hint: byte[]}
type extendedContract =
  | Contract of contract
  | HighVContract of version : uint32 * data : byte[]

val contractVersion : (extendedContract -> uint32)

val ContractHashBytes = uint32
val PubKeyHashBytes = uint32
val TxHashBytes = uint32

//type Transaction = {version: uint32; inputs: Outpoint list; witnesses: Witness list; outputs: Output list; contract: Contract option}
type transaction = {version: uint32;
  inputs: outpoint list;
  witnesses: witness list;
  outputs: output list;
  contract: extendedContract option}

type nonce = byte[]

type blockHeader = {
    version: uint32;
    parent: hash;
    blockNumber: uint32;
    txMerkleRoot: hash;
    witnessMerkleRoot: hash;
    contractMerkleRoot: hash;
    extraData: byte[] list;
    timestamp: int64;
    pdiff: uint32;
    nonce: Nonce;
    }

type contractContext = {contractId: byte[]; utxo: Map<outpoint, output>; tip: blockHeader; }

val merkleData : blockHeader -> byte[] list

type block = {
    header: blockHeader;
    transactions: transaction list
    }
