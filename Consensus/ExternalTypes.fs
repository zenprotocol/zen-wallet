module Consensus.ExternalTypes

open Consensus.Types


type hash = Hash

type lockCore = LockCore

type outputLock = OutputLock

//let lockVersion : (OutputLock -> uint32) = function
//    | CoinbaseLock lCore
//    | FeeLock lCore
//    | ContractSacrificeLock lCore
//    | HighVLock(lockCore = lCore) -> lCore.version
//    | PKLock _ -> 0u
//    | ContractLock _ -> 0u

//let typeCode : (OutputLock -> int) = function
//    | CoinbaseLock _ -> 0
//    | FeeLock _ -> 1
//    | ContractSacrificeLock _ -> 2
//    | PKLock _ -> 3
//    | ContractLock (_,_) -> 4
//    | HighVLock (typeCode = tCode) -> tCode

//let lockData : (OutputLock -> byte[] list) = function
    //| CoinbaseLock lCore
    //| FeeLock lCore
    //| ContractSacrificeLock lCore
    //| HighVLock(lockCore = lCore) -> lCore.lockData
    //| PKLock pkHash ->
    //    [pkHash]
    //| ContractLock (hash,data) ->
        //[hash;data]

type spend = Spend

type output = Output

type outpoint = Outpoint

type witness = Witness

type contract = Contract

type extendedContract = ExtendedContract

//let contractVersion : (ExtendedContract -> uint32) = function
    //| Contract _ -> 0u
    //| HighVContract(version=version) -> version

//let ContractHashBytes = 32
//let PubKeyHashBytes = 32
//let TxHashBytes = 32

type transaction = Transaction

type nonce = Nonce

type blockHeader = BlockHeader

type contractContext = ContractContext

//let merkleData (bh : BlockHeader) =
    //bh.txMerkleRoot :: bh.witnessMerkleRoot :: bh.contractMerkleRoot :: bh.extraData

type block = Block