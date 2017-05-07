module Test
open Zen
let run (app: App) = 
    app.SetNetwork("lan_client")
    app.SetBlockChainDBSuffix("1")
    app.ResetBlockChainDB()
    app.AddGenesisBlock()

    app.SetWallet("test1")
    app.ResetWalletDB()
    app.Acuire(0)
    app.Reconnect()
    let x = app.Spend(1000, 0)
    0