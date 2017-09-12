

namespace Consensus
  module Utilities = begin
    val inline toBytes :
      n: ^T -> byte [] when  ^T : (static member op_Explicit :  ^T -> uint32)
    val bitsToBytes : bs:bool [] -> byte []
    val bytesToBits : bs:byte [] -> bool []
  end

namespace Consensus
  module Authentication = begin
    val sign : msg:byte [] -> key:byte [] -> byte []
    val verify : signature:byte [] -> msg:byte [] -> key:byte [] -> bool
  end

namespace Consensus
  module Types = begin
    val arraySegtoArray : seg:System.ArraySegment<'a> -> 'a []
    val inline toArray :
      m: ^a -> byte []
        when  ^a : (static member Serializer :  ^a *
                                               Froto.Serialization.ZeroCopyBuffer
                                                 ->
                                                 Froto.Serialization.ZeroCopyBuffer)
    type InnerField =
      | V of Froto.Serialization.Encoding.FieldNum * uint64
      | F32 of Froto.Serialization.Encoding.FieldNum * uint32
      | F64 of Froto.Serialization.Encoding.FieldNum * uint64
      | LD of Froto.Serialization.Encoding.FieldNum * byte []
      with
        member FieldNum : Froto.Serialization.Encoding.FieldNum
        member ToRawField : Froto.Serialization.Encoding.RawField
      end
    val createInnerField :
      _arg1:Froto.Serialization.Encoding.RawField -> InnerField
    val createRawField :
      inner:InnerField -> Froto.Serialization.Encoding.RawField
    type Hash = byte []
    type LockCore =
      {version: uint32;
       lockData: byte [] list;}
    type OutputLock =
      | CoinbaseLock of LockCore
      | FeeLock of LockCore
      | ContractSacrificeLock of LockCore
      | PKLock of pkHash: Hash
      | ContractLock of contractHash: Hash * data: byte []
      | HighVLock of lockCore: LockCore * typeCode: int
    type OutputLockP =
      | FeeLockP
      | PKLockP of Hash
      | ContractSacrificeLockP of Hash option
      | ContractLockP of Hash * byte []
      | OtherLockP of InnerField list
      with
        member Version : uint32
        static member DecodeFixup : m:OutputLockP -> OutputLockP
        static member FoundFields : 'a -> Set<'b> when 'b : comparison
        static member
          RememberFound : m:'c * found:Froto.Serialization.Encoding.FieldNum ->
                            'c
        static member
          Serializer : m:OutputLockP * zcb:Froto.Serialization.ZeroCopyBuffer ->
                         Froto.Serialization.ZeroCopyBuffer
        static member
          UnknownFields : m:OutputLockP ->
                            Froto.Serialization.Encoding.RawField list
        static member
          DecoderRing : Map<int,
                            (OutputLockP ->
                               Froto.Serialization.Encoding.RawField ->
                               OutputLockP)>
        static member Default : OutputLockP
        static member
          RequiredFields : Set<Froto.Serialization.Encoding.FieldNum>
      end
    val lockVersion : _arg1:OutputLock -> uint32
    val typeCode : _arg1:OutputLock -> int
    val lockData : _arg1:OutputLock -> byte [] list
    type Spend =
      {asset: Hash;
       amount: uint64;}
      with
        static member DecodeFixup : m:'c -> 'c
        static member FoundFields : 'a -> Set<'b> when 'b : comparison
        static member
          RememberFound : m:Spend * found:Froto.Serialization.Encoding.FieldNum ->
                            Spend
        static member
          Serializer : m:Spend * zcb:Froto.Serialization.ZeroCopyBuffer ->
                         Froto.Serialization.ZeroCopyBuffer
        static member
          UnknownFields : m:Spend -> Froto.Serialization.Encoding.RawField list
        static member
          DecoderRing : Map<int,
                            (Spend -> Froto.Serialization.Encoding.RawField ->
                               Spend)>
        static member Default : Spend
        static member
          RequiredFields : Set<Froto.Serialization.Encoding.FieldNum>
      end
    type Output =
      {lock: OutputLock;
       spend: Spend;}
    type OutputP =
      {lockP: OutputLockP;
       spendP: Spend;}
      with
        static member DecodeFixup : m:'c -> 'c
        static member FoundFields : 'a -> Set<'b> when 'b : comparison
        static member
          RememberFound : m:OutputP *
                          found:Froto.Serialization.Encoding.FieldNum -> OutputP
        static member
          Serializer : m:OutputP * zcb:Froto.Serialization.ZeroCopyBuffer ->
                         Froto.Serialization.ZeroCopyBuffer
        static member
          UnknownFields : m:OutputP ->
                            Froto.Serialization.Encoding.RawField list
        static member
          DecoderRing : Map<int,
                            (OutputP -> Froto.Serialization.Encoding.RawField ->
                               OutputP)>
        static member Default : OutputP
        static member
          RequiredFields : Set<Froto.Serialization.Encoding.FieldNum>
      end
    type Outpoint =
      {txHash: Hash;
       index: uint32;}
      with
        static member DecodeFixup : m:'c -> 'c
        static member FoundFields : 'a -> Set<'b> when 'b : comparison
        static member
          RememberFound : m:Outpoint *
                          found:Froto.Serialization.Encoding.FieldNum ->
                            Outpoint
        static member
          Serializer : m:Outpoint * zcb:Froto.Serialization.ZeroCopyBuffer ->
                         Froto.Serialization.ZeroCopyBuffer
        static member
          UnknownFields : m:Outpoint ->
                            Froto.Serialization.Encoding.RawField list
        static member
          DecoderRing : Map<int,
                            (Outpoint ->
                               Froto.Serialization.Encoding.RawField -> Outpoint)>
        static member Default : Outpoint
        static member
          RequiredFields : Set<Froto.Serialization.Encoding.FieldNum>
      end
    type Witness = byte []
    type Contract =
      {code: byte [];
       bounds: byte [];
       hint: byte [];}
    type ExtendedContract =
      | Contract of Contract
      | HighVContract of version: uint32 * data: byte []
    val contractVersion : _arg1:ExtendedContract -> uint32
    val ContractHashBytes : int
    val PubKeyHashBytes : int
    val TxHashBytes : int
    type ContractP =
      {version: uint32;
       code: byte [];
       hint: byte [];
       _unknownFields: InnerField list;}
      with
        static member DecodeFixup : m:ContractP -> ContractP
        static member FoundFields : 'a -> Set<'b> when 'b : comparison
        static member
          RememberFound : m:ContractP *
                          found:Froto.Serialization.Encoding.FieldNum ->
                            ContractP
        static member
          Serializer : m:ContractP * zcb:Froto.Serialization.ZeroCopyBuffer ->
                         Froto.Serialization.ZeroCopyBuffer
        static member
          UnknownFields : m:ContractP ->
                            Froto.Serialization.Encoding.RawField list
        static member
          DecoderRing : Map<int,
                            (ContractP ->
                               Froto.Serialization.Encoding.RawField ->
                               ContractP)>
        static member Default : ContractP
        static member
          RequiredFields : Set<Froto.Serialization.Encoding.FieldNum>
      end
    type Transaction =
      {version: uint32;
       inputs: Outpoint list;
       witnesses: Witness list;
       outputs: Output list;
       contract: ExtendedContract option;}
    type TransactionP =
      {version: uint32;
       inputsP: Outpoint list;
       witnessesP: Witness list;
       outputsP: OutputP list;
       contractP: ContractP option;
       _unknownFields: InnerField list;}
      with
        static member DecodeFixup : m:TransactionP -> TransactionP
        static member FoundFields : 'a -> Set<'b> when 'b : comparison
        static member RememberFound : m:'c * 'd -> 'c
        static member
          Serializer : m:TransactionP * zcb:Froto.Serialization.ZeroCopyBuffer ->
                         Froto.Serialization.ZeroCopyBuffer
        static member
          UnknownFields : m:TransactionP ->
                            Froto.Serialization.Encoding.RawField list
        static member
          DecoderRing : Map<int,
                            (TransactionP ->
                               Froto.Serialization.Encoding.RawField ->
                               TransactionP)>
        static member Default : TransactionP
        static member
          RequiredFields : Set<Froto.Serialization.Encoding.FieldNum>
      end
    type Nonce = byte []
    type Commitments =
      {txMerkleRoot: Hash;
       witnessMerkleRoot: Hash;
       contractMerkleRoot: Hash;
       extraCommitments: Hash list;}
      with
        member ToList : Hash list
        static member DecodeFixup : m:Commitments -> Commitments
        static member FoundFields : 'a -> Set<'b> when 'b : comparison
        static member
          RememberFound : m:Commitments *
                          found:Froto.Serialization.Encoding.FieldNum ->
                            Commitments
        static member
          Serializer : m:Commitments * zcb:Froto.Serialization.ZeroCopyBuffer ->
                         Froto.Serialization.ZeroCopyBuffer
        static member
          UnknownFields : m:Commitments ->
                            Froto.Serialization.Encoding.RawField list
        static member
          DecoderRing : Map<int,
                            (Commitments ->
                               Froto.Serialization.Encoding.RawField ->
                               Commitments)>
        static member Default : Commitments
        static member
          RequiredFields : Set<Froto.Serialization.Encoding.FieldNum>
      end
    type BlockHeader =
      {version: uint32;
       parent: Hash;
       blockNumber: uint32;
       txMerkleRoot: Hash;
       witnessMerkleRoot: Hash;
       contractMerkleRoot: Hash;
       extraData: byte [] list;
       timestamp: int64;
       pdiff: uint32;
       nonce: Nonce;}
      with
        member Commitments : Commitments
        static member DecodeFixup : m:'d -> 'd
        static member FoundFields : 'a -> Set<'b> when 'b : comparison
        static member
          RememberFound : m:BlockHeader *
                          found:Froto.Serialization.Encoding.FieldNum ->
                            BlockHeader
        static member
          Serializer : m:BlockHeader * zcb:Froto.Serialization.ZeroCopyBuffer ->
                         Froto.Serialization.ZeroCopyBuffer
        static member
          UnknownFields : m:'c -> Froto.Serialization.Encoding.RawField list
        static member
          DecoderRing : Map<int,
                            (BlockHeader ->
                               Froto.Serialization.Encoding.RawField ->
                               BlockHeader)>
        static member Default : BlockHeader
        static member
          RequiredFields : Set<Froto.Serialization.Encoding.FieldNum>
      end
    type ContractContext =
      {contractId: byte [];
       utxo: Map<Outpoint,Output>;
       tip: BlockHeader;}
    val merkleData : bh:BlockHeader -> Hash list
    type Block =
      {header: BlockHeader;
       transactions: Transaction list;}
    type BlockP =
      {headerP: BlockHeader;
       transactionsP: TransactionP list;}
      with
        static member DecodeFixup : m:BlockP -> BlockP
        static member FoundFields : 'a -> Set<'b> when 'b : comparison
        static member RememberFound : m:'c * 'd -> 'c
        static member
          Serializer : m:BlockP * zcb:Froto.Serialization.ZeroCopyBuffer ->
                         Froto.Serialization.ZeroCopyBuffer
        static member
          UnknownFields : m:Outpoint ->
                            Froto.Serialization.Encoding.RawField list
        static member
          DecoderRing : Map<int,
                            (BlockP -> Froto.Serialization.Encoding.RawField ->
                               BlockP)>
        static member Default : BlockP
        static member RequiredFields : Set<System.IComparable>
      end
  end

