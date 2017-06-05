module Test
open NUnit.Framework
open Consensus
open Zen

let run (app: App) = 
    let contract = "DatafeedContract.cs"
    app.SetNetwork("standalone")
    app.ResetBlockChainDB()
    app.AddGenesisBlock()
    app.ResetWalletDB()
    app.ActivateTestContract(contract, 10)
    app.MineBlock()
    app.Acquire(0)
    Assert.IsTrue(app.Spend(app.GetTestContractAddress(contract), 2500000UL))
    app.MineBlock()
    let outpoint = app.FindOutpoint(app.GetTestContractAddress(contract), Consensus.Tests.zhash)
    let data = Array.concat [ Array.init 32 (fun i -> byte(i)) ; [|(byte)(outpoint.index)|] ; outpoint.txHash ]
    Assert.IsTrue(app.SendTestContract(contract, data))
    app.MineBlock()
    app.Dump()
    "script succeeded"