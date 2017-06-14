module Test
open NUnit.Framework
open Zen
let run (app: App) = 
    app.SetNetwork("lan_client")
    app.SetBlockChainDBSuffix("1")
    app.ResetBlockChainDB()
    app.AddGenesisBlock()

    app.SetWallet("test1")
    app.ResetWalletDB()
    app.Acquire(0)
    app.Connect()
    app.SetMinerEnabled(true)
    Assert.IsTrue(app.Spend(app.GetTestAddress(0), 1000000UL))
    0