namespace Consensus
  module Serialization = begin
    type OutputLockSerializer =
      class
        inherit MsgPack.Serialization.MessagePackSerializer<Types.OutputLock>
        new : ownerContext:MsgPack.Serialization.SerializationContext ->
                OutputLockSerializer
        override
          PackToCore : packer:MsgPack.Packer * oLock:Types.OutputLock -> unit
        override UnpackFromCore : unpacker:MsgPack.Unpacker -> Types.OutputLock
      end
    type SpendSerializer =
      class
        inherit MsgPack.Serialization.MessagePackSerializer<Types.Spend>
        new : ownerContext:MsgPack.Serialization.SerializationContext ->
                SpendSerializer
        override PackToCore : packer:MsgPack.Packer * Types.Spend -> unit
        override UnpackFromCore : unpacker:MsgPack.Unpacker -> Types.Spend
      end
    type OutputSerializer =
      class
        inherit MsgPack.Serialization.MessagePackSerializer<Types.Output>
        new : ownerContext:MsgPack.Serialization.SerializationContext ->
                OutputSerializer
        override PackToCore : packer:MsgPack.Packer * Types.Output -> unit
        override UnpackFromCore : unpacker:MsgPack.Unpacker -> Types.Output
      end
    type OutpointSerializer =
      class
        inherit MsgPack.Serialization.MessagePackSerializer<Types.Outpoint>
        new : ownerContext:MsgPack.Serialization.SerializationContext ->
                OutpointSerializer
        override PackToCore : packer:MsgPack.Packer * Types.Outpoint -> unit
        override UnpackFromCore : unpacker:MsgPack.Unpacker -> Types.Outpoint
      end
    type ContractSerializer =
      class
        inherit MsgPack.Serialization.MessagePackSerializer<Types.Contract>
        new : ownerContext:MsgPack.Serialization.SerializationContext ->
                ContractSerializer
        override PackToCore : packer:MsgPack.Packer * Types.Contract -> unit
        override UnpackFromCore : unpacker:MsgPack.Unpacker -> Types.Contract
      end
    type ExtendedContractSerializer =
      class
        inherit MsgPack.Serialization.MessagePackSerializer<Types.ExtendedContract>
        new : ownerContext:MsgPack.Serialization.SerializationContext ->
                ExtendedContractSerializer
        override
          PackToCore : packer:MsgPack.Packer * eContract:Types.ExtendedContract ->
                         unit
        override
          UnpackFromCore : unpacker:MsgPack.Unpacker -> Types.ExtendedContract
      end
    type TransactionSerializer =
      class
        inherit MsgPack.Serialization.MessagePackSerializer<Types.Transaction>
        new : ownerContext:MsgPack.Serialization.SerializationContext ->
                TransactionSerializer
        override
          PackToCore : packer:MsgPack.Packer * tx:Types.Transaction -> unit
        override UnpackFromCore : unpacker:MsgPack.Unpacker -> Types.Transaction
        member typeCode : byte
      end
    type BlockHeaderSerializer =
      class
        inherit MsgPack.Serialization.MessagePackSerializer<Types.BlockHeader>
        new : ownerContext:MsgPack.Serialization.SerializationContext ->
                BlockHeaderSerializer
        override
          PackToCore : packer:MsgPack.Packer * bh:Types.BlockHeader -> unit
        override UnpackFromCore : unpacker:MsgPack.Unpacker -> Types.BlockHeader
        member typeCode : byte
      end
    type BlockSerializer =
      class
        inherit MsgPack.Serialization.MessagePackSerializer<Types.Block>
        new : ownerContext:MsgPack.Serialization.SerializationContext ->
                BlockSerializer
        override PackToCore : packer:MsgPack.Packer * blk:Types.Block -> unit
        override UnpackFromCore : unpacker:MsgPack.Unpacker -> Types.Block
        member typeCode : byte
      end
    val context : MsgPack.Serialization.SerializationContext
  end

