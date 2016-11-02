module Consensus.Serialization

open Consensus.Types

open MsgPack
open MsgPack.Serialization
open System.Runtime.Serialization



type OutputLockSerializer(ownerContext) =
    inherit MessagePackSerializer<OutputLock>(ownerContext: SerializationContext)

    override __.PackToCore(packer: Packer, oLock: OutputLock) =
        match oLock with
        | HighVLock (lockCore = lCore) when lCore.version = 0u ->
            // possible code smell; this is really validation logic
            raise <| new SerializationException("Cannot serialize malformed HighVLock")
        | _ -> (new MessagePackObject(lockVersion oLock))
            :: (new MessagePackObject(typeCode oLock))
            :: (lockData oLock) |> packer.PackArray |> ignore

    override __.UnpackFromCore(unpacker: Unpacker) : OutputLock =
        let mutable v = 0ul
        let mutable tC = 0
        if not <| unpacker.IsArrayHeader then
            raise <| new SerializationException("OutputLock is not an array")
        else
            use subtreeReader = unpacker.ReadSubtree()
            if subtreeReader.ItemsCount < 2L then
                raise <| new SerializationException("Too few items in OutputLock")
            elif not <| subtreeReader.ReadUInt32(&v) then
                raise <| new SerializationException("Bad OutputLock version")
            elif not <| subtreeReader.ReadInt32(&tC) then
                raise <| new SerializationException("Bad OutputLock version")
            else
                let lD = if subtreeReader.ItemsCount = 2L then [] else List.ofSeq subtreeReader
                let lCore = {version=v;lockData=lD} : LockCore
                match v, tC with
                    | _, 0 ->
                        CoinbaseLock lCore
                    | _, 1 ->
                        FeeLock lCore
                    | _, 2 ->
                        ContractSacrificeLock lCore
                    | v, tC when (v > 0u) -> 
                        HighVLock (lockCore=lCore,typeCode=tC)
                    | _, 3 when lD.Length <> 1 ->
                        raise <| new SerializationException("PKLock has too many items")
                    | _, 3 ->
                        let hd = lD.Head
                        let isBinary = hd.IsTypeOf<byte[]>()
                        if not <| isBinary.GetValueOrDefault() then
                            raise <| new SerializationException("Bad hash for PKLock")
                        elif (hd.AsBinary()).Length <> PubKeyHashBytes then
                            raise <| new SerializationException("PKLock hash is wrong length")
                        else
                            PKLock (hd.AsBinary())
                    | _, 4 when lD.Length <> 2 ->
                        raise <| new SerializationException("ContractLock has wrong number of items")
                    | _, 4 when not <| lD.Head.IsTypeOf<byte[]>().GetValueOrDefault() ->
                        raise <| new SerializationException("Bad hash for ContractLock")
                    | _, 4 when lD.Head.AsBinary().Length <> ContractHashBytes ->
                        raise <| new SerializationException("ContractLock hash is wrong length")
                    | _, 4 when not <| lD.Item(1).IsTypeOf<byte[]>().GetValueOrDefault() ->
                        raise <| new SerializationException("Bad data for ContractLock")
                    | _, 4 ->
                        ContractLock (lD.Head.AsBinary(), lD.Item(1).AsBinary())
                    | _ ->
                        raise <| new SerializationException("Bad typeCode for version 0 OutputLock")

type SpendSerializer(ownerContext) =
    inherit MessagePackSerializer<Spend>(ownerContext: SerializationContext)

    override __.PackToCore(packer: Packer, {asset=asset; amount=amount}: Spend) =
        packer.PackArrayHeader(2).PackBinary(asset).Pack(amount)
        |> ignore
           
    override __.UnpackFromCore(unpacker: Unpacker) : Spend =
        if not <| unpacker.IsArrayHeader then
            raise <| new SerializationException("Spend is not an array")
        else
            let mutable asset:Hash = Hash.Empty()
            let mutable amount:uint64 = 0uL
            use subtreeReader = unpacker.ReadSubtree()
            if subtreeReader.ItemsCount <> 2L then
                raise <| new SerializationException("Wrong number of items in Spend")
            elif not <| subtreeReader.ReadBinary(&asset) then
                raise <| new SerializationException("Bad asset for Spend")
            elif not <| subtreeReader.ReadUInt64(&amount) then
                raise <| new SerializationException("Bad amount for Spend")
            elif asset.Length <> ContractHashBytes then
                raise <| new SerializationException("Bad asset for Spend")
            else 
                {asset=asset; amount=amount} : Spend

