#light "off"
module Consensus.Types
open Prims
open FStar.Pervasives

type lockCore =
Consensus.Realized.lockCore


type contract =
Consensus.Realized.contract


type extraData =
Consensus.Realized.extraData


type byte =
FStar.UInt8.t


type opcode =
FStar.UInt8.t


type hash =
Zen.Crypto.hash

type spend =
{asset : hash; amount : FStar.UInt64.t}


let __proj__Mkspend__item__asset : spend  ->  hash = (fun ( projectee  :  spend ) -> (match (projectee) with
| {asset = __fname__asset; amount = __fname__amount} -> begin
__fname__asset
end))


let __proj__Mkspend__item__amount : spend  ->  FStar.UInt64.t = (fun ( projectee  :  spend ) -> (match (projectee) with
| {asset = __fname__asset; amount = __fname__amount} -> begin
__fname__amount
end))

type outpoint =
{txHash : hash; index : FStar.UInt32.t}


let __proj__Mkoutpoint__item__txHash : outpoint  ->  hash = (fun ( projectee  :  outpoint ) -> (match (projectee) with
| {txHash = __fname__txHash; index = __fname__index} -> begin
__fname__txHash
end))


let __proj__Mkoutpoint__item__index : outpoint  ->  FStar.UInt32.t = (fun ( projectee  :  outpoint ) -> (match (projectee) with
| {txHash = __fname__txHash; index = __fname__index} -> begin
__fname__index
end))


