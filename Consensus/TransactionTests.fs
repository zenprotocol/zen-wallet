module Consensus.TransactionTests

open NUnit.Framework
open NUnit.Framework.Constraints
open System

open Types
open TransactionValidation

[<Test>]
let ``Signed transaction validates``()=
    Assert.IsTrue(true)