type OutputSerializer(ownerContext) =
    inherit MessagePackSerializer<Output>(ownerContext: SerializationContext)

    override __.PackToCore(packer: Packer, {lock=lock; spend=spend}: Output) =
        //__.OwnerContext.Serializers.Register<OutputLock>(new OutputLockSerializer(__.OwnerContext)) |> ignore
        packer.PackArrayHeader(2).Pack<OutputLock>(lock).Pack<Spend>(spend)
        |> ignore
    
    override __.UnpackFromCore(unpacker: Unpacker) : Output =
        if not <| unpacker.IsArrayHeader then
            raise <| new SerializationException("Spend is not an array")
        else
            use subtreeReader = unpacker.ReadSubtree()
            if subtreeReader.ItemsCount <> 2L then
                raise <| new SerializationException("Wrong number of items in Output")
            else
                let lock = subtreeReader.Unpack<OutputLock>()
                let spend = subtreeReader.Unpack<Spend>()
                {lock=lock; spend=spend} : Output
        

type OutpointSerializer(ownerContext) =
    inherit MessagePackSerializer<Outpoint>(ownerContext: SerializationContext)

    override __.PackToCore(packer: Packer, {txHash=txHash; index=index}: Outpoint) =
        packer.PackArrayHeader(2).PackBinary(txHash).Pack(index)
        |> ignore
    
    override __.UnpackFromCore(unpacker: Unpacker) : Outpoint =
        if not <| unpacker.IsArrayHeader then
            raise <| new SerializationException("Outpoint is not an array")
        else
            use subtreeReader = unpacker.ReadSubtree()
            if subtreeReader.ItemsCount <> 2L then
                raise <| new SerializationException("Wrong number of items in Outpoint")
            else 
                let txHash = subtreeReader.Unpack<Hash>()
                let index = subtreeReader.Unpack<uint32>()
                {txHash=txHash; index=index} : Outpoint

type ContractSerializer(ownerContext) =
    inherit MessagePackSerializer<Contract>(ownerContext: SerializationContext)

    override __.PackToCore(packer: Packer, {code=code;bounds=bounds;hint=hint}: Contract) =
        packer.PackArray([code; bounds; hint])
        |> ignore
    
    override __.UnpackFromCore(unpacker: Unpacker) : Contract =
       if not <| unpacker.IsArrayHeader then
           raise <| new SerializationException("Spend is not an array")
       else
           use subtreeReader = unpacker.ReadSubtree()
           if subtreeReader.ItemsCount <> 3L then
               raise <| new SerializationException("Wrong number of items in Contract")
           else 
               let code = subtreeReader.Unpack<byte[]>()
               let bounds = subtreeReader.Unpack<byte[]>()
               let hint = subtreeReader.Unpack<byte[]>()
               {code=code; bounds=bounds; hint=hint} : Contract

type ExtendedContractSerializer(ownerContext) =
    inherit MessagePackSerializer<ExtendedContract>(ownerContext: SerializationContext)

    override __.PackToCore(packer: Packer, eContract: ExtendedContract) =
        packer.PackArrayHeader(2)
         .Pack(contractVersion eContract) |> ignore
        match eContract with
            | Contract ct -> packer.Pack(ct) |> ignore
            | HighVContract(data=data) -> packer.Pack(data) |> ignore
              
    override __.UnpackFromCore(unpacker: Unpacker) : ExtendedContract =
        if not <| unpacker.IsArrayHeader then
            raise <| new SerializationException("ExtendedContract is not an array")
        use subtreeReader = unpacker.ReadSubtree()
        if subtreeReader.ItemsCount <> 2L then
            raise <| new SerializationException("Wrong number of items in ExtendedContract")
        let v = subtreeReader.Unpack<uint32>()
        match v with
            | 0u ->
                let ct = subtreeReader.Unpack<Contract>()
                Contract ct
            | _ ->
                let d = subtreeReader.Unpack<MessagePackObject>()
                HighVContract(version=v,data=d)