type 'An witness =
(byte, 'An) Zen.Array.t


type nonce =
(byte, Prims.unit) Zen.Array.t

type blockHeader =
{version : FStar.UInt32.t; parent : hash; blockNumber : FStar.UInt32.t; txMerkleRoot : hash; witnessMerkleRoot : hash; contractMerkleRoot : hash; extraData : extraData; timestamp : FStar.Int64.t; pdiff : FStar.UInt32.t; nonce : nonce}


let __proj__MkblockHeader__item__version : blockHeader  ->  FStar.UInt32.t = (fun ( projectee  :  blockHeader ) -> (match (projectee) with
| {version = __fname__version; parent = __fname__parent; blockNumber = __fname__blockNumber; txMerkleRoot = __fname__txMerkleRoot; witnessMerkleRoot = __fname__witnessMerkleRoot; contractMerkleRoot = __fname__contractMerkleRoot; extraData = __fname__extraData; timestamp = __fname__timestamp; pdiff = __fname__pdiff; nonce = __fname__nonce} -> begin
__fname__version
end))


let __proj__MkblockHeader__item__parent : blockHeader  ->  hash = (fun ( projectee  :  blockHeader ) -> (match (projectee) with
| {version = __fname__version; parent = __fname__parent; blockNumber = __fname__blockNumber; txMerkleRoot = __fname__txMerkleRoot; witnessMerkleRoot = __fname__witnessMerkleRoot; contractMerkleRoot = __fname__contractMerkleRoot; extraData = __fname__extraData; timestamp = __fname__timestamp; pdiff = __fname__pdiff; nonce = __fname__nonce} -> begin
__fname__parent
end))


let __proj__MkblockHeader__item__blockNumber : blockHeader  ->  FStar.UInt32.t = (fun ( projectee  :  blockHeader ) -> (match (projectee) with
| {version = __fname__version; parent = __fname__parent; blockNumber = __fname__blockNumber; txMerkleRoot = __fname__txMerkleRoot; witnessMerkleRoot = __fname__witnessMerkleRoot; contractMerkleRoot = __fname__contractMerkleRoot; extraData = __fname__extraData; timestamp = __fname__timestamp; pdiff = __fname__pdiff; nonce = __fname__nonce} -> begin
__fname__blockNumber
end))


let __proj__MkblockHeader__item__txMerkleRoot : blockHeader  ->  hash = (fun ( projectee  :  blockHeader ) -> (match (projectee) with
| {version = __fname__version; parent = __fname__parent; blockNumber = __fname__blockNumber; txMerkleRoot = __fname__txMerkleRoot; witnessMerkleRoot = __fname__witnessMerkleRoot; contractMerkleRoot = __fname__contractMerkleRoot; extraData = __fname__extraData; timestamp = __fname__timestamp; pdiff = __fname__pdiff; nonce = __fname__nonce} -> begin
__fname__txMerkleRoot
end))


let __proj__MkblockHeader__item__witnessMerkleRoot : blockHeader  ->  hash = (fun ( projectee  :  blockHeader ) -> (match (projectee) with
| {version = __fname__version; parent = __fname__parent; blockNumber = __fname__blockNumber; txMerkleRoot = __fname__txMerkleRoot; witnessMerkleRoot = __fname__witnessMerkleRoot; contractMerkleRoot = __fname__contractMerkleRoot; extraData = __fname__extraData; timestamp = __fname__timestamp; pdiff = __fname__pdiff; nonce = __fname__nonce} -> begin
__fname__witnessMerkleRoot
end))


let __proj__MkblockHeader__item__contractMerkleRoot : blockHeader  ->  hash = (fun ( projectee  :  blockHeader ) -> (match (projectee) with
| {version = __fname__version; parent = __fname__parent; blockNumber = __fname__blockNumber; txMerkleRoot = __fname__txMerkleRoot; witnessMerkleRoot = __fname__witnessMerkleRoot; contractMerkleRoot = __fname__contractMerkleRoot; extraData = __fname__extraData; timestamp = __fname__timestamp; pdiff = __fname__pdiff; nonce = __fname__nonce} -> begin
__fname__contractMerkleRoot
end))


let __proj__MkblockHeader__item__extraData : blockHeader  ->  extraData = (fun ( projectee  :  blockHeader ) -> (match (projectee) with
| {version = __fname__version; parent = __fname__parent; blockNumber = __fname__blockNumber; txMerkleRoot = __fname__txMerkleRoot; witnessMerkleRoot = __fname__witnessMerkleRoot; contractMerkleRoot = __fname__contractMerkleRoot; extraData = __fname__extraData; timestamp = __fname__timestamp; pdiff = __fname__pdiff; nonce = __fname__nonce} -> begin
__fname__extraData
end))


let __proj__MkblockHeader__item__timestamp : blockHeader  ->  FStar.Int64.t = (fun ( projectee  :  blockHeader ) -> (match (projectee) with
| {version = __fname__version; parent = __fname__parent; blockNumber = __fname__blockNumber; txMerkleRoot = __fname__txMerkleRoot; witnessMerkleRoot = __fname__witnessMerkleRoot; contractMerkleRoot = __fname__contractMerkleRoot; extraData = __fname__extraData; timestamp = __fname__timestamp; pdiff = __fname__pdiff; nonce = __fname__nonce} -> begin
__fname__timestamp
end))


let __proj__MkblockHeader__item__pdiff : blockHeader  ->  FStar.UInt32.t = (fun ( projectee  :  blockHeader ) -> (match (projectee) with
| {version = __fname__version; parent = __fname__parent; blockNumber = __fname__blockNumber; txMerkleRoot = __fname__txMerkleRoot; witnessMerkleRoot = __fname__witnessMerkleRoot; contractMerkleRoot = __fname__contractMerkleRoot; extraData = __fname__extraData; timestamp = __fname__timestamp; pdiff = __fname__pdiff; nonce = __fname__nonce} -> begin
__fname__pdiff
end))


let __proj__MkblockHeader__item__nonce : blockHeader  ->  nonce = (fun ( projectee  :  blockHeader ) -> (match (projectee) with
| {version = __fname__version; parent = __fname__parent; blockNumber = __fname__blockNumber; txMerkleRoot = __fname__txMerkleRoot; witnessMerkleRoot = __fname__witnessMerkleRoot; contractMerkleRoot = __fname__contractMerkleRoot; extraData = __fname__extraData; timestamp = __fname__timestamp; pdiff = __fname__pdiff; nonce = __fname__nonce} -> begin
__fname__nonce
end))

type 'dummyV0 data =
| Bool of Prims.bool
| Byte of FStar.UInt8.t
| Empty
| Hash of hash
| Key of Zen.Crypto.key
| Outpoint of outpoint
| Output of output
| OutputLock of outputLock
| Sig of Zen.Crypto.signature
| UInt8 of FStar.UInt8.t
| UInt32 of FStar.UInt32.t
| UInt64 of FStar.UInt64.t
| BoolVector of Prims.nat * (Prims.bool, Prims.unit) Zen.Vector.t
| BoolArray of Prims.nat * (Prims.bool, Prims.unit) Zen.Array.t
| ByteVector of Prims.nat * (FStar.UInt8.t, Prims.unit) Zen.Vector.t
| ByteArray of Prims.nat * (FStar.UInt8.t, Prims.unit) Zen.Array.t
| HashVector of Prims.nat * (hash, Prims.unit) Zen.Vector.t
| HashArray of Prims.nat * (hash, Prims.unit) Zen.Array.t
| KeyVector of Prims.nat * (Zen.Crypto.key, Prims.unit) Zen.Vector.t
| KeyArray of Prims.nat * (Zen.Crypto.key, Prims.unit) Zen.Array.t
| SigVector of Prims.nat * (Zen.Crypto.signature, Prims.unit) Zen.Vector.t
| SigArray of Prims.nat * (Zen.Crypto.signature, Prims.unit) Zen.Array.t
| OutpointVector of Prims.nat * (outpoint, Prims.unit) Zen.Vector.t
| OutpointArray of Prims.nat * (outpoint, Prims.unit) Zen.Array.t
| OutputVector of Prims.nat * (output, Prims.unit) Zen.Vector.t
| OutputArray of Prims.nat * (output, Prims.unit) Zen.Array.t
| OutputLockVector of Prims.nat * (outputLock, Prims.unit) Zen.Vector.t
| OutputLockArray of Prims.nat * (outputLock, Prims.unit) Zen.Array.t
| UInt8Vector of Prims.nat * (FStar.UInt8.t, Prims.unit) Zen.Vector.t
| UInt8Array of Prims.nat * (FStar.UInt8.t, Prims.unit) Zen.Array.t
| UInt32Vector of Prims.nat * (FStar.UInt32.t, Prims.unit) Zen.Vector.t
| UInt32Array of Prims.nat * (FStar.UInt32.t, Prims.unit) Zen.Array.t
| UInt64Vector of Prims.nat * (FStar.UInt64.t, Prims.unit) Zen.Vector.t
| UInt64Array of Prims.nat * (FStar.UInt64.t, Prims.unit) Zen.Array.t
| Data2 of Prims.nat * Prims.nat * Prims.unit data * Prims.unit data
| Data3 of Prims.nat * Prims.nat * Prims.nat * Prims.unit data * Prims.unit data * Prims.unit data
| Data4 of Prims.nat * Prims.nat * Prims.nat * Prims.nat * Prims.unit data * Prims.unit data * Prims.unit data * Prims.unit data 
 and outputLock =
| CoinbaseLock of lockCore
| FeeLock of lockCore
| ContractSacrificeLock of lockCore
| PKLock of hash
| ContractLock of hash * Prims.nat * Prims.unit data
| HighVLock of (lockCore * Prims.nat) 
 and output =
{lock : outputLock; spend : spend}


let uu___is_Bool : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____1436  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Bool (v1) -> begin
true
end
| uu____1447 -> begin
false
end))


