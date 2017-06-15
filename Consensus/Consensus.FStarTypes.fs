module Consensus.FStarTypes

open Consensus.Types

type hash = Hash

type lockCore = LockCore
type outputLock = OutputLock

let lockVersion : (OutputLock -> uint32) = lockVersion

let typeCode : (OutputLock -> int) = typeCode

let lockData : (OutputLock -> byte[] list) = lockData

type spend = Spend

type output = Output

type outpoint = Outpoint

type witness = Witness

type contract = Contract
type extendedContract = ExtendedContract

let contractVersion : (ExtendedContract -> uint32) = contractVersion

let ContractHashBytes = 32
let PubKeyHashBytes = 32
let TxHashBytes = 32

//type Transaction = {version: uint32; inputs: Outpoint list; witnesses: Witness list; outputs: Output list; contract: Contract option}
type transaction = Transaction
type nonce = Nonce

type blockHeader = BlockHeader

type contractContext = ContractContext

let merkleData = merkleData

type block = Block
