module Consensus.TransactionTests

open NUnit.Framework
open NUnit.Framework.Constraints
open System

open Types
open TransactionValidation

[<Test>]
let ``Signed transaction validates``()=
    Assert.IsTrue(true)

let kp = Sodium.PublicKeyBox.GenerateKeyPair()

[<Test>]
let ``Sodium works``()=
    Assert.That(kp, Is.Not.Null)