let __proj__Bool__item__v : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____1476  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Bool (v1) -> begin
v1
end))


let uu___is_Byte : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____1508  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Byte (v1) -> begin
true
end
| uu____1519 -> begin
false
end))


let __proj__Byte__item__v : Prims.nat  ->  Prims.unit data  ->  FStar.UInt8.t = (fun ( uu____1548  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Byte (v1) -> begin
v1
end))


let uu___is_Empty : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____1579  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Empty -> begin
true
end
| uu____1589 -> begin
false
end))


let uu___is_Hash : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____1616  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Hash (v1) -> begin
true
end
| uu____1628 -> begin
false
end))


let __proj__Hash__item__v : Prims.nat  ->  Prims.unit data  ->  hash = (fun ( uu____1658  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Hash (v1) -> begin
v1
end))


let uu___is_Key : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____1692  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Key (v1) -> begin
true
end
| uu____1703 -> begin
false
end))


let __proj__Key__item__v : Prims.nat  ->  Prims.unit data  ->  Zen.Crypto.key = (fun ( uu____1732  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Key (v1) -> begin
v1
end))


let uu___is_Outpoint : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____1764  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Outpoint (v1) -> begin
true
end
| uu____1775 -> begin
false
end))


let __proj__Outpoint__item__v : Prims.nat  ->  Prims.unit data  ->  outpoint = (fun ( uu____1804  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Outpoint (v1) -> begin
v1
end))


let uu___is_Output : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____1836  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Output (v1) -> begin
true
end
| uu____1847 -> begin
false
end))


let __proj__Output__item__v : Prims.nat  ->  Prims.unit data  ->  output = (fun ( uu____1876  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Output (v1) -> begin
v1
end))


let uu___is_OutputLock : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____1908  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| OutputLock (v1) -> begin
true
end
| uu____1919 -> begin
false
end))


let __proj__OutputLock__item__v : Prims.nat  ->  Prims.unit data  ->  outputLock = (fun ( uu____1948  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| OutputLock (v1) -> begin
v1
end))


let uu___is_Sig : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____1980  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Sig (v1) -> begin
true
end
| uu____1991 -> begin
false
end))


let __proj__Sig__item__v : Prims.nat  ->  Prims.unit data  ->  Zen.Crypto.signature = (fun ( uu____2020  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Sig (v1) -> begin
v1
end))


let uu___is_UInt8 : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____2052  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt8 (v1) -> begin
true
end
| uu____2063 -> begin
false
end))


