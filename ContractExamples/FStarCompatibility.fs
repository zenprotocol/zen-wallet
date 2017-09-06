module ContractExamples.FStarCompatilibity

open ContractExamples.Execution
open Zen
open Types

type FStarContractFunction = inputMsg -> transactionSkeleton

let private unCost (Cost.C inj:Cost.cost<'Aa, 'An>) : 'Aa = inj.Force()

let private vectorToList (z:Vector.t<'Aa, _>) : List<'Aa> =
     // 0I's are eraseable
     Vector.foldl 0I 0I (fun acc e -> Cost.ret (e::acc)) [] z 
     |> unCost
     |> List.rev

let private listToVector (ls:List<'Aa>) : Vector.t<'Aa, _> =
    let len = List.length ls 
    let lsIndexed = List.mapi (fun i elem -> bigint (len - i - 1), elem) ls // vertors are reverse-zero-indexed

    List.foldBack (fun (i,x) acc -> Vector.VCons (i, x, acc)) lsIndexed Vector.VNil

let private fsToFstArr : byte[] -> Array.t<'Aa,_> =
    ArrayRealized.A

let private fstToFsArr : Array.t<'Aa,_> -> byte[] =
    function ArrayRealized.A a -> a

let private fstToFsOutpoint (a:outpoint) : Consensus.Types.Outpoint =
    { txHash = fstToFsArr a.txHash; index = a.index }

let private fsToFstLock (a:Consensus.Types.OutputLock) : outputLock =
    match a with 
    | Consensus.Types.PKLock (pkHash) ->
        PKLock (fsToFstArr pkHash)

let private fstToFsLock (a:outputLock) : Consensus.Types.OutputLock =
    match a with 
    | PKLock (pkHash) ->
        Consensus.Types.PKLock (fstToFsArr pkHash)

let private fsToFstOutput (a:Option<Consensus.Types.Output>) : FStar.Pervasives.Native.option<output> =
    match a with
    | None -> 
        FStar.Pervasives.Native.option.None
    | Some output -> 
        FStar.Pervasives.Native.option.Some { lock = fsToFstLock output.lock; spend = { asset = fsToFstArr output.spend.asset; amount = output.spend.amount}}

let private fstToFsOutput (a:output) : Consensus.Types.Output =
    { lock = fstToFsLock a.lock; spend = { asset = fstToFsArr a.spend.asset; amount = a.spend.amount}}

let private convertUtxo (utxo: Utxo) : utxo = fstToFsOutpoint >> utxo >> fsToFstOutput

let convertInput (a:ContractFunctionInput) : inputMsg =
    match a with (msg, contractHash, utxo) -> 
    {
        cmd = 0uy;
        data = Prims.Mkdtuple2 (0I, ByteArray (0I, fsToFstArr msg));
        contractHash = ArrayRealized.A contractHash;
        utxo = convertUtxo utxo
        lastTx = FStar.Pervasives.Native.option<outpoint>.None
    }

let convertResult (a:transactionSkeleton) : TransactionSkeleton =
  match a with Tx (_,outpoints,_,outputs,_,ByteArray (_, data)) -> 
      (List.map fstToFsOutpoint (vectorToList outpoints), List.map fstToFsOutput (vectorToList outputs), fstToFsArr data)

let convertContractFunction (fn:FStarContractFunction) = convertInput >> fn >> convertResult

open NUnit.Framework

[<Test>]
let ``Vector convertions should get original value``() =
    let arr = [1;2;3]
    let vec = arr |> listToVector |> vectorToList
    Assert.AreEqual (vec, arr)

[<Test>]
let ``Should convert outpoint``() =
    let randomHash = Array.map (fun x -> x*x) [|10uy..41uy|]
    let index = 100u
    let fstRandomHash = fsToFstArr randomHash
    let fsOutpoint = fstToFsOutpoint { txHash = fstRandomHash; index = index }
    Assert.AreEqual (fsOutpoint.txHash, randomHash)
    Assert.AreEqual (fsOutpoint.index, index)


let len : Zen.Array.t<'A, _> -> bigint =
    function Zen.ArrayRealized.A a -> bigint (Array.length a)

let fstarMockFunction (input:inputMsg) : transactionSkeleton =  
  let data = input.contractHash
  let outpoints = [{ txHash = data; index = 0u }]
  let outputs = [{ lock = PKLock data; spend = {asset = data; amount = 0UL } }]
  Tx (bigint (List.length outpoints), listToVector outpoints, bigint (List.length outputs), (listToVector outputs), len data, ByteArray ((len data), data))

//TODO: cover tests