namespace Consensus
  module Tree = begin
    type LazyTree<'LeafData,'BranchData> =
      | Leaf of 'LeafData
      | Branch of
        'BranchData * Lazy<LazyTree<'LeafData,'BranchData>> *
        Lazy<LazyTree<'LeafData,'BranchData>>
    val lazyCata :
      fLeaf:('LeafData -> 'r) ->
        fBranch:('BranchData -> Lazy<'r> -> Lazy<'r> -> 'r) ->
          tree:LazyTree<'LeafData,'BranchData> -> 'r
    val lazyMap :
      fLeaf:('a -> 'b) ->
        fBranch:('c -> 'd) -> (LazyTree<'a,'c> -> LazyTree<'b,'d>)
    type FullTree<'L,'B> =
      | Leaf of 'L
      | Branch of data: 'B * left: FullTree<'L,'B> * right: FullTree<'L,'B>
    val cata :
      fLeaf:('L -> 'a) ->
        fBranch:('B -> 'a -> 'a -> 'a) -> tree:FullTree<'L,'B> -> 'a
    val map :
      fLeaf:('a -> 'b) ->
        fBranch:('c -> 'd) -> (FullTree<'a,'c> -> FullTree<'b,'d>)
    val height : _arg1:FullTree<'a,'b> -> int
    type Loc =
      {height: int32;
       loc: bool [];}
    type LocData<'T> =
      {data: 'T;
       location: Loc;}
    val rightLocation : _arg1:Loc -> Loc
    val leftLocation : _arg1:Loc -> Loc
    val addLocation :
      height:int32 -> tree:FullTree<'a,'b> -> FullTree<LocData<'a>,LocData<'b>>
    val locLocation : _arg1:FullTree<LocData<'L>,LocData<'B>> -> Loc
    val locHeight : _arg1:FullTree<LocData<'L>,LocData<'B>> -> int32
    type OptTree<'L,'B> = FullTree<'L option,'B>
    val complete : s:seq<'T> -> FullTree<LocData<'T option>,LocData<'a option>>
    val normalize<'L,'B> :
      (FullTree<LocData<'L option>,LocData<'B>> ->
         FullTree<LocData<'L option>,LocData<'B>>)
    val liftLocation : f:('a -> 'b) -> _arg1:LocData<'a> -> LocData<'b>
  end