let __proj__UInt8__item__v : Prims.nat  ->  Prims.unit data  ->  FStar.UInt8.t = (fun ( uu____2092  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt8 (v1) -> begin
v1
end))


let uu___is_UInt32 : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____2124  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt32 (v1) -> begin
true
end
| uu____2135 -> begin
false
end))


let __proj__UInt32__item__v : Prims.nat  ->  Prims.unit data  ->  FStar.UInt32.t = (fun ( uu____2164  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt32 (v1) -> begin
v1
end))


let uu___is_UInt64 : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____2196  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt64 (v1) -> begin
true
end
| uu____2207 -> begin
false
end))


let __proj__UInt64__item__v : Prims.nat  ->  Prims.unit data  ->  FStar.UInt64.t = (fun ( uu____2236  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt64 (v1) -> begin
v1
end))


let uu___is_BoolVector : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____2269  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| BoolVector (l, v1) -> begin
true
end
| uu____2289 -> begin
false
end))


let __proj__BoolVector__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____2323  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| BoolVector (l, v1) -> begin
l
end))


let __proj__BoolVector__item__v : Prims.nat  ->  Prims.unit data  ->  (Prims.bool, Prims.unit) Zen.Vector.t = (fun ( uu____2383  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| BoolVector (l, v1) -> begin
v1
end))


let uu___is_BoolArray : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____2435  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| BoolArray (l, a) -> begin
true
end
| uu____2455 -> begin
false
end))


let __proj__BoolArray__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____2489  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| BoolArray (l, a) -> begin
l
end))


let __proj__BoolArray__item__a : Prims.nat  ->  Prims.unit data  ->  (Prims.bool, Prims.unit) Zen.Array.t = (fun ( uu____2549  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| BoolArray (l, a) -> begin
a
end))


let uu___is_ByteVector : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____2601  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| ByteVector (l, v1) -> begin
true
end
| uu____2621 -> begin
false
end))


let __proj__ByteVector__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____2655  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| ByteVector (l, v1) -> begin
l
end))


let __proj__ByteVector__item__v : Prims.nat  ->  Prims.unit data  ->  (FStar.UInt8.t, Prims.unit) Zen.Vector.t = (fun ( uu____2715  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| ByteVector (l, v1) -> begin
v1
end))


let uu___is_ByteArray : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____2767  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| ByteArray (l, a) -> begin
true
end
| uu____2787 -> begin
false
end))


let __proj__ByteArray__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____2821  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| ByteArray (l, a) -> begin
l
end))


let __proj__ByteArray__item__a : Prims.nat  ->  Prims.unit data  ->  (FStar.UInt8.t, Prims.unit) Zen.Array.t = (fun ( uu____2881  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| ByteArray (l, a) -> begin
a
end))


let uu___is_HashVector : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____2933  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| HashVector (l, v1) -> begin
true
end
| uu____2954 -> begin
false
end))


let __proj__HashVector__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____2988  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| HashVector (l, v1) -> begin
l
end))


let __proj__HashVector__item__v : Prims.nat  ->  Prims.unit data  ->  (hash, Prims.unit) Zen.Vector.t = (fun ( uu____3050  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| HashVector (l, v1) -> begin
v1
end))


let uu___is_HashArray : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____3104  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| HashArray (l, a) -> begin
true
end
| uu____3125 -> begin
false
end))


let __proj__HashArray__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____3159  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| HashArray (l, a) -> begin
l
end))


let __proj__HashArray__item__a : Prims.nat  ->  Prims.unit data  ->  (hash, Prims.unit) Zen.Array.t = (fun ( uu____3221  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| HashArray (l, a) -> begin
a
end))


let uu___is_KeyVector : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____3275  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| KeyVector (l, v1) -> begin
true
end
| uu____3295 -> begin
false
end))


let __proj__KeyVector__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____3329  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| KeyVector (l, v1) -> begin
l
end))


let __proj__KeyVector__item__v : Prims.nat  ->  Prims.unit data  ->  (Zen.Crypto.key, Prims.unit) Zen.Vector.t = (fun ( uu____3389  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| KeyVector (l, v1) -> begin
v1
end))


let uu___is_KeyArray : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____3441  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| KeyArray (l, a) -> begin
true
end
| uu____3461 -> begin
false
end))


let __proj__KeyArray__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____3495  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| KeyArray (l, a) -> begin
l
end))


let __proj__KeyArray__item__a : Prims.nat  ->  Prims.unit data  ->  (Zen.Crypto.key, Prims.unit) Zen.Array.t = (fun ( uu____3555  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| KeyArray (l, a) -> begin
a
end))


