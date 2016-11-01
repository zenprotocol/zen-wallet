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
            let subtreeReader = unpacker.ReadSubtree()
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
            let subtreeReader = unpacker.ReadSubtree()
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
            let subtreeReader = unpacker.ReadSubtree()
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
            raise <| new SerializationException("Spend is not an array")
        else
            let subtreeReader = unpacker.ReadSubtree()
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
           let subtreeReader = unpacker.ReadSubtree()
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
        let subtreeReader = unpacker.ReadSubtree()
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
    let typeCode:byte = 1uy

    override __.PackToCore(packer: Packer, tx: Transaction) =
        let body = new System.IO.MemoryStream()
        let bodyPacker = Packer.Create(body)
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
        packer.PackExtendedTypeValue(typeCode,body.ToArray())
        |> ignore

    
    override __.UnpackFromCore(unpacker: Unpacker) : Transaction =
        let mutable extObj = MessagePackExtendedTypeObject()
        if not <| unpacker.ReadMessagePackExtendedTypeObject(&extObj) then
            raise <| new SerializationException("Not an exttype")
        elif extObj.TypeCode <> typeCode then
            raise <| new SerializationException("Not a transaction")
        let body = new System.IO.MemoryStream(extObj.GetBody())
        let bodyUnpacker = Unpacker.Create(body)
        if not <| bodyUnpacker.IsArrayHeader then
            raise <| new SerializationException("tx body not an array")
        let subtreeReader = bodyUnpacker.ReadSubtree()
        if subtreeReader.ItemsCount <> 4L || 
           subtreeReader.ItemsCount <> 5L then
               raise <| new SerializationException "wrong number items in tx body"
        let version = subtreeReader.Unpack<uint32>()
        let inputs = subtreeReader.Unpack<Outpoint list>()
        let witnesses = subtreeReader.Unpack<Witness list>()
        let outputs = subtreeReader.Unpack<Output list>()
        let contract =
            if subtreeReader.ItemsCount = 5L then
                Some <| subtreeReader.Unpack<ExtendedContract>() else
                None
        {version=version; inputs=inputs; witnesses=witnesses; outputs=outputs; contract=contract}


type BlockHeaderSerializer(ownerContext) =
    inherit MessagePackSerializer<BlockHeader>(ownerContext: SerializationContext)
    let typeCode:byte = 3uy

    override __.PackToCore(packer: Packer, bh: BlockHeader) =
        failwith "todo" //TODO
    
    override __.UnpackFromCore(unpacker: Unpacker) : BlockHeader =
        failwith "todo" //TODO

type BlockSerializer(ownerContext) =
    inherit MessagePackSerializer<Block>(ownerContext: SerializationContext)

    override __.PackToCore(packer: Packer, blk: Block) =
        failwith "todo" //TODO
    
    override __.UnpackFromCore(unpacker: Unpacker) : Block =
        failwith "todo" //TODO

let context = new SerializationContext()
context.Serializers.RegisterOverride<OutputLock>(new OutputLockSerializer(context))
//context.ExtTypeCodeMapping.Add("Transaction", 0x01uy) |> ignore

//type TransactionSerializer(ownerContext) =
//    inherit MessagePackSerializer<Transaction>(ownerContext: SerializationContext)
//    let typeCode:byte = 0x01uy

//    member __.PackToCore(packer: Packer, tx: Transaction) =
//        // pack body as array of 5 elts
//        let buffer = new System.IO.MemoryStream()
//        let bodyPacker = Packer.Create(buffer)
//        bodyPacker.PackArrayHeader( 5 ) |> ignore
//        bodyPacker.Pack(tx.version) |> ignore
//        bodyPacker.PackArray(tx.inputs) |> ignore
//        bodyPacker.PackArray(tx.outputs) |> ignore
//        bodyPacker.PackArray(tx.witnesses) |> ignore
//        bodyPacker.Pack(tx.contract) |> ignore
//        // tag in ext with typecode
//        packer.PackExtendedTypeValue(typeCode, buffer.ToArray()) |> ignore


//    member __.UnpackFromCore(unpacker: Unpacker) : Transaction =
//        let mutable version = 0ul
//        let extended = unpacker.LastReadData.AsMessagePackExtendedTypeObject()
//        if (extended.TypeCode <> typeCode) then raise (new UnpackException("transaction must be encoded with typecode 0x01"))
//        let bodyStream = new System.IO.MemoryStream(extended.GetBody())
//        let bodyUnpacker = Unpacker.Create(bodyStream)
//        if (not <| bodyUnpacker.Read()) then raise (new UnpackException("transaction cannot be null"))
//        if (not <| bodyUnpacker.IsArrayHeader) then raise <| new UnpackException("transaction currently requires enclosing array")
//        if (UnpackHelpers.GetItemsCount(bodyUnpacker) <> 5) then (raise <| SerializationExceptions.NewUnexpectedArrayLength(5,UnpackHelpers.GetItemsCount(unpacker)))
//        if (not <| bodyUnpacker.ReadUInt32(&version)) then raise <| SerializationExceptions.NewMissingProperty("version")
//        let inputsObj = bodyUnpacker.ReadItemData()
//        let inputs = inputsObj.AsEnumerable() |> Seq.map (fun x -> x.AsList() |> 
//        Transaction
