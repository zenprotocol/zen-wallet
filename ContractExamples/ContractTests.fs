module ContractExamples.ContractTests

open System
open NUnit.Framework
open NUnit.Framework.Constraints

open QuotedContracts
open Consensus.Types
open Sodium

let zenAsset = Array.create 32 0uy
let fakeControl = Array.create 32 1uy
let fakeOracle= Array.create 32 2uy
let fakeCall = Array.create 32 3uy

let goodSeed = Array.init 32 (fun x -> byte x)
let badSeed = Array.init 32 (fun x -> 1uy + byte x)

let keypair = Sodium.PublicKeyAuth.GenerateKeyPair goodSeed

let oracleParams = {ownerPubKey = keypair.PublicKey}
let callParams = {
    numeraire=zenAsset;
    controlAsset=fakeControl;
    controlAssetReturn=Consensus.Merkle.innerHash keypair.PublicKey;
    oracle=fakeOracle;
    underlying="GOOG";
    price=1337.00M;
    minimumCollateralRatio=0.2M;
    ownerPubKey=keypair.PublicKey
    }
let secureParams = {destination= Consensus.Merkle.innerHash keypair.PublicKey}


let quotedOracle = oracleFactory oracleParams
let quotedCall = callOptionFactory callParams
let quotedSecure = secureTokenFactory secureParams

let compiledOracle = Swensen.Unquote.Operators.eval quotedOracle
let compiledCall = Swensen.Unquote.Operators.eval quotedCall
let compiledSecure = Swensen.Unquote.Operators.eval quotedSecure

let BadTx = Swensen.Unquote.Operators.eval QuotedContracts.BadTx
let utxosOf (d:Map<Outpoint, Output>) k = d.TryFind(k)

[<Test>]
let ``Oracle contract makes BadTx when invoked with nonsense``() =
    Assert.That( (BadTx = compiledOracle ([|7uy;8uy|],[|4uy|],fun _ -> None )), Is.True)



let oracleOutpoint = {txHash = Array.create 32 77uy; index=0u}
let oracleOutpointSerialized = simplePackOutpoint oracleOutpoint
let oracleData = Array.zeroCreate<byte> 32
Random(1234).NextBytes oracleData
let oracleMsg = Array.append oracleOutpointSerialized oracleData
let oracleSig = Consensus.Authentication.sign oracleMsg keypair.PrivateKey
let oracleWit = Array.append oracleMsg oracleSig
let oracleOutput:Output = {lock=ContractLock (fakeOracle,[||]);spend={asset=zenAsset; amount = 50000UL}}
let oracleOutputMap = Map.add oracleOutpoint oracleOutput Map.empty
let oracleRes = compiledOracle (oracleWit,fakeOracle,utxosOf oracleOutputMap )

[<Test>]
let ``Oracle contract succeeds when invoked with authentication``() =
    Assert.That((BadTx = oracleRes),Is.False)

[<Test>]
let ``Oracle contract creates output with new data``() =
    match oracleRes with
    | _, oputs, _ ->
        Assert.That(oputs, Has.Length.GreaterThanOrEqualTo 2)
        match oputs.[1] with
        | {lock=ContractLock (hash, data)} ->
            Assert.That(hash, Is.EqualTo fakeOracle)
            Assert.That(data, Is.EqualTo oracleData)
        | _ -> raise <| AssertionException "Not a contract lock."

[<Test>]
let ``Call option contract makes BadTx when invoked with nonsense``() =
    Assert.That( (BadTx = compiledCall ([|7uy;8uy|],[|4uy|],fun _ -> None )), Is.True)

let callInitialData = {lock=ContractLock(fakeCall, makeData (0UL,1000UL,0UL));spend={asset=callParams.controlAsset;amount=1UL}}
let callFunds = {lock=ContractLock(fakeCall, [||]);spend={asset=callParams.numeraire;amount=1000UL}}
let fakeReturnPubKeyHash = Array.zeroCreate<byte> 32
Random(3456).NextBytes fakeReturnPubKeyHash

let rng = Random(4567)

let callOutpointsSerialized = List.init 3 (fun _ -> Array.zeroCreate<byte> 33)
List.iter (fun x -> rng.NextBytes x) callOutpointsSerialized
let callOutpoints = List.map makeOutpoint callOutpointsSerialized 