let uu___is_SigVector : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____3607  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| SigVector (l, v1) -> begin
true
end
| uu____3627 -> begin
false
end))


let __proj__SigVector__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____3661  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| SigVector (l, v1) -> begin
l
end))


let __proj__SigVector__item__v : Prims.nat  ->  Prims.unit data  ->  (Zen.Crypto.signature, Prims.unit) Zen.Vector.t = (fun ( uu____3721  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| SigVector (l, v1) -> begin
v1
end))


let uu___is_SigArray : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____3773  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| SigArray (l, a) -> begin
true
end
| uu____3793 -> begin
false
end))


let __proj__SigArray__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____3827  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| SigArray (l, a) -> begin
l
end))


let __proj__SigArray__item__a : Prims.nat  ->  Prims.unit data  ->  (Zen.Crypto.signature, Prims.unit) Zen.Array.t = (fun ( uu____3887  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| SigArray (l, a) -> begin
a
end))


let uu___is_OutpointVector : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____3939  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| OutpointVector (l, v1) -> begin
true
end
| uu____3959 -> begin
false
end))


let __proj__OutpointVector__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____3993  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| OutpointVector (l, v1) -> begin
l
end))


let __proj__OutpointVector__item__v : Prims.nat  ->  Prims.unit data  ->  (outpoint, Prims.unit) Zen.Vector.t = (fun ( uu____4053  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| OutpointVector (l, v1) -> begin
v1
end))


let uu___is_OutpointArray : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____4105  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| OutpointArray (l, a) -> begin
true
end
| uu____4125 -> begin
false
end))


let __proj__OutpointArray__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____4159  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| OutpointArray (l, a) -> begin
l
end))


let __proj__OutpointArray__item__a : Prims.nat  ->  Prims.unit data  ->  (outpoint, Prims.unit) Zen.Array.t = (fun ( uu____4219  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| OutpointArray (l, a) -> begin
a
end))


let uu___is_OutputVector : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____4271  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| OutputVector (l, v1) -> begin
true
end
| uu____4291 -> begin
false
end))


let __proj__OutputVector__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____4325  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| OutputVector (l, v1) -> begin
l
end))


let __proj__OutputVector__item__v : Prims.nat  ->  Prims.unit data  ->  (output, Prims.unit) Zen.Vector.t = (fun ( uu____4385  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| OutputVector (l, v1) -> begin
v1
end))


let uu___is_OutputArray : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____4437  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| OutputArray (l, a) -> begin
true
end
| uu____4457 -> begin
false
end))


let __proj__OutputArray__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____4491  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| OutputArray (l, a) -> begin
l
end))


let __proj__OutputArray__item__a : Prims.nat  ->  Prims.unit data  ->  (output, Prims.unit) Zen.Array.t = (fun ( uu____4551  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| OutputArray (l, a) -> begin
a
end))


let uu___is_OutputLockVector : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____4603  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| OutputLockVector (l, v1) -> begin
true
end
| uu____4623 -> begin
false
end))


let __proj__OutputLockVector__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____4657  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| OutputLockVector (l, v1) -> begin
l
end))


let __proj__OutputLockVector__item__v : Prims.nat  ->  Prims.unit data  ->  (outputLock, Prims.unit) Zen.Vector.t = (fun ( uu____4717  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| OutputLockVector (l, v1) -> begin
v1
end))


let uu___is_OutputLockArray : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____4769  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| OutputLockArray (l, a) -> begin
true
end
| uu____4789 -> begin
false
end))


let __proj__OutputLockArray__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____4823  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| OutputLockArray (l, a) -> begin
l
end))


let __proj__OutputLockArray__item__a : Prims.nat  ->  Prims.unit data  ->  (outputLock, Prims.unit) Zen.Array.t = (fun ( uu____4883  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| OutputLockArray (l, a) -> begin
a
end))


let uu___is_UInt8Vector : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____4935  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt8Vector (l, v1) -> begin
true
end
| uu____4955 -> begin
false
end))


let __proj__UInt8Vector__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____4989  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt8Vector (l, v1) -> begin
l
end))


let __proj__UInt8Vector__item__v : Prims.nat  ->  Prims.unit data  ->  (FStar.UInt8.t, Prims.unit) Zen.Vector.t = (fun ( uu____5049  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt8Vector (l, v1) -> begin
v1
end))


let uu___is_UInt8Array : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____5101  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt8Array (l, a) -> begin
true
end
| uu____5121 -> begin
false
end))


let __proj__UInt8Array__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____5155  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt8Array (l, a) -> begin
l
end))


