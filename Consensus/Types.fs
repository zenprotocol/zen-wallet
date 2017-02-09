module Consensus.Types

type Hash = byte[]

type LockCore = {version: uint32; lockData: byte[] list}

type OutputLock =
    public
    | CoinbaseLock of LockCore
    | FeeLock of LockCore
    | ContractSacrificeLock of LockCore
    | PKLock of pkHash: Hash
    | ContractLock of contractHash : Hash * data : byte[]
    | HighVLock of lockCore : LockCore * typeCode : int

let lockVersion : (OutputLock -> uint32) = function
    | CoinbaseLock lCore
    | FeeLock lCore
    | ContractSacrificeLock lCore
    | HighVLock(lockCore = lCore) -> lCore.version
    | PKLock _ -> 0u
    | ContractLock _ -> 0u

let typeCode : (OutputLock -> int) = function
    | CoinbaseLock _ -> 0
    | FeeLock _ -> 1
    | ContractSacrificeLock _ -> 2
    | PKLock _ -> 3
    | ContractLock (_,_) -> 4
    | HighVLock (typeCode = tCode) -> tCode

let lockData : (OutputLock -> byte[] list) = function
    | CoinbaseLock lCore
    | FeeLock lCore
    | ContractSacrificeLock lCore
    | HighVLock(lockCore = lCore) -> lCore.lockData
    | PKLock pkHash ->
        [pkHash]
    | ContractLock (hash,data) ->
        [hash;data]

type Spend = {asset: Hash; amount: uint64}


type Output = {lock: OutputLock; spend: Spend}

type Outpoint = {txHash: Hash; index: uint32}

type Witness = byte[]

type Contract = {code: byte[]; bounds: byte[]; hint: byte[]}

type ExtendedContract =
    | Contract of Contract
    | HighVContract of version : uint32 * data : byte[]

let contractVersion : (ExtendedContract -> uint32) = function
    | Contract _ -> 0u
    | HighVContract(version=version) -> version

let ContractHashBytes = 32
let PubKeyHashBytes = 32
let TxHashBytes = 32

//type Transaction = {version: uint32; inputs: Outpoint list; witnesses: Witness list; outputs: Output list; contract: Contract option}
type Transaction = {version: uint32; inputs: Outpoint list; witnesses: Witness list; outputs: Output list; contract: ExtendedContract option}

type Nonce = byte[]

type BlockHeader = {
    version: uint32;
    parent: Hash;
    blockNumber: uint32;
    txMerkleRoot: Hash;
    witnessMerkleRoot: Hash;
    contractMerkleRoot: Hash;
    extraData: byte[] list;
    timestamp: int64;
    pdiff: uint32;
    nonce: Nonce;
    }

let merkleData (bh : BlockHeader) =
    bh.txMerkleRoot :: bh.witnessMerkleRoot :: bh.contractMerkleRoot :: bh.extraData

type Block = {
    header: BlockHeader;
    transactions: Transaction list
    }