namespace Consensus
  module Merkle = begin
    type Hashable =
      | Transaction of Types.Transaction
      | OutputLock of Types.OutputLock
      | Spend of Types.Spend
      | Outpoint of Types.Outpoint
      | Output of Types.Output
      | Contract of Types.Contract
      | ExtendedContract of Types.ExtendedContract
      | BlockHeader of Types.BlockHeader
      | Block of Types.Block
      | Hash of Types.Hash
    val tag : _arg1:Hashable -> byte []
    val innerHash : bs:byte [] -> byte []
    val innerHashList : bseq:seq<byte []> -> byte []
    val serialize : x:'a -> byte []
    val taggedHash : wrapper:('T -> Hashable) -> ('T -> Types.Hash)
    val transactionHasher : (Types.Transaction -> Types.Hash)
    val outputLockHasher : (Types.OutputLock -> Types.Hash)
    val spendHasher : (Types.Spend -> Types.Hash)
    val outpointHasher : (Types.Outpoint -> Types.Hash)
    val outputHasher : (Types.Output -> Types.Hash)
    val contractHasher : (Types.Contract -> Types.Hash)
    val extendedContractHasher : (Types.ExtendedContract -> Types.Hash)
    val blockHeaderHasher : (Types.BlockHeader -> Types.Hash)
    val blockHasher : block:Types.Block -> Types.Hash
    val hashHasher : (Types.Hash -> Types.Hash)
    val defaultHash : cTW:byte [] -> (int -> byte [])
    val inline toBytes :
      n: ^T -> byte [] when  ^T : (static member op_Explicit :  ^T -> uint32)
    val bitsToBytes : bs:bool [] -> byte []
    val bytesToBits : bs:byte [] -> bool []
    val merkleRoot :
      cTW:byte [] -> hasher:('a -> byte []) -> items:'a list -> byte []
  end

namespace Consensus
  module SparseMerkleTree = begin
    val TSize : int
    val BSize : int
    type BLocation =
      {h: int;
       b: byte [];}
    val baseLocation : BLocation
    val zeroLoc' : n:int -> bool []
    val zeroLoc : bool []
    val splitindex : h:int -> byte []
    val bitAt : h:int -> k:byte [] -> bool
    val left : BLocation -> BLocation
    val right : BLocation -> BLocation
    val indexOf : n:int -> int * int
    type SMT<'V> =
      {cTW: byte [];
       mutable kv: Map<Types.Hash,'V>;
       mutable digests: Map<BLocation,Types.Hash>;
       defaultDigests: int -> Types.Hash;
       serializer: 'V -> byte [];}
    val emptySMT : cTW:byte [] -> serializer:('V -> byte []) -> SMT<'V>
    val splitlist :
      kvl:('a * 'b) list -> s:'a -> ('a * 'b) list * ('a * 'b) list
        when 'a : comparison
    val splitmap :
      kv:Map<'a,'b> -> s:'a -> Map<'a,'b> * Map<'a,'b> when 'a : comparison
    val lHash : cTW:byte [] -> b:byte [] -> sv:byte [] -> byte []
    val optLeafHash :
      cTW:byte [] -> b:byte [] -> (byte [] option -> byte [] option)
    val iHash : dl:byte [] -> dr:byte [] -> BLocation -> byte []
    val optInnerHash :
      defaultDigests:(int -> byte []) ->
        dl:byte [] option -> dr:byte [] option -> BLocation -> byte [] option
    val digestOpt :
      cTW:byte [] ->
        kv:Map<Types.Hash,Types.Hash> ->
          digests:Map<BLocation,Types.Hash> ->
            ddigests:(int -> Types.Hash) -> BLocation -> Types.Hash option
    val digestSMTOpt : SMT<'a> -> location:BLocation -> Types.Hash option
    val fromOpt :
      f:(SMT<'V> -> BLocation -> Types.Hash option) ->
        smt:SMT<'V> -> location:BLocation -> Types.Hash
    val digestSMT<'V> : (SMT<'V> -> BLocation -> Types.Hash)
    val optCache :
      digests:byref<Map<BLocation,byte []>> ->
        defaultDigests:(int -> byte []) ->
          location:BLocation ->
            lroot:byte [] option -> rroot:byte [] option -> byte [] option
    val optUpdate :
      cTW:byte [] ->
        splitkv:Map<Types.Hash,Types.Hash> ->
          digests:byref<Map<BLocation,Types.Hash>> ->
            ddigests:(int -> Types.Hash) ->
              BLocation ->
                kvs:Map<Types.Hash,Types.Hash option> -> byte [] option
    val optUpdateSMT :
      SMT<Types.Hash> ->
        location:BLocation ->
          kvs:Map<Types.Hash,Types.Hash option> -> byte [] option
    val updateSMT :
      smt:SMT<Types.Hash> ->
        location:BLocation ->
          kvs:Map<Types.Hash,Types.Hash option> -> Types.Hash
    val optFindRoot :
      cTW:byte [] ->
        defaultDigests:(int -> byte []) ->
          path:Types.Hash option list ->
            k:byte [] ->
              v:byte [] option -> location:BLocation -> byte [] option
    val optAudit :
      cTW:byte [] ->
        kv:Map<byte [],Types.Hash> ->
          digests:Map<BLocation,Types.Hash> ->
            ddigests:(int -> Types.Hash) ->
              location:BLocation -> key:byte [] -> Types.Hash option list
    val optAuditSMT : smt:SMT<'a> -> key:byte [] -> Types.Hash option list
  end

namespace Consensus
  module ChainParameters = begin
    [<MeasureAttribute ()>]
    type zen
    [<MeasureAttribute ()>]
    type kalapa
    [<LiteralAttribute ()>]
    val MaxZen : int64<zen>
    [<LiteralAttribute ()>]
    val KalapasPerZen : int64<kalapa/zen>
    val MaxKalapas : int64<kalapa>
    val TotalMinerRewardZen : int64<zen>
    val TotalMinerReward : int64<kalapa>
    [<MeasureAttribute ()>]
    type sec
    [<MeasureAttribute ()>]
    type year
    val secondsPerYear : float<sec/year>
    val blockInterval : float<sec>
    val initialRewardZen : int64<zen>
    val initialReward : int64<kalapa>
    val blocksPerHalvingPeriod : uint32
    val halvingPeriod : float<sec>
    val totalRewardUpToPeriod : n:uint32 -> int64<kalapa>
    val totalRewardInPeriod : n:uint32 -> int64<kalapa>
    val totalRewardL : int64<kalapa> list
    val perBlockRewardL : int64<kalapa> list
    val rewardPerBlockInPeriod : n:uint32 -> int64<kalapa>
    val periodOfBlock : n:uint32 -> uint32
    val rewardInBlock : n:uint32 -> int64<kalapa>
  end

namespace Consensus
  module TransactionValidation = begin
    val MaxTransactionSize : int
    val zhash : byte []
    val MaxKalapa : uint64
    val toOpt : f:('a -> 'b) -> x:'a -> 'b option
    val isCanonical<'V> : bytearray:byte [] -> bool
    val guardedDeserialise : s:byte [] -> 'V
    type PointedInput = Types.Outpoint * Types.Output
    type PointedTransaction =
      {version: uint32;
       pInputs: PointedInput list;
       witnesses: Types.Witness list;
       outputs: Types.Output list;
       contract: Types.ExtendedContract option;}
    val toPointedTransaction :
      tx:Types.Transaction -> inputs:Types.Output list -> PointedTransaction
    val unpoint : PointedTransaction -> Types.Transaction
    type SigHashOutputType =
      | SigHashAll
      | SigHashNone
      | SigHashSingle
      with
        member Byte : byte
      end
    type SigHashInputType =
      | SigHashOneCanPay
      | SigHashAnyoneCanPay
      with
        member Byte : byte
      end
    type SigHashType =
      | SigHashType of
        inputType: SigHashInputType * outputType: SigHashOutputType
      with
        member Byte : byte
        static member Make : hbyte:byte -> SigHashType
      end
    type PKWitness =
      | PKWitness of
        publicKey: byte [] * edSignature: byte [] * hashtype: SigHashType
      with
        member toWitness : Types.Witness
        static member TryMake : wit:Types.Witness -> PKWitness option
      end
    val reducedTx :
      tx:Types.Transaction -> index:int -> SigHashType -> Types.Transaction
    val txDigest :
      tx:Types.Transaction -> index:int -> hashtype:SigHashType -> Types.Hash
    val goodOutputVersions : PointedTransaction -> bool
    val validatePKLockAtIndex :
      ptx:PointedTransaction -> index:int -> pkHash:byte [] -> bool
    val signatureAtIndex :
      tx:Types.Transaction ->
        index:int -> hashtype:SigHashType -> privkey:byte [] -> byte []
    val pkWitnessAtIndex :
      tx:Types.Transaction ->
        index:int -> hashtype:SigHashType -> privkey:byte [] -> PKWitness
    val validateAtIndex : ptx:PointedTransaction -> index:int -> bool
    val signTx :
      tx:Types.Transaction -> outputkeys:byte [] list -> Types.Transaction
    val spendMap : outputs:seq<Types.Output> -> Map<Types.Hash,uint64>
    val checkUserTransactionAmounts : ptx:PointedTransaction -> bool
    val checkAutoTransactionAmounts :
      ptx:PointedTransaction -> contract:Types.Hash -> bool
    val checkCoinbaseTransactionAmounts :
      ptx:PointedTransaction -> claimable:Map<Types.Hash,uint64> -> bool
    val validateUserTx : ptx:PointedTransaction -> bool
    type ContractFunctionInput =
      byte [] * Types.Hash * (Types.Outpoint -> Types.Output option)
    type TransactionSkeleton = Types.Outpoint list * Types.Output list * byte []
    type ContractFunction = ContractFunctionInput -> TransactionSkeleton
    val validateAutoTx :
      ptx:PointedTransaction ->
        utxos:(Types.Outpoint -> Types.Output option) ->
          contracts:(Types.Hash -> ContractFunction option) -> bool
    val isUserTx : ptx:PointedTransaction -> bool
    val isAutoTx : ptx:PointedTransaction -> bool
    val validateNonCoinbaseTx :
      ptx:PointedTransaction ->
        utxos:(Types.Outpoint -> Types.Output option) ->
          contracts:(Types.Hash -> ContractFunction option) -> bool
    val validateCoinbaseTx :
      ptx:PointedTransaction ->
        claimable:Map<Types.Hash,uint64> -> blocknumber:uint32 -> bool
  end

namespace Consensus
  module BlockValidation = begin
    val blocksPerUpdatePeriod : int
    val expectedSecs : float<ChainParameters.sec>
    val secsToTimespan : s:float<ChainParameters.sec> -> System.TimeSpan
    val expectedTimeSpan : System.TimeSpan
    val bigIntToBytes : bidiff:bigint -> byte []
    val bytesToBigInt : diff:byte [] -> System.Numerics.BigInteger
    val compressedToBigInt : pdiff:uint32 -> System.Numerics.BigInteger
    val compressDifficulty : bigdiff:System.Numerics.BigInteger -> uint32
    val target : bigdiff:System.Numerics.BigInteger -> byte []
    type Difficulty =
      {compressed: uint32;
       uncompressed: byte [];
       big: bigint;
       target: byte [];}
      with
        static member create : compressed:uint32 -> Difficulty
        static member create : ucmp:byte [] -> Difficulty
        static member create : big:bigint -> Difficulty
      end
    val nextDifficulty : Difficulty -> timeDelta:System.TimeSpan -> Difficulty
    val checkHeader : header:Types.BlockHeader -> bool
    val totalWork : oldTotal:double -> currentDiff:uint32 -> double
    val checkPOW :
      header:Types.BlockHeader -> consensusDifficulty:Difficulty -> bool
    val transactionMR : cTW:byte [] -> txs:Types.Transaction list -> byte []
    val checkTransactionMerkleRoot : block:Types.Block -> bool
    val checkWitnessMerkleRoot : block:'a -> bool
    val checkContractMerkleRoot : block:'a -> bool
    type BlockContext =
      {difficulty: Difficulty;}
    val coinbaseClaimable :
      block:Types.Block ->
        contractSacs:Map<Types.Hash,uint64> -> Map<Types.Hash,uint64>
    val validateBlock : blockcontext:BlockContext -> block:Types.Block -> bool
  end

namespace Consensus
  module Tests = begin
    val zhash : byte []
    val randomhash : byte []
    val randomhash' : byte []
    val pklock : Types.OutputLock
    [<NUnit.Framework.Test ()>]
    val ( PKLock has lockVersion 0u ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Hashes have the right length (32 bytes) ) : unit -> unit
    val minlockcore : Types.LockCore
    [<NUnit.Framework.Test ()>]
    val ( Minimal lockcore has LockCore type ) : unit -> unit
    val randomdata : byte []
    val randomtxhash : byte []
    val randomtxhash' : byte []
    val cbaselock : Types.OutputLock
    val feelock : Types.OutputLock
    val ctlock : Types.OutputLock
    [<NUnit.Framework.Test ()>]
    val ( CoinbaseLock minlockcore has version 0u ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( FeeLock minlockcore has version 0u ) : unit -> unit
    val zspend : Types.Spend
    [<NUnit.Framework.Test ()>]
    val ( zspend has type spend ) : unit -> unit
    val pkoutput : Types.Output
    val ctoutput : Types.Output
    val feeoutput : Types.Output
    val randomoutpoint : Types.Outpoint
    val pkwit : byte []
    val ctr : Types.Contract
    val extContract : Types.ExtendedContract
    val ts : System.DateTimeOffset
    val unixts : int64
    val difficultymantissa : byte
    val difficultyexponent : uint32
    val minpdiff : uint32
    [<NUnit.Framework.Test ()>]
    val ( Minimum pdiff is 2^24 = 0x01000000 = 16777216u ) : unit -> unit
    val tx : Types.Transaction
    val bheader : Types.BlockHeader
    val blk : Types.Block
    [<NUnit.Framework.Test ()>]
    val ( OutputLock serializes CoinbaseLock ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( OutputLock round trip of CoinbaseLock produces same object ) :
      unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( OutputLock round trip of FeeLock produces same object ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( OutputLock round trip of PKLock produces same object ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( OutputLock round trip of ContractLock produces same object ) :
      unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Spend round trip produces same object ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Output round trip of PK output produces same object ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Output round trip of contract locked output produces same object ) :
      unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Output round trip of feelocked output produces same object ) :
      unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Outpoint round trip produces same object ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Contract round trip produces same object ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( ExtendedContract round trip of version 0 contract produces same object )
          : unit -> unit
    val highVContract : Types.ExtendedContract
    [<NUnit.Framework.Test ()>]
    val ( ExtendedContract round trip of HighVContract produces same object ) :
      unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Witness round trip produces same object ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Transaction round trip of transaction with contract produces same object )
          : unit -> unit
    val txwithoutcontract : Types.Transaction
    [<NUnit.Framework.Test ()>]
    val ( Transaction round trip of transaction without contract produces same object )
          : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( BlockHeader round trip without extraData produces same object ) :
      unit -> unit
    val extraMRs : byte [] list
    val bheaderwithextradata : Types.BlockHeader
    [<NUnit.Framework.Test ()>]
    val ( BlockHeader round trip with extraData produces same object ) :
      unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Block round trip produces same object ) : unit -> unit
  end

namespace Consensus
  module MerkleTests = begin
    val sha3 : Org.BouncyCastle.Crypto.Digests.Sha3Digest
    type Hex = Org.BouncyCastle.Utilities.Encoders.Hex
    [<NUnit.Framework.Test ()>]
    val ( SHA3 hash of null string matches known value ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Print tree of [1..14] ) : unit -> unit
  end

namespace Consensus
  module SparseMerkleTreeTests = begin
    val testSize : int
    val testSerializer : (byte [] -> byte [])
    val treeconst : byte []
    val testSMT : SparseMerkleTree.SMT<byte []>
    val firstKey : byte []
    val firstValue : byte []
    val defLocation : Tree.Loc
    [<NUnit.Framework.Test ()>]
    val ( Empty SMT has no keys ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Empty SMT has default digest ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Empty SMT has default digest for given height ) : unit -> unit
    val smtOne : SparseMerkleTree.SMT<byte []>
    val toInsert : Map<byte [],byte [] option>
    val oneDigest : byte [] option
    [<NUnit.Framework.Test ()>]
    val ( Inserting one key gives non-default digest ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( One item SMT has one key ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Removing item from SMT removes key and changes digest ) : unit -> unit
    val randomKeys : size:int -> seq<byte []>
    [<NUnit.Framework.Test ()>]
    val ( Adding 5 items results in 5 keys ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Adding 1000 items results in 1000 keys ) : unit -> unit
    val smt : SparseMerkleTree.SMT<byte []>
    val aval : byte []
    val toInsertSeq : seq<byte [] * byte [] option>
    val toInserta : Map<byte [],byte [] option>
    val d : byte [] option
    val aKey : Types.Hash
    [<NUnit.Framework.Test ()>]
    val ( Audit path generated for existing key ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Audit path for existing key verifies against SMT ) : unit -> unit
  end

namespace Consensus
  module TransactionTests = begin
    [<NUnit.Framework.Test ()>]
    val ( Signed transaction validates ) : unit -> unit
    val kp : Sodium.KeyPair
    [<NUnit.Framework.Test ()>]
    val ( Sodium works ) : unit -> unit
  end

namespace Consensus
  module ExternalTypes = begin
    type hash = Types.Hash
    type lockCore = Types.LockCore
    type outputLock = Types.OutputLock
    type spend = Types.Spend
    type output = Types.Output
    type outpoint = Types.Outpoint
    type witness = Types.Witness
    type contract = Types.Contract
    type extendedContract = Types.ExtendedContract
    type transaction = Types.Transaction
    type nonce = Types.Nonce
    type blockHeader = Types.BlockHeader
    type contractContext = Types.ContractContext
    type block = Types.Block
  end

namespace Consensus
  module SerializationTests = begin
    val filteredInnerGen : toFilter:Set<int32> -> FsCheck.Gen<Types.InnerField>
    val filteredInnerShrink :
      toFilter:Set<int32> -> (Types.InnerField -> seq<Types.InnerField>)
    val filteredInnerListShrink :
      toFilter:Set<int32> ->
        l:Types.InnerField list -> seq<Types.InnerField list>
    type ArbitraryModifiers =
      class
        static member
          ArrSeg : unit -> FsCheck.Arbitrary<System.ArraySegment<byte>>
        static member Contract : unit -> FsCheck.Arbitrary<Types.ContractP>
        static member OutputLock : unit -> FsCheck.Arbitrary<Types.OutputLockP>
        static member
          Transaction : unit -> FsCheck.Arbitrary<Types.TransactionP>
      end
    [<NUnit.Framework.OneTimeSetUp ()>]
    val setup : unit -> unit
    [<FsCheck.NUnit.PropertyAttribute ()>]
    val ( Outpoint round-trips ) : p:Types.Outpoint -> bool
    [<FsCheck.NUnit.PropertyAttribute ()>]
    val ( Spend round-trips ) : s:Types.Spend -> bool
    [<FsCheck.NUnit.PropertyAttribute ()>]
    val ( Output lock round-trips ) : l:Types.OutputLockP -> FsCheck.Property
    [<FsCheck.NUnit.PropertyAttribute ()>]
    val ( Output round-trips ) : l:Types.OutputP -> FsCheck.Property
    [<FsCheck.NUnit.PropertyAttribute ()>]
    val ( Contract round-trips ) : cn:Types.ContractP -> FsCheck.Property
    [<FsCheck.NUnit.PropertyAttribute ()>]
    val ( Transaction round-trips ) : t:Types.TransactionP -> FsCheck.Property
  end

namespace Consensus
  module TemporarySerializationTests = begin
    val filteredInnerGen : toFilter:Set<int32> -> FsCheck.Gen<Types.InnerField>
    val filteredInnerShrink :
      toFilter:Set<int32> -> (Types.InnerField -> seq<Types.InnerField>)
    val filteredInnerListShrink :
      toFilter:Set<int32> ->
        l:Types.InnerField list -> seq<Types.InnerField list>
    type ArbitraryModifiers =
      class
        static member
          ArrSeg : unit -> FsCheck.Arbitrary<System.ArraySegment<byte>>
        static member Contract : unit -> FsCheck.Arbitrary<Types.ContractP>
        static member OutputLock : unit -> FsCheck.Arbitrary<Types.OutputLockP>
        static member
          Transaction : unit -> FsCheck.Arbitrary<Types.TransactionP>
      end
    [<NUnit.Framework.Test ()>]
    val ( Outpoint round-trips ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Spend round-trips ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Output lock round-trips ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Output round-trips ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Contract round-trips ) : unit -> unit
    [<NUnit.Framework.Test ()>]
    val ( Transaction round-trips ) : unit -> unit
  end