let __proj__UInt8Array__item__a : Prims.nat  ->  Prims.unit data  ->  (FStar.UInt8.t, Prims.unit) Zen.Array.t = (fun ( uu____5215  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt8Array (l, a) -> begin
a
end))


let uu___is_UInt32Vector : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____5267  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt32Vector (l, v1) -> begin
true
end
| uu____5287 -> begin
false
end))


let __proj__UInt32Vector__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____5321  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt32Vector (l, v1) -> begin
l
end))


let __proj__UInt32Vector__item__v : Prims.nat  ->  Prims.unit data  ->  (FStar.UInt32.t, Prims.unit) Zen.Vector.t = (fun ( uu____5381  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt32Vector (l, v1) -> begin
v1
end))


let uu___is_UInt32Array : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____5433  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt32Array (l, a) -> begin
true
end
| uu____5453 -> begin
false
end))


let __proj__UInt32Array__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____5487  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt32Array (l, a) -> begin
l
end))


let __proj__UInt32Array__item__a : Prims.nat  ->  Prims.unit data  ->  (FStar.UInt32.t, Prims.unit) Zen.Array.t = (fun ( uu____5547  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt32Array (l, a) -> begin
a
end))


let uu___is_UInt64Vector : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____5599  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt64Vector (l, v1) -> begin
true
end
| uu____5619 -> begin
false
end))


let __proj__UInt64Vector__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____5653  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt64Vector (l, v1) -> begin
l
end))


let __proj__UInt64Vector__item__v : Prims.nat  ->  Prims.unit data  ->  (FStar.UInt64.t, Prims.unit) Zen.Vector.t = (fun ( uu____5713  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt64Vector (l, v1) -> begin
v1
end))


let uu___is_UInt64Array : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____5765  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt64Array (l, a) -> begin
true
end
| uu____5785 -> begin
false
end))


let __proj__UInt64Array__item__l : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____5819  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt64Array (l, a) -> begin
l
end))


let __proj__UInt64Array__item__a : Prims.nat  ->  Prims.unit data  ->  (FStar.UInt64.t, Prims.unit) Zen.Array.t = (fun ( uu____5879  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| UInt64Array (l, a) -> begin
a
end))


let uu___is_Data2 : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____5933  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data2 (n1, n2, _1, _2) -> begin
true
end
| uu____5959 -> begin
false
end))


let __proj__Data2__item__n1 : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____5995  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data2 (n1, n2, _1, _2) -> begin
n1
end))


let __proj__Data2__item__n2 : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____6053  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data2 (n1, n2, _1, _2) -> begin
n2
end))


let __proj__Data2__item___1 : Prims.nat  ->  Prims.unit data  ->  Prims.unit data = (fun ( uu____6111  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data2 (n1, n2, _1, _2) -> begin
_1
end))


let __proj__Data2__item___2 : Prims.nat  ->  Prims.unit data  ->  Prims.unit data = (fun ( uu____6171  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data2 (n1, n2, _1, _2) -> begin
_2
end))


let uu___is_Data3 : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____6227  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data3 (n1, n2, n3, _1, _2, _3) -> begin
true
end
| uu____6261 -> begin
false
end))


let __proj__Data3__item__n1 : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____6299  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data3 (n1, n2, n3, _1, _2, _3) -> begin
n1
end))


let __proj__Data3__item__n2 : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____6367  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data3 (n1, n2, n3, _1, _2, _3) -> begin
n2
end))


let __proj__Data3__item__n3 : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____6435  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data3 (n1, n2, n3, _1, _2, _3) -> begin
n3
end))


let __proj__Data3__item___1 : Prims.nat  ->  Prims.unit data  ->  Prims.unit data = (fun ( uu____6503  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data3 (n1, n2, n3, _1, _2, _3) -> begin
_1
end))


let __proj__Data3__item___2 : Prims.nat  ->  Prims.unit data  ->  Prims.unit data = (fun ( uu____6573  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data3 (n1, n2, n3, _1, _2, _3) -> begin
_2
end))


let __proj__Data3__item___3 : Prims.nat  ->  Prims.unit data  ->  Prims.unit data = (fun ( uu____6643  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data3 (n1, n2, n3, _1, _2, _3) -> begin
_3
end))


let uu___is_Data4 : Prims.nat  ->  Prims.unit data  ->  Prims.bool = (fun ( uu____6709  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data4 (n1, n2, n3, n4, _1, _2, _3, _4) -> begin
true
end
| uu____6751 -> begin
false
end))


let __proj__Data4__item__n1 : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____6791  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data4 (n1, n2, n3, n4, _1, _2, _3, _4) -> begin
n1
end))


