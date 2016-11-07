module Consensus.Tests

open NUnit.Framework
open NUnit.Framework.Constraints
open System

// TODO: fix code-completions in Xamarin referencing different
// NUnit assembly to that actually used in compilation, resulting
// in several apparently missing methods that are actually present!

// Note: current tests use blocks and transactions that are not and
// cannot be valid under full validation rules. They are only
// *structurally* valid, in that they serialize and deserialize.
// TODO: use valid test objects.

open MsgPack
open MsgPack.Serialization
open Types
open Serialization


// If you need to uncomment this line, your serializers aren't
// passing context when serializing inner objects
// SerializationContext.Default <- context

let zhash = Array.zeroCreate<byte>(32)
let randomhash = Array.map (fun x -> x*x) [|10uy..41uy|]
let randomhash' = Array.map (fun x -> x*x*x) [|26uy..57uy|]

let pklock = PKLock randomhash

[<Test>]
let ``PKLock has lockVersion 0u``() =
    Assert.AreEqual(0u,lockVersion pklock)

[<Test>]
let ``Hashes have the right length (32 bytes)``() =
    Assert.AreEqual(PubKeyHashBytes,zhash.Length)
    Assert.AreEqual(PubKeyHashBytes,randomhash.Length)
    Assert.AreEqual(PubKeyHashBytes,randomhash'.Length)

let minlockcore = {version=0u;lockData=[]}


[<Test>]
let ``Minimal lockcore has LockCore type``() =
    Assert.That(minlockcore, Is.InstanceOf<LockCore>())


let randomdata = Array.map (fun x -> (byte)((x*x) % 256u)) [|134u..1043u|]

let randomtxhash = Array.map2 (+) randomhash randomhash'

let randomtxhash' = Array.map ((+) 2uy) randomtxhash

let cbaselock = CoinbaseLock minlockcore
let feelock = FeeLock minlockcore
let ctlock = ContractLock (contractHash=randomhash', data=randomdata)


[<Test>]
let ``CoinbaseLock minlockcore has version 0u``() =
    Assert.That(lockVersion cbaselock, Is.EqualTo(0u))

[<Test>]
let ``FeeLock minlockcore has version 0u``() =
    Assert.That(lockVersion feelock, Is.EqualTo(0u))

let zspend = {asset=zhash;amount=1234UL}

let ``zspend has type spend``() =
    Assert.That(zspend, Is.InstanceOf<Spend>())

let pkoutput = {lock=pklock;spend=zspend}
let ctoutput = {lock=ctlock;spend=zspend}
let feeoutput = {lock=feelock;spend=zspend}


let randomoutpoint = {txHash=randomtxhash;index=43u}

let pkwit = randomhash

let ctr = {code=randomdata;bounds=Array.take 50 randomdata; hint = Array.skip 25 randomdata}

let extContract = Contract ctr

let ts = new DateTimeOffset(new DateTime(2017,1,1,8,0,0), TimeSpan(0L))
let unixts = ts.ToUnixTimeSeconds()

let difficultymantissa = 1uy;
let difficultyexponent = 0u;

let minpdiff = (difficultyexponent &&& 0x00ffffffu) ||| ((uint32)difficultymantissa <<< 24)

[<Test>]
let ``Minimum pdiff is 2^24 = 0x01000000 = 16777216u``() = 
    Assert.That(minpdiff, Is.EqualTo(0x01000000))


let tx:Transaction =
    {
    version=0u;
    inputs = [randomoutpoint];
    witnesses=[pkwit];
    outputs=[pkoutput];
    contract=Some extContract
    }

let bheader:BlockHeader =
    {
    version=0u;
    parent=zhash;
    txMerkleRoot=randomhash;
    witnessMerkleRoot=randomhash';
    contractMerkleRoot=randomtxhash';
    extraData = []
    timestamp = unixts;
    pdiff = minpdiff;
    nonce = zhash
    }

let blk: Block =
    {
    header=bheader;
    transactions=[tx]
    }

// Testing serialization.

[<Test>]
let ``OutputLock serializes CoinbaseLock``() =
    let serializer = MessagePackSerializer.Get<OutputLock>(context)
    use stream = new IO.MemoryStream()
    Assert.That((fun () -> serializer.Pack(stream, cbaselock)), Throws.Nothing);

[<Test>]
let ``OutputLock round trip of CoinbaseLock produces same object``() =
    let serializer = MessagePackSerializer.Get<OutputLock>(context)
    use stream = new IO.MemoryStream()
    serializer.Pack(stream, cbaselock)
    stream.Position <- 0L
    let res = serializer.Unpack(stream)
    Assert.That(res, Is.EqualTo(cbaselock))

[<Test>]
let ``OutputLock round trip of FeeLock produces same object``() =
    let serializer = MessagePackSerializer.Get<OutputLock>(context)
    use stream = new IO.MemoryStream()
    serializer.Pack(stream, feelock)
    stream.Position <- 0L
    let res = serializer.Unpack(stream)
    Assert.That(res, Is.EqualTo(feelock))

[<Test>]
let ``OutputLock round trip of PKLock produces same object``() =
    let serializer = MessagePackSerializer.Get<OutputLock>(context)
    use stream = new IO.MemoryStream()
    serializer.Pack(stream, pklock)
    stream.Position <- 0L
    let res = serializer.Unpack(stream)
    Assert.That(res, Is.EqualTo(pklock))

[<Test>]
let ``OutputLock round trip of ContractLock produces same object``() =
    let serializer = MessagePackSerializer.Get<OutputLock>(context)
    use stream = new IO.MemoryStream()
    serializer.Pack(stream, ctlock)
    stream.Position <- 0L
    let res = serializer.Unpack(stream)
    Assert.That(res, Is.EqualTo(ctlock))

[<Test>]
let ``Spend round trip produces same object``() =
    let serializer = MessagePackSerializer.Get<Spend>(context)
    use stream = new IO.MemoryStream()
    serializer.Pack(stream, zspend)
    stream.Position <- 0L
    let res = serializer.Unpack(stream)
    Assert.That(res, Is.EqualTo(zspend))

[<Test>]
let ``Output round trip of PK output produces same object``() =
    let serializer = MessagePackSerializer.Get<Output>(context)
    use stream = new IO.MemoryStream()
    serializer.Pack(stream, pkoutput)
    stream.Position <- 0L
    //let strbytes =
    //    (stream.ToArray())
    //    |> Array.fold (fun st x -> st + sprintf "%02X, " x) ""
    //TestContext.Out.WriteLine(strbytes)
    let res = serializer.Unpack(stream)
    Assert.That(res, Is.EqualTo(pkoutput))

[<Test>]
let ``Output round trip of contract locked output produces same object``() =
    let serializer = MessagePackSerializer.Get<Output>(context)
    use stream = new IO.MemoryStream()
    serializer.Pack(stream, ctoutput)
    stream.Position <- 0L
    let res = serializer.Unpack(stream)
    Assert.That(res, Is.EqualTo(ctoutput))

[<Test>]
let ``Output round trip of feelocked output produces same object``() =
    let serializer = MessagePackSerializer.Get<Output>(context)
    use stream = new IO.MemoryStream()
    serializer.Pack(stream, feeoutput)
    stream.Position <- 0L
    let res = serializer.Unpack(stream)
    Assert.That(res, Is.EqualTo(feeoutput))

[<Test>]
let ``Outpoint round trip produces same object``() =
    let serializer = MessagePackSerializer.Get<Outpoint>(context)
    use stream = new IO.MemoryStream()
    serializer.Pack(stream, randomoutpoint)
    stream.Position <- 0L
    let res = serializer.Unpack(stream)
    Assert.That(res, Is.EqualTo(randomoutpoint))

[<Test>]
let ``Contract round trip produces same object``() =
    let serializer = MessagePackSerializer.Get<Contract>(context)
    use stream = new IO.MemoryStream()
    serializer.Pack(stream, ctr)
    stream.Position <- 0L
    let res = serializer.Unpack(stream)
    Assert.That(res, Is.EqualTo(ctr))

[<Test>]
let ``ExtendedContract round trip of version 0 contract produces same object``() =
    let serializer = MessagePackSerializer.Get<ExtendedContract>(context)
    use stream = new IO.MemoryStream()
    serializer.Pack(stream, extContract)
    stream.Position <- 0L
    let res = serializer.Unpack(stream)
    Assert.That(res, Is.EqualTo(extContract))

let highVContract = HighVContract (version=9u, data=[|0uy;5uy;75uy|])

[<Test>]
let ``ExtendedContract round trip of HighVContract produces same object``() =
    let serializer = MessagePackSerializer.Get<ExtendedContract>(context)
    use stream = new IO.MemoryStream()
    serializer.Pack(stream, highVContract)
    stream.Position <- 0L
    let res = serializer.Unpack(stream)
    Assert.That(res, Is.EqualTo(highVContract))

[<Test>]
let ``Witness round trip produces same object``() =
    let serializer = MessagePackSerializer.Get<Witness>(context)
    use stream = new IO.MemoryStream()
    serializer.Pack(stream, pkwit)
    stream.Position <- 0L
    let res = serializer.Unpack(stream)
    Assert.That(res, Is.EqualTo(pkwit))

[<Test>]
let ``Transaction round trip of transaction with contract produces same object``() =
    let serializer = MessagePackSerializer.Get<Transaction>(context)
    use stream = new IO.MemoryStream()
    serializer.Pack(stream, tx)
    stream.Position <- 0L
    let res = serializer.Unpack(stream)
    Assert.That(res, Is.EqualTo(tx))

let txwithoutcontract = { tx with contract=None }
[<Test>]
let ``Transaction round trip of transaction without contract produces same object``() =
    let serializer = MessagePackSerializer.Get<Transaction>(context)
    use stream = new IO.MemoryStream()
    serializer.Pack(stream, txwithoutcontract)
    stream.Position <- 0L
    let res = serializer.Unpack(stream)
    Assert.That(res, Is.EqualTo(txwithoutcontract))

[<Test>]
let ``BlockHeader round trip without extraData produces same object``() =
    let serializer = MessagePackSerializer.Get<BlockHeader>(context)
    use stream = new IO.MemoryStream()
    serializer.Pack(stream, bheader)
    stream.Position <- 0L
    let res = serializer.Unpack(stream)
    Assert.That(res, Is.EqualTo(bheader))

// Generate some extra hashes
let extraMRs =
    List.unfold
        (fun (i, prev, curr) -> 
            if i > 5 then None else
                let next = Array.map2 (+) prev curr
                Some (next, (i+1,curr,next)))
        (0,randomhash,randomhash')

let bheaderwithextradata = {bheader with extraData=extraMRs}

[<Test>]
let ``BlockHeader round trip with extraData produces same object``() =
    let serializer = MessagePackSerializer.Get<BlockHeader>(context)
    use stream = new IO.MemoryStream()
    serializer.Pack(stream, bheaderwithextradata)
    stream.Position <- 0L
    let res = serializer.Unpack(stream)
    Assert.That(res, Is.EqualTo(bheaderwithextradata))

[<Test>]
let ``Block round trip produces same object``() =
    let serializer = MessagePackSerializer.Get<Block>(context)
    use stream = new IO.MemoryStream()
    serializer.Pack(stream, blk)
    stream.Position <- 0L
    let res = serializer.Unpack(stream)
    Assert.That(res, Is.EqualTo(blk))

// TODO: Test erroneous objects