[<Test>]
let ``Call option accepts collateral from authenticated source, increments counter``() =
    let callCollateralize = {
        lock=ContractLock(fakeCall, makeCollateralizeData fakeReturnPubKeyHash 0UL keypair);
        spend={asset=callParams.numeraire;amount=30000UL}
    }
    let utxos = utxosOf (Map.ofList <| List.zip callOutpoints [callCollateralize; callInitialData; callFunds])
    let msg = [|0uy|] :: callOutpointsSerialized |> Array.concat
    let res = compiledCall (msg,fakeCall,utxos)
    Assert.That((res=BadTx), Is.False)
    let points, puts, _ = res
    match puts.[0] with
    | {lock=ContractLock (hash,d)} ->
        let data = tryParseData d
        match data with
        | Some ( 0UL, 31000UL, 1UL) -> Assert.True(true)
        | _ -> raise <| AssertionException "bad data"
    | _ -> raise <| AssertionException "bad output"

[<Test>]
let ``Call option returns to sender when source not authed, does not increment counter``() =
    let badKeypair = PublicKeyAuth.GenerateKeyPair badSeed
    let badCollateralize = {
        lock=ContractLock(fakeCall, makeCollateralizeData fakeReturnPubKeyHash 0UL badKeypair);
        spend={asset=callParams.numeraire;amount=30000UL}}
    let utxos = utxosOf (Map.ofList <| List.zip callOutpoints [badCollateralize; callInitialData; callFunds])
    let msg = [|0uy|] :: callOutpointsSerialized |> Array.concat
    let res = compiledCall (msg,fakeCall,utxos)
    Assert.That((res=BadTx), Is.False)
    let points, puts, _ = res
    Assert.That(puts.Length, Is.EqualTo 1)
    Assert.That((puts.[0]={badCollateralize with lock=PKLock fakeReturnPubKeyHash}),Is.True)

[<Test>]
let ``Call option contract Buy operation sends correct number of assets to purchaser``() =
    let callBuy = {
        lock=ContractLock(fakeCall, makeBuyData fakeReturnPubKeyHash);
        spend={asset=callParams.numeraire; amount= uint64 <| ceil callParams.price * 5M }
    }
    let callFundedData = {callInitialData with lock=ContractLock(fakeCall, makeData(0UL, 1000000UL, 1UL))}
    let callMoreFunds = {callFunds with spend={callFunds.spend with amount=1000000UL}}
    let utxos = utxosOf (Map.ofList <| List.zip callOutpoints [callBuy; callFundedData; callMoreFunds])
    let msg = [|1uy|] :: callOutpointsSerialized |> Array.concat
    let res = compiledCall (msg,fakeCall,utxos)

    Assert.That((res=BadTx), Is.False)
    let points, puts, _ = res
    Assert.That(points.Length, Is.EqualTo 3)
    Assert.That(puts.Length, Is.EqualTo 3)
    let destination, purchase =
        match puts.[0] with
        | {lock=PKLock (hash); spend=spend} -> hash, spend
        | _ -> raise <| AssertionException (sprintf "bad purchase output: %A" puts.[0])
    Assert.That(destination, Is.EqualTo fakeReturnPubKeyHash)
    Assert.That(purchase.asset, Is.EqualTo fakeCall)
    Assert.That(purchase.amount, Is.EqualTo 5)

[<Test>]
let ``Call option contract Exercise operation sends correct amount of numeraire to exerciser``() =
    let callExercise = {
        lock=ContractLock(fakeCall, makeExerciseData fakeReturnPubKeyHash);
        spend={asset=fakeCall; amount=3UL }
    }
    let callFundedData = {callInitialData with lock=ContractLock(fakeCall, makeData(20UL, 1000000UL, 1UL))}
    let callMoreFunds = {callFunds with spend={callFunds.spend with amount=1000000UL}}
    let utxos = utxosOf (Map.ofList <| List.zip callOutpoints [callExercise; callFundedData; callMoreFunds])
    let msg = [|2uy|] :: callOutpointsSerialized |> Array.concat
    let res = compiledCall (msg, fakeCall, utxos)

    Assert.That((res=BadTx), Is.False)
    let points, puts, _ = res
    let destination, payoff =
        match puts.[0] with
        | {lock=PKLock (hash); spend=spend} -> hash, spend
        | _ -> raise <| AssertionException (sprintf "bad purchase output: %A" puts.[0])
    Assert.That(destination, Is.EqualTo fakeReturnPubKeyHash)
    Assert.That(payoff.asset, Is.EqualTo callParams.numeraire)
    Assert.That(payoff.amount, 3M*callParams.price |> floor |> uint64 |> Is.EqualTo)