let __proj__Data4__item__n2 : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____6869  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data4 (n1, n2, n3, n4, _1, _2, _3, _4) -> begin
n2
end))


let __proj__Data4__item__n3 : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____6947  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data4 (n1, n2, n3, n4, _1, _2, _3, _4) -> begin
n3
end))


let __proj__Data4__item__n4 : Prims.nat  ->  Prims.unit data  ->  Prims.nat = (fun ( uu____7025  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data4 (n1, n2, n3, n4, _1, _2, _3, _4) -> begin
n4
end))


let __proj__Data4__item___1 : Prims.nat  ->  Prims.unit data  ->  Prims.unit data = (fun ( uu____7103  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data4 (n1, n2, n3, n4, _1, _2, _3, _4) -> begin
_1
end))


let __proj__Data4__item___2 : Prims.nat  ->  Prims.unit data  ->  Prims.unit data = (fun ( uu____7183  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data4 (n1, n2, n3, n4, _1, _2, _3, _4) -> begin
_2
end))


let __proj__Data4__item___3 : Prims.nat  ->  Prims.unit data  ->  Prims.unit data = (fun ( uu____7263  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data4 (n1, n2, n3, n4, _1, _2, _3, _4) -> begin
_3
end))


let __proj__Data4__item___4 : Prims.nat  ->  Prims.unit data  ->  Prims.unit data = (fun ( uu____7343  :  Prims.nat ) ( projectee  :  Prims.unit data ) -> (match (projectee) with
| Data4 (n1, n2, n3, n4, _1, _2, _3, _4) -> begin
_4
end))


let uu___is_CoinbaseLock : outputLock  ->  Prims.bool = (fun ( projectee  :  outputLock ) -> (match (projectee) with
| CoinbaseLock (_0) -> begin
true
end
| uu____7398 -> begin
false
end))


let __proj__CoinbaseLock__item___0 : outputLock  ->  lockCore = (fun ( projectee  :  outputLock ) -> (match (projectee) with
| CoinbaseLock (_0) -> begin
_0
end))


let uu___is_FeeLock : outputLock  ->  Prims.bool = (fun ( projectee  :  outputLock ) -> (match (projectee) with
| FeeLock (_0) -> begin
true
end
| uu____7418 -> begin
false
end))


let __proj__FeeLock__item___0 : outputLock  ->  lockCore = (fun ( projectee  :  outputLock ) -> (match (projectee) with
| FeeLock (_0) -> begin
_0
end))


let uu___is_ContractSacrificeLock : outputLock  ->  Prims.bool = (fun ( projectee  :  outputLock ) -> (match (projectee) with
| ContractSacrificeLock (_0) -> begin
true
end
| uu____7438 -> begin
false
end))


let __proj__ContractSacrificeLock__item___0 : outputLock  ->  lockCore = (fun ( projectee  :  outputLock ) -> (match (projectee) with
| ContractSacrificeLock (_0) -> begin
_0
end))


let uu___is_PKLock : outputLock  ->  Prims.bool = (fun ( projectee  :  outputLock ) -> (match (projectee) with
| PKLock (_0) -> begin
true
end
| uu____7459 -> begin
false
end))


let __proj__PKLock__item___0 : outputLock  ->  hash = (fun ( projectee  :  outputLock ) -> (match (projectee) with
| PKLock (_0) -> begin
_0
end))


let uu___is_ContractLock : outputLock  ->  Prims.bool = (fun ( projectee  :  outputLock ) -> (match (projectee) with
| ContractLock (contractHash, n1, _2) -> begin
true
end
| uu____7493 -> begin
false
end))


let __proj__ContractLock__item__contractHash : outputLock  ->  hash = (fun ( projectee  :  outputLock ) -> (match (projectee) with
| ContractLock (contractHash, n1, _2) -> begin
contractHash
end))


let __proj__ContractLock__item__n : outputLock  ->  Prims.nat = (fun ( projectee  :  outputLock ) -> (match (projectee) with
| ContractLock (contractHash, n1, _2) -> begin
n1
end))


let __proj__ContractLock__item___2 : outputLock  ->  Prims.unit data = (fun ( projectee  :  outputLock ) -> (match (projectee) with
| ContractLock (contractHash, n1, _2) -> begin
_2
end))


let uu___is_HighVLock : outputLock  ->  Prims.bool = (fun ( projectee  :  outputLock ) -> (match (projectee) with
| HighVLock (_0) -> begin
true
end
| uu____7588 -> begin
false
end))


let __proj__HighVLock__item___0 : outputLock  ->  (lockCore * Prims.nat) = (fun ( projectee  :  outputLock ) -> (match (projectee) with
| HighVLock (_0) -> begin
_0
end))


