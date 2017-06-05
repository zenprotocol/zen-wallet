module Test
open NUnit.Framework
open Zen
let run (app: App) = 
    app.SetNetwork("standalone")
    app.ResetBlockChainDB()
    app.AddGenesisBlock()
    //Assert.IsTrue(app.Spend(250000000, aplock()
    app.ResetWalletDB()

    app.ActivateTestContract("TestContract.cs", 10)
 //   app.ActivateTestContract("Contracts.fs", 10)  
    
    app.MineBlock()
    app.Acquire(0)

    Assert.IsTrue(app.SendTestContract("TestContract.cs", 250000000UL, app.GetTestAddress(1).Bytes ))
//    Assert.IsTrue(app.SendTestContract("Contracts.fs", 250000000UL, app.GetTestAddress(1).Bytes ))

 //   Assert.IsTrue(app.Spend(250000000UL, app.GetTestContractAddress("TestContract.cs"), app.GetTestAddress(0).Bytes))
    app.MineBlock()


   // Assert.IsTrue(app.SendTestContract("TestContract.cs", "issue_token"))


    //app.SetWallet("test1")
    //app.ResetWalletDB()
    //app.AddKey(0)

    //app.MineBlock()
    //app.Dump()

    //app.SetWallet("default")

    //app.Dump()
    "spend script succeeded"

