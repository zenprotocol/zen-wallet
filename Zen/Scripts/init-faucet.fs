module Test
open NUnit.Framework
open Consensus
open Zen

let run (app: App) = 
    app.ResetBlockChainDB()
    app.AddGenesisBlock()
    app.ResetWalletDB()
    app.Connect()
    app.Acquire(0)
    "script succeeded"