let __proj__Mkoutput__item__lock : output  ->  outputLock = (fun ( projectee  :  output ) -> (match (projectee) with
| {lock = __fname__lock; spend = __fname__spend} -> begin
__fname__lock
end))


let __proj__Mkoutput__item__spend : output  ->  spend = (fun ( projectee  :  output ) -> (match (projectee) with
| {lock = __fname__lock; spend = __fname__spend} -> begin
__fname__spend
end))


type 'An inputData =
'An data

type transactionSkeleton =
| Tx of Prims.nat * (outpoint, Prims.unit) Zen.Vector.t * Prims.nat * (output, Prims.unit) Zen.Vector.t * Prims.nat * Prims.unit data


let uu___is_Tx : transactionSkeleton  ->  Prims.bool = (fun ( projectee  :  transactionSkeleton ) -> true)


let __proj__Tx__item__l1 : transactionSkeleton  ->  Prims.nat = (fun ( projectee  :  transactionSkeleton ) -> (match (projectee) with
| Tx (l1, outpoints, l2, outputs, l3, data) -> begin
l1
end))


let __proj__Tx__item__outpoints : transactionSkeleton  ->  (outpoint, Prims.unit) Zen.Vector.t = (fun ( projectee  :  transactionSkeleton ) -> (match (projectee) with
| Tx (l1, outpoints, l2, outputs, l3, data) -> begin
outpoints
end))


let __proj__Tx__item__l2 : transactionSkeleton  ->  Prims.nat = (fun ( projectee  :  transactionSkeleton ) -> (match (projectee) with
| Tx (l1, outpoints, l2, outputs, l3, data) -> begin
l2
end))


let __proj__Tx__item__outputs : transactionSkeleton  ->  (output, Prims.unit) Zen.Vector.t = (fun ( projectee  :  transactionSkeleton ) -> (match (projectee) with
| Tx (l1, outpoints, l2, outputs, l3, data) -> begin
outputs
end))


let __proj__Tx__item__l3 : transactionSkeleton  ->  Prims.nat = (fun ( projectee  :  transactionSkeleton ) -> (match (projectee) with
| Tx (l1, outpoints, l2, outputs, l3, data) -> begin
l3
end))


let __proj__Tx__item__data : transactionSkeleton  ->  Prims.unit data = (fun ( projectee  :  transactionSkeleton ) -> (match (projectee) with
| Tx (l1, outpoints, l2, outputs, l3, data) -> begin
data
end))


type utxo =
outpoint  ->  output FStar.Pervasives.Native.option

type inputMsg =
{cmd : opcode; data : (Prims.nat, Prims.unit inputData) Prims.dtuple2; contractHash : hash; utxo : utxo; lastTx : outpoint FStar.Pervasives.Native.option}


let __proj__MkinputMsg__item__cmd : inputMsg  ->  opcode = (fun ( projectee  :  inputMsg ) -> (match (projectee) with
| {cmd = __fname__cmd; data = __fname__data; contractHash = __fname__contractHash; utxo = __fname__utxo; lastTx = __fname__lastTx} -> begin
__fname__cmd
end))


let __proj__MkinputMsg__item__data : inputMsg  ->  (Prims.nat, Prims.unit inputData) Prims.dtuple2 = (fun ( projectee  :  inputMsg ) -> (match (projectee) with
| {cmd = __fname__cmd; data = __fname__data; contractHash = __fname__contractHash; utxo = __fname__utxo; lastTx = __fname__lastTx} -> begin
__fname__data
end))


let __proj__MkinputMsg__item__contractHash : inputMsg  ->  hash = (fun ( projectee  :  inputMsg ) -> (match (projectee) with
| {cmd = __fname__cmd; data = __fname__data; contractHash = __fname__contractHash; utxo = __fname__utxo; lastTx = __fname__lastTx} -> begin
__fname__contractHash
end))


let __proj__MkinputMsg__item__utxo : inputMsg  ->  utxo = (fun ( projectee  :  inputMsg ) -> (match (projectee) with
| {cmd = __fname__cmd; data = __fname__data; contractHash = __fname__contractHash; utxo = __fname__utxo; lastTx = __fname__lastTx} -> begin
__fname__utxo
end))


let __proj__MkinputMsg__item__lastTx : inputMsg  ->  outpoint FStar.Pervasives.Native.option = (fun ( projectee  :  inputMsg ) -> (match (projectee) with
| {cmd = __fname__cmd; data = __fname__data; contractHash = __fname__contractHash; utxo = __fname__utxo; lastTx = __fname__lastTx} -> begin
__fname__lastTx
end))




