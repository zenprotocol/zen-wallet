module Test
open NUnit.Framework
open ContractExamples
open Wallet
open Zen

let run (app: App) = 

    let contractParams = new ContractExamples.QuotedContracts.SecureTokenParameters(app.GetTestAddress(0).Bytes)
    let contract = ContractExamples.QuotedContracts.secureTokenFactory(contratParams)
    let code = ContractExamples.Execution.quotedToString(contract)
    let contractHash = app.GetContractHash(code)
    let contractAddress = new Wallet.core.Data.Address(contractHash, Wallet.core.Data.AddressType.Contract)

    let myaddress = app.GetTestAddress(0)
    let myaddressBytes = System.Convert.ToBase64String(myaddress.Bytes)

    printfn "contract address: %A" contractAddress
    printfn "my address bytes: %A" myaddressBytes
    printfn "my address: %A" myaddress

    app.ActivateTestContractCode(code, 10)
    app.MineTestBlock()

    ////// send to contract and get the outpoint
    let res, tx = app.Spend(contractAddress, 1UL, app.GetTestAddress(0).Bytes, null)
    Assert.IsTrue(res)

    let outpoint = app.GetFirstContractLockOutpoint(tx)
  
    let tokenGenCommandData = Array.concat [ [|0uy|] ; app.GetOutpointBytes(outpoint) ]
    Assert.IsTrue(app.SendTestQuotedContract(contractHash, tokenGenCommandData))

    app.MineTestBlock()
    app.Dump()
 
   // app.GUI()

    "script succeeded"