[<Test>]
let ``Call option contract Close returns funds and control token on auth``()=
    let callClose = {
        lock=ContractLock(fakeCall, makeCloseData fakeReturnPubKeyHash 1UL keypair);
        spend={asset=callParams.numeraire;amount=1UL}
    }
    let callClosingData = {callInitialData with lock=ContractLock(fakeCall, makeData(0UL, 1000000UL, 1UL))}
    let callMoreFunds = {callFunds with spend={callFunds.spend with amount=1000000UL}}
    let utxos = utxosOf (Map.ofList <| List.zip callOutpoints [callClose; callClosingData; callMoreFunds])
    let msg = [|3uy|] :: callOutpointsSerialized |> Array.concat
    let res = compiledCall (msg,fakeCall,utxos)
    Assert.That((res=BadTx), Is.False)
    let points, puts, _ = res
    Assert.That(puts.Length, Is.EqualTo 3)
    let locks = List.map (fun p -> p.lock) puts
    Assert.That(List.forall (fun l -> match l with PKLock fakeReturnPubKeyHash -> true | _ -> false) locks, Is.True)
    let spends = List.map (fun p -> p.spend) puts
    let expectedSpends = [callClose.spend; callMoreFunds.spend; callClosingData.spend]
    Assert.That((spends=expectedSpends), Is.True)

//open Execution

[<Test>]
let ``Compilation of raw contract succeeds``()=
    let contract = """fun (message,contracthash,utxos)  ->
    let ownerPubKey = Array.zeroCreate<byte> 32
    maybe {
        if message.Length <> 129 then return! None
        let m, s = message.[0..64], message.[65..128]
        if not <| verify s m ownerPubKey then return! None
        let opoint = {txHash=m.[1..32]; index = (uint32)m.[0]}
        let! oput = utxos opoint
        let dataOutput = {
            spend={asset=contracthash; amount=1UL};
            lock=ContractLock (contracthash, m.[33..64])
        }
        return ([opoint;], [oput; dataOutput], [||])
    } |> Option.defaultValue BadTx"""
    let comp = Execution.compile contract
    Assert.That(comp, Is.Not.EqualTo None)

open MBrace.FsPickler.Combinators

[<Test>]
let ``Compiled raw contract deserialized correctly``() =
    let contract = """fun (message,contracthash,utxos)  ->
    let ownerPubKey = Array.zeroCreate<byte> 32
    maybe {
        if message.Length <> 129 then return! None
        let m, s = message.[0..64], message.[65..128]
        if not <| verify s m ownerPubKey then return! None
        let opoint = {txHash=m.[1..32]; index = (uint32)m.[0]}
        let! oput = utxos opoint
        let dataOutput = {
            spend={asset=contracthash; amount=1UL};
            lock=ContractLock (contracthash, m.[33..64])
        }
        return ([opoint;], [oput; dataOutput], [||])
    } |> Option.defaultValue BadTx"""
    let comp = Execution.compile contract
    match comp with
    | None -> failwith "no contract"
    | Some c ->
        let f:ContractFunction = Execution.deserialize c
        let x = f ([||],[||],fun _ -> None)
        Assert.That((x=BadTx), Is.True)

[<Test>]
let ``Quoted contract metadata extracts``()=
    let callStr = Execution.quotedToString quotedCall
    let m = Execution.metadata callStr
    Assert.That(m, Is.Not.EqualTo(None))

[<Test>]
let ``Quoted secure token metadata extracts``()=
    let secureStr = Execution.quotedToString quotedSecure
    let m = Execution.metadata secureStr
    Assert.That(m, Is.Not.EqualTo(None))