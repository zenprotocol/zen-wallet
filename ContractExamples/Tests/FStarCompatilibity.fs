module Tests.ContractExamples.FStarCompatilibity

open NUnit.Framework
open ContractExamples.FStarCompatilibity

let arr = [1;2;3]

[<Test>]
let ``Should get original value``() =
    let vec = arr |> listToVector |> vectorToList
    Assert.AreEqual (vec, arr)