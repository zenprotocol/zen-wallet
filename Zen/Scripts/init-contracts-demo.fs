module Test
open NUnit.Framework
open Consensus
open Zen

let run (app: App) = 
    let contract = "DatafeedContract.cs"
    app.ResetBlockChainDB()
    app.AddGenesisBlock()
    app.ResetWalletDB()
    app.ActivateTestContract(contract, 10)
    app.MineBlock()
    app.Acquire(0)
    Assert.IsTrue(app.Spend(app.GetTestContractAddress(contract), 2500000UL))
    app.MineBlock()
    "script succeeded"