type PKWitnessSerializer(ownerContext) =
    inherit MessagePackSerializer<PKWitness>(ownerContext: SerializationContext)

    override __.PackToCore(packer: Packer, PKWitness pkw: PKWitness) =
        packer.Pack<Witness>(pkw) |> ignore
    
    override __.UnpackFromCore(unpacker: Unpacker) : PKWitness =
        PKWitness <| unpacker.Unpack<Witness>()

type TransactionSerializer(ownerContext) =
    inherit MessagePackSerializer<Transaction>(ownerContext: SerializationContext)
    member this.typeCode:byte = 1uy

    override __.PackToCore(packer: Packer, tx: Transaction) =
        use body = new System.IO.MemoryStream()
        use bodyPacker = Packer.Create(body)
        let bodyLength = match tx.contract with
                         | None -> 4
                         | Some _ -> 5
        bodyPacker.PackArrayHeader(bodyLength)
         .Pack(tx.version)
         .PackArray(tx.inputs)
         .PackArray(tx.witnesses)
         .PackArray(tx.outputs)
         |> (fun bPkr
              -> if tx.contract.IsSome then 
                   bPkr.Pack(tx.contract.Value) |> ignore else ()
            )
        packer.PackExtendedTypeValue(__.typeCode,body.ToArray())
        |> ignore

    
    override __.UnpackFromCore(unpacker: Unpacker) : Transaction =
        let mutable extObj = MessagePackExtendedTypeObject()
        if not <| unpacker.ReadMessagePackExtendedTypeObject(&extObj) then
            printfn "In transaction, cannot read extended type object"
            raise <| new SerializationException("Not an exttype")
        elif extObj.TypeCode <> __.typeCode then
            printfn "In transaction, wrong typecode %i" extObj.TypeCode
            raise <| new SerializationException("Not a transaction")
        use body = new System.IO.MemoryStream(extObj.GetBody())
        use bodyUnpacker = Unpacker.Create(body)
        if not <| bodyUnpacker.IsArrayHeader then
            printfn "In txn, not an arrayheader"
            raise <| new SerializationException("tx body not an array")
        use subtreeReader = bodyUnpacker.ReadSubtree()
        if subtreeReader.ItemsCount <> 4L || 
           subtreeReader.ItemsCount <> 5L then
               printfn "In txn, wrong number items in tx body %i" subtreeReader.ItemsCount
               raise <| new SerializationException "wrong number items in tx body"
        let version = subtreeReader.Unpack<uint32>()
        let inputs = subtreeReader.Unpack<Outpoint list>()
        let witnesses = subtreeReader.Unpack<Witness list>()
        let outputs = subtreeReader.Unpack<Output list>()
        let contract =
            if subtreeReader.ItemsCount = 5L then
                Some <| subtreeReader.Unpack<ExtendedContract>() else
                None
        printfn "Finished unpacking transaction"
        {version=version; inputs=inputs; witnesses=witnesses; outputs=outputs; contract=contract}


type BlockHeaderSerializer(ownerContext) =
    inherit MessagePackSerializer<BlockHeader>(ownerContext: SerializationContext)
    member this.typeCode:byte = 3uy

    override __.PackToCore(packer: Packer, bh: BlockHeader) =
        use body = new System.IO.MemoryStream()
        use bodyPacker = Packer.Create(body)
        bodyPacker.PackArrayHeader(6)
         .Pack<uint32>(bh.version)
         .Pack<Hash>(bh.parent)
         .Pack<Hash list>(merkleData bh)
         .Pack<int64>(bh.timestamp)
         .Pack<uint32>(bh.pdiff)
         .Pack<byte[]>(bh.nonce)
         |> ignore
        packer.PackExtendedTypeValue(__.typeCode,body.ToArray())
        |> ignore
    
    override __.UnpackFromCore(unpacker: Unpacker) : BlockHeader =
        let mutable extObj = MessagePackExtendedTypeObject()
        if not <| unpacker.ReadMessagePackExtendedTypeObject(&extObj) then
            raise <| new SerializationException("Not an exttype")
        elif extObj.TypeCode <> __.typeCode then
            raise <| new SerializationException("Not a block header")
        
        use body = new System.IO.MemoryStream(extObj.GetBody())
        use bodyUnpacker = Unpacker.Create(body)
        if not <| bodyUnpacker.IsArrayHeader then
            raise <| new SerializationException("Block header is not an array")
        use subtreeReader = bodyUnpacker.ReadSubtree()
        if subtreeReader.ItemsCount <> 6L then
            raise <| SerializationException("Wrong number of items in block header")
        let version = subtreeReader.Unpack<uint32>()
        let parent = subtreeReader.Unpack<byte[]>()
//        if not <| subtreeReader.IsArrayHeader then
//            raise <| SerializationException("merkle roots not in array")
        let mData = subtreeReader.Unpack<byte[] list>()
        if mData.Length < 3 then
            raise <| SerializationException("too few merkle roots in array")
        let txMR = mData.Item(0)
        let wMR = mData.Item(1)
        let cMR = mData.Item(2)
        let exData = List.skip 3 mData
        let timestamp = bodyUnpacker.Unpack<int64>()
        let pdiff = bodyUnpacker.Unpack<uint32>()
        let nonce = bodyUnpacker.Unpack<byte[]>()
        {
            version=version;
            parent=parent;
            txMerkleRoot=txMR;
            witnessMerkleRoot=wMR;
            contractMerkleRoot=cMR;
            extraData=exData;
            timestamp=timestamp;
            pdiff=pdiff;
            nonce=nonce
        }

type BlockSerializer(ownerContext) =
    inherit MessagePackSerializer<Block>(ownerContext: SerializationContext)
    member this.typeCode:byte = 2uy

    override __.PackToCore(packer: Packer, blk: Block) =
        use body = new System.IO.MemoryStream()
        use bodyPacker = Packer.Create(body)
        bodyPacker.PackArrayHeader(2)
         .Pack(blk.header)
         .PackArray(blk.transactions)
         |> ignore
        packer.PackExtendedTypeValue(__.typeCode,body.ToArray())
        |> ignore

    override __.UnpackFromCore(unpacker: Unpacker) : Block =
        let mutable extObj = MessagePackExtendedTypeObject()
        if not <| unpacker.ReadMessagePackExtendedTypeObject(&extObj) then
            raise <| new SerializationException("Not an exttype")
        elif extObj.TypeCode <> __.typeCode then
            raise <| new SerializationException("Not a block")
        
        use body = new System.IO.MemoryStream(extObj.GetBody())
        use bodyUnpacker = Unpacker.Create(body)
        if not <| bodyUnpacker.IsArrayHeader then
            raise <| new SerializationException("block is not an array")
        use subtreeReader = bodyUnpacker.ReadSubtree()
        if subtreeReader.ItemsCount <> 2L then
            raise <| new SerializationException("wrong number of items in block")
        let header = subtreeReader.Unpack<BlockHeader>()
        let transactions = subtreeReader.Unpack<Transaction list>()
        {header=header;transactions=transactions}

let context = new SerializationContext()
context.Serializers.RegisterOverride<OutputLock>(new OutputLockSerializer(context))
context.Serializers.RegisterOverride<Spend>(new SpendSerializer(context))
context.Serializers.RegisterOverride<Output>(new OutputSerializer(context))
context.Serializers.RegisterOverride<Outpoint>(new OutpointSerializer(context))
context.Serializers.RegisterOverride<Contract>(new ContractSerializer(context))
context.Serializers.RegisterOverride<ExtendedContract>(new ExtendedContractSerializer(context))
context.Serializers.RegisterOverride<PKWitness>(new PKWitnessSerializer(context))
context.Serializers.RegisterOverride<Transaction>(new TransactionSerializer(context))
context.Serializers.RegisterOverride<BlockHeader>(new BlockHeaderSerializer(context))
context.Serializers.RegisterOverride<Block>(new BlockSerializer(context))

//TODO setup reading the typecode from the serializers
context.ExtTypeCodeMapping.Add("Transaction", 0x01uy) |> ignore
context.ExtTypeCodeMapping.Add("Block", 0x02uy) |> ignore
context.ExtTypeCodeMapping.Add("BlockHeader", 0x03uy) |> ignore

