module Test
open NUnit.Framework
open Zen
let run (app: App) = 
    app.SetNetwork("standalone")
    app.ResetBlockChainDB()
    app.AddGenesisBlock()
    app.ResetWalletDB()
    app.Acquire(0)
    "script succeeded"

