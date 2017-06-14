module Test
open NUnit.Framework
open Zen
let run (app: App) = 
    app.PurgeAssetsCache();
    app.SetNetwork("standalone")
    app.ResetBlockChainDB()
    app.AddGenesisBlock()
    app.ResetWalletDB()
    app.Acquire(0)
    app.AddKey(0)

    let contract = "SendToken.cs"
    app.ActivateTestContract(contract, 10)
    app.MineTestBlock()

    app.SendTestContract(contract, app.GetTestAddress(0).Bytes)
    app.MineTestBlock()
    "script succeeded"