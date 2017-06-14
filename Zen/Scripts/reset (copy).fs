module Test
open NUnit.Framework
open Zen
let run (app: App) = 
    //let contract = "TestContract.cs"
    let contract = "Contracts.fs"

    app.SetNetwork("standalone")
    app.ResetBlockChainDB()
    app.AddGenesisBlock()
    app.ResetWalletDB()

    app.ActivateTestContract(contract, 10)
    
    app.MineTestBlock()
    app.Acquire(0)

    //let buyCommandData = Array.concat [ [|0uy|] ; app.GetTestAddress(1).Bytes ]
    //Assert.IsTrue(app.Spend(app.GetTestContractAddress(contract), 250000000UL, buyCommandData ))
    //app.MineTestBlock()






    //Assert.IsTrue(app.SendTestContract(contract, buyCommandData ))
    //app.MineTestBlock()

    //app.SetWallet("test1")
    //app.ResetWalletDB()
    //app.AddKey(0)

    //app.MineTestBlock()
    //app.Dump()

    //app.SetWallet("default")

    app.Dump()
    "spend script succeeded"

