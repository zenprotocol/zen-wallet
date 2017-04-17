module Test
open NUnit.Framework
open Zen
let run (app: App) = 
    app.SetNetwork("standalone")
    app.ResetBlockChainDB()
    app.AddGenesisBlock()
    app.ResetWalletDB()
    app.Acuire(0)
    Assert.IsTrue(app.Spend(250000000, 0))


    app.SetWallet("test1")
    app.ResetWalletDB()
    app.AddKey(0)

    app.MineBlock()
    app.Dump()
    //app.SetWallet("default")
    //app.Dump()
    "spend script succeded"

