module ContractExamples.FStarCompatibility

open ContractExamples.Execution
open Zen.Types.Extracted
open FStar.Pervasives
open Zen

type ContractResult = Result<TransactionSkeleton, string>

let private unCost (Zen.Cost.Realized.C inj:Zen.Cost.Realized.cost<'Aa, 'An>) : 'Aa = inj.Force()

let vectorToList (z:Vector.t<'Aa, _>) : List<'Aa> =
     // 0I's are eraseable
     Vector.foldl 0I 0I (fun acc e -> Zen.Cost.Realized.ret (e::acc)) [] z 
     |> unCost
     |> List.rev

let listToVector (ls:List<'Aa>) : Vector.t<'Aa, _> =
    let len = List.length ls 
    let lsIndexed = List.mapi (fun i elem -> bigint (len - i - 1), elem) ls // vertors are reverse-zero-indexed
    List.foldBack (fun (i,x) acc -> Vector.VCons (i, x, acc)) lsIndexed Vector.VNil

let private fstToFsOutpoint (a:outpoint) : Consensus.Types.Outpoint =
    { txHash = a.txHash; index = a.index }

let fsToFstOutpoint (a:Consensus.Types.Outpoint) : outpoint =
    { txHash = a.txHash; index = a.index }

open Consensus.Serialization

let private getDataPoints (data : data<unit>) : bigint =
    match data with 
    | Empty -> 0I
    | Bool _ -> 1I
    | Byte _ -> 1I
    | Hash _ -> 1I
    | Key _ -> 1I
    | Outpoint _ -> 1I
    | Output _ -> 1I
    | OutputLock _ -> 1I
    | Sig _ -> 1I
    | UInt8 _ -> 1I
    | UInt32 _ -> 1I
    | UInt64 _ -> 1I
    | Data2 (n1, n2, _, _) -> (n1 + n2)
    | Data3 (n1, n2, n3, _, _, _) -> (n1 + n2 + n3)
    | OutpointVector (l, _) -> l
    | UInt64Vector (l, _) -> l
    | ByteArray (l, _) -> l
    | _ -> 
        //TODO: complete cases
        data.ToString()
        |> System.NotImplementedException
        |> raise

let private fsToFstLock (outputLock:Consensus.Types.OutputLock) : outputLock =
    match outputLock with 
    | Consensus.Types.PKLock (pkHash) ->
        PKLock pkHash
    | Consensus.Types.ContractLock (pkHash, null) ->
        ContractLock (pkHash, 0I, Empty)
    | Consensus.Types.ContractLock (pkHash, [||]) ->
        ContractLock (pkHash, 0I, Empty)
    | Consensus.Types.ContractLock (pkHash, bytes) ->
        let serializer = context.GetSerializer<data<unit>>()
        let data = serializer.UnpackSingleObject bytes
        ContractLock (pkHash, getDataPoints data, data)
    | _ ->
        //TODO
        outputLock.ToString()
        |> System.NotImplementedException
        |> raise

let private fstToFsLock (outputLock:outputLock) : Consensus.Types.OutputLock =
    match outputLock with 
    | PKLock (pkHash) ->
        Consensus.Types.PKLock pkHash
    | ContractLock (pkHash, _, data) ->
        let serializer = context.GetSerializer<data<unit>>()
        Consensus.Types.ContractLock (pkHash, serializer.PackSingleObject data)
    | _ ->
        //TODO
        outputLock.ToString()
        |> System.NotImplementedException
        |> raise

let private fsToFstOutput (output:Option<Consensus.Types.Output>) : Native.option<output> =
    match output with
    | None -> 
        Native.option.None
    | Some output -> 
        Native.option.Some { lock = fsToFstLock output.lock; spend = { asset = output.spend.asset; amount = output.spend.amount}}

let private fstToFsOutput (output:output) : Consensus.Types.Output =
    { lock = fstToFsLock output.lock; spend = { asset = output.spend.asset; amount = output.spend.amount}}

let private convertUtxo (utxo: Utxo) : utxo = fstToFsOutpoint >> utxo >> fsToFstOutput

open MsgPack
open MsgPack.Serialization
open System.Runtime.Serialization

type DataSerializer(ownerContext) =
    inherit MessagePackSerializer<data<unit>>(ownerContext: SerializationContext)

    override __.PackToCore (p: Packer, data: data<unit>) =
        match data with
        | Bool v -> p.Pack(0).Pack(System.Convert.ToByte v) 
        | Byte v -> p.Pack(1).Pack(v) 
        | Empty -> p.Pack(2)
        | Hash v -> p.Pack(3).PackBinary(v)
        | UInt64 v -> p.Pack(4).Pack(v)
        | Outpoint { txHash = txHash; index = index} -> 
            p.Pack(5).PackBinary(txHash).Pack(index)
        | Output v -> 
            //TODO: reafactor
            let serializer = context.GetSerializer<Consensus.Types.Output>()
            p.Pack(6).PackBinary(serializer.PackSingleObject(fstToFsOutput v))
        | Data2 (l1, l2, d1, d2) -> 
            //TODO: reafactor
            let serializer = context.GetSerializer<data<unit>>()
            p.Pack(7).Pack(l1).Pack(l2).PackBinary(serializer.PackSingleObject(d1)).PackBinary(serializer.PackSingleObject(d2))
        | Optional (l, Native.option.Some d) ->
            //TODO: reafactor
            let serializer = context.GetSerializer<data<unit>>()
            p.Pack(8).Pack(l).PackBinary(serializer.PackSingleObject(d))
        | Optional (l, Native.option.None) ->
            p.Pack(9).Pack(l)
        | ByteArray (n, bytes) ->
            p.Pack(10).PackBinary(bytes)
        | UInt32 (v) ->
            p.Pack(11).Pack(v)
        | UInt64Vector (l, v) ->
            let list = vectorToList v
            p.Pack(12).Pack(l).Pack<List<uint64>>(list)
        | UInt32Vector (l, v) ->
            let list = vectorToList v
            p.Pack(13).Pack(l).Pack<List<uint32>>(list)
        | OutpointVector (l, v) ->
            let list = vectorToList v
            p.Pack(14).Pack(l).Pack<List<outpoint>>(list)
        | OutputLock (PKLock bytes) ->
            p.Pack(15).PackBinary(bytes)
        | _ -> 
            //TODO: complete cases
            data.ToString()
            |> System.NotImplementedException
            |> raise
        |> ignore
    override __.UnpackFromCore (p: Unpacker) =
        let code = p.Unpack()
        p.Read() |> ignore
        match code with
        | 0 -> Bool (System.Convert.ToBoolean (p.Unpack<byte>()))
        | 1 -> Byte (p.Unpack())
        | 2 -> Empty
        | 3 -> Hash (p.Unpack())
        | 4 -> UInt64 (p.Unpack())
        | 5 -> 
            let txHash = p.Unpack()
            p.Read() |> ignore
            let index = p.Unpack()
            Outpoint { txHash = txHash; index = index} 
        | 6 -> 
            //TODO: reafactor
            let serializer = context.GetSerializer<Consensus.Types.Output>()
            let bytes = p.Unpack()
            let output = serializer.UnpackSingleObject bytes
            match fsToFstOutput (Some (output)) with
            | Native.option.Some o -> Output o
            | Native.option.None -> 
                "Error unpacking output"
                |> SerializationException
                |> raise
        | 7 ->
            //TODO: reafactor
            let serializer = context.GetSerializer<data<unit>>()
            let l1 = p.Unpack()
            p.Read() |> ignore
            let l2 = p.Unpack()
            p.Read() |> ignore
            let d1 = serializer.UnpackSingleObject(p.Unpack())
            p.Read() |> ignore
            let d2 = serializer.UnpackSingleObject(p.Unpack())
            p.Read() |> ignore
            Data2 (l1, l2, d1, d2)
        | 8 ->
            //TODO: reafactor
            let serializer = context.GetSerializer<data<unit>>()
            let l = p.Unpack()
            p.Read() |> ignore
            let d = serializer.UnpackSingleObject(p.Unpack())
            p.Read() |> ignore
            Optional (l, Native.option.Some d)
        | 9 ->
            let serializer = context.GetSerializer<data<unit>>()
            let l = p.Unpack()
            p.Read() |> ignore
            Optional (l, Native.option.None)
        | 10 ->
            let serializer = context.GetSerializer<data<unit>>()
            let l = p.Unpack()
            p.Read() |> ignore
            let bytes = p.Unpack()
            p.Read() |> ignore
            ByteArray (l, bytes)
        | 11 ->
            let v = p.Unpack()
            p.Read() |> ignore
            UInt32 v
        | 12 ->
            let len = p.Unpack()
            p.Read() |> ignore
            let lst = p.Unpack()
            let vec = listToVector lst
            p.Read() |> ignore
            UInt64Vector (len, vec)
        | 13 ->
            let len = p.Unpack()
            p.Read() |> ignore
            let lst = p.Unpack()
            let vec = listToVector lst
            p.Read() |> ignore
            UInt32Vector (len, vec)
        | 14 ->
            let len = p.Unpack()
            p.Read() |> ignore
            let lst = p.Unpack()
            let vec = listToVector lst
            p.Read() |> ignore
            OutpointVector (len, vec)
        | 15 ->
            let bytes = p.Unpack()
            p.Read() |> ignore
            OutputLock (PKLock bytes)
        | _ -> 
            "Unwnown code encountered while unpacking data: " + code.ToString()
            |> SerializationException
            |> raise

context.Serializers.RegisterOverride<data<unit>>(new DataSerializer(context))

let convertInput : ContractFunctionInput -> inputMsg = 
    function (message, contractHash, utxo) -> 
        let unpacked = context.GetSerializer<data<unit>>().UnpackSingleObject message.[1..]
        {
            cmd = message.[0];
            data = Prims.Mkdtuple2 (getDataPoints unpacked, unpacked);
            contractHash = contractHash;
            utxo = convertUtxo utxo
        }

let private convertResult (txSkeleton:result<transactionSkeleton>) : ContractResult =
  match txSkeleton with 
  | result.E exp -> //TODO
      ""
      |> System.NotImplementedException
      |> raise
  | result.Err msg -> Error msg
  | result.V txSkeleton -> 
    match txSkeleton with 
    | Tx (_, outpoints, _, outputs, _) -> 
        let convertList f list = List.map f (vectorToList list)
        Ok (convertList fstToFsOutpoint outpoints, convertList fstToFsOutput outputs, [||])

let convertContractFunction = function 
    | MainFunc (CostFunc (_, cf), mf) -> 
        convertInput >> mf >> unCost >> convertResult,
        convertInput >> cf >> unCost// function 
       // | Cost.Realized.C (Lazy i) -> i


open NUnit.Framework

[<Test>]
let ``Vector round trip convertions should end up with original value``() =
    let arr = [1;2;3]
    let vec = arr |> listToVector |> vectorToList
    Assert.AreEqual (vec, arr)

[<Test>]
let ``Should convert outpoint``() =
    let randomHash = Array.map (fun x -> x*x) [|10uy..41uy|]
    let index = 100u
    let fstRandomHash = randomHash
    let fsOutpoint = fstToFsOutpoint { txHash = fstRandomHash; index = index }
    Assert.AreEqual (fsOutpoint.txHash, randomHash)
    Assert.AreEqual (fsOutpoint.index, index)
   

let len : array<'A> -> bigint =
    function a -> bigint (Array.length a)

let fstarMockFunction (input:inputMsg) : transactionSkeleton =  
  let data = input.contractHash
  let outpoints = [{ txHash = data; index = 0u }]
  let outputs = [{ lock = PKLock data; spend = {asset = data; amount = 0UL } }]
  Tx (bigint (List.length outpoints), listToVector outpoints, bigint (List.length outputs), (listToVector outputs), Native.option.None)

//TODO: cover tests

let serializer = context.GetSerializer<data<unit>>()

[<Test>]
let ``Serialization of Bool``() =
    let value:data<unit> = Bool true
    let valueSerialized = serializer.PackSingleObject(value)
    let valueDeserialized = serializer.UnpackSingleObject(valueSerialized)
    Assert.AreEqual (value, valueDeserialized)

[<Test>]
let ``Serialization of Hash``() =
    let hash = Array.map (fun x -> x*x) [|10uy..41uy|]
    let value:data<unit> = Hash hash
    let valueSerialized = serializer.PackSingleObject(value)
    let valueDeserialized = serializer.UnpackSingleObject(valueSerialized)
    Assert.AreEqual (value, valueDeserialized)

[<Test>]
let ``Serialization of Optional Hash``() =
    let hash = Array.map (fun x -> x*x) [|10uy..41uy|]
    let value:data<unit> = Optional (1I, Native.option.Some (Hash hash))
    let valueSerialized = serializer.PackSingleObject(value)
    let valueDeserialized = serializer.UnpackSingleObject(valueSerialized)
    Assert.AreEqual (value, valueDeserialized)

[<Test>]
let ``Serialization of Optional None``() =
    let hash = Array.map (fun x -> x*x) [|10uy..41uy|]
    let value:data<unit> = Optional (1I, Native.option.None)
    let valueSerialized = serializer.PackSingleObject(value)
    let valueDeserialized = serializer.UnpackSingleObject(valueSerialized)
    Assert.AreEqual (value, valueDeserialized)

[<Test>]
let ``Serialization of Outpoint``() =
    let hash = Array.map (fun x -> x*x) [|10uy..41uy|]
    let outpoint = { txHash = hash; index = 11ul }
    let value:data<unit> = Outpoint outpoint
    let valueSerialized = serializer.PackSingleObject(value)
    let valueDeserialized = serializer.UnpackSingleObject(valueSerialized)
    Assert.AreEqual (value, valueDeserialized)

[<Test>]
let ``Serialization of Output``() =
    let hash = Array.map (fun x -> x*x) [|10uy..41uy|]
    let output = { lock = PKLock hash; spend = { asset = hash; amount = 11UL }}
    let value:data<unit> = Output output
    let valueSerialized = serializer.PackSingleObject(value)
    let valueDeserialized = serializer.UnpackSingleObject(valueSerialized)
    Assert.AreEqual (value, valueDeserialized)

[<Test>]
let ``Serialization of composition 1``() =
    let value:data<unit> = Zen.Types.Extracted.Data2 (15I, 25I, (Bool true), (Bool true))
    let valueSerialized = serializer.PackSingleObject(value)
    let valueDeserialized = serializer.UnpackSingleObject(valueSerialized)
    Assert.AreEqual (value, valueDeserialized)

[<Test>]
let ``Serialization of composition 2``() =
    let hash = Array.map (fun x -> x*x) [|10uy..41uy|]
    let outpoint = { txHash = hash; index = 11ul }
    let value:data<unit> = Zen.Types.Extracted.Data2 (35I, 45I, (Outpoint outpoint), (Outpoint outpoint))
    let valueSerialized = serializer.PackSingleObject(value)
    let valueDeserialized = serializer.UnpackSingleObject(valueSerialized)
    Assert.AreEqual (value, valueDeserialized)

[<Test>]
let ``Serialization of tuple with Some-value Optional``() =
    let value:data<unit> = Zen.Types.Extracted.Data2 (15I, 25I, (Bool true), Optional (1I, Native.option.Some (Bool true)))
    let valueSerialized = serializer.PackSingleObject(value)
    let valueDeserialized = serializer.UnpackSingleObject(valueSerialized)
    Assert.AreEqual (value, valueDeserialized)

[<Test>]
let ``Serialization of tuple with None-value Optional``() =
    let value:data<unit> = Zen.Types.Extracted.Data2 (15I, 25I, (Bool true), Optional (1I, Native.option.None))
    let valueSerialized = serializer.PackSingleObject(value)
    let valueDeserialized = serializer.UnpackSingleObject(valueSerialized)
    Assert.AreEqual (value, valueDeserialized)