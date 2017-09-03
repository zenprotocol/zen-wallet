module ContractExamples.FStarCompatilibity

let unCost (Zen.Cost.C inj:Zen.Cost.cost<'Aa, 'An>) : 'Aa = inj.Force()

let vectorToList (z:Zen.Vector.t<'Aa, _>) : List<'Aa> =
     // 0I's are eraseable
     Zen.Vector.foldl 0I 0I (fun acc e -> Zen.Cost.ret (e::acc)) [] z 
     |> unCost
     |> List.rev


let listToVector (ls:List<'Aa>) : Zen.Vector.t<'Aa, _> =
    let len = List.length ls 
    let lsIndexed = List.mapi (fun i elem -> bigint (len - i - 1), elem) ls // vertors are reverse-zero-indexed

    List.foldBack (fun (i,x) acc -> Zen.Vector.VCons (i, x, acc)) lsIndexed Zen.Vector.VNil