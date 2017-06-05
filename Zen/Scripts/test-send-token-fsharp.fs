module Test
open NUnit.Framework
open ContractExamples
open Wallet

open Zen
let run (app: App) = 
    app.PurgeAssetsCache()
    app.PurgeContracts()
    app.SetNetwork("standalone")
    app.ResetBlockChainDB()
    app.AddGenesisBlock()
    app.ResetWalletDB()
    app.Acquire(0)
    app.AddKey(0)

    let contract = ContractExamples.QuotedContracts.tokenGen
    let code = ContractExamples.Execution.quotedToString(contract)
    let contractHash = app.GetContractHash(code)
    let contractAddress = app.GetTestContractAddress(contractHash)

    printfn "contract address: %A" contractAddress
    let myaddress = System.Convert.ToBase64String(app.GetTestAddress(0).Bytes)
    printfn "my address: %A" myaddress

    app.ActivateTestContractCode(code, 10)
    app.MineBlock()

    //// send to contract and get the outpoint
    //let res, tx = app.Spend(contractAddress, 1UL, app.GetTestAddress(0).Bytes, null)
    //Assert.IsTrue(res)
    //let outpoint = app.GetFirstContractLockOutpoint(tx)
  
    //let tokenGenCommandData = Array.concat [ [|0uy|] ; app.GetOutpointBytes(outpoint) ]
    //Assert.IsTrue(app.SendTestQuotedContract(contractHash, tokenGenCommandData))

    //app.MineBlock()
    //app.Dump()
 
   // app.GUI()

    "script succeeded"