module Test
open Zen
let run (app: App) = 
    app.SetNetwork("lan_host")
    app.SetBlockChainDBSuffix("2")
    app.ResetBlockChainDB()
    app.AddGenesisBlock()

    app.SetWallet("test2")
    app.ResetWalletDB()
    app.AddKey(0)
    app.Reconnect()
    0