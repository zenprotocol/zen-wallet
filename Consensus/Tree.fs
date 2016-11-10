module Consensus.Tree

type LazyTree<'LeafData,'BranchData> =
    | Leaf of 'LeafData
    | Branch of 'BranchData * Lazy<LazyTree<'LeafData,'BranchData>> * Lazy<LazyTree<'LeafData,'BranchData>>

let rec lazyCata fLeaf fBranch (tree : LazyTree<'LeafData, 'BranchData>) : 'r =
    let recurse = lazyCata fLeaf fBranch
    let lazyRecurse (lTree : Lazy<_>) = lazy(recurse <| lTree.Force())
    match tree with
    | Leaf leafData -> fLeaf leafData
    | Branch (branchData, lazyTreeL, lazyTreeR) ->
        fBranch branchData <|| (lazyRecurse lazyTreeL, lazyRecurse lazyTreeR)

let lazyMap fLeaf fBranch =
    lazyCata (Leaf << fLeaf)
         (fun branchData lazyTreeL lazyTreeR ->
             Branch (fBranch branchData, lazyTreeL, lazyTreeR))

type FullTree<'L,'B> =
    | Leaf of 'L
    | Branch of data : 'B * left : FullTree<'L,'B> * right : FullTree<'L,'B>

let rec cata fLeaf fBranch (tree : FullTree<'L,'B>) =
    let recurse = cata fLeaf fBranch
    match tree with
    | Leaf leafData -> fLeaf leafData
    | Branch (branchData, leftTree, rightTree) ->
        fBranch branchData
        <| recurse leftTree
        <| recurse rightTree

let map fLeaf fBranch =
    cata (Leaf << fLeaf)
         (fun branchData treeL treeR ->
             Branch (fBranch branchData, treeL, treeR))


let rec height = function
    | Leaf _ -> 0
    | Branch (left=left) -> 1 + height left

type LocBranchData<'T> = {branchData:'T; height:int32; loc:uint32}
type LocLeafData<'T> = {leafData:'T; loc:uint32}


type Loc = {height:int32; loc:uint32}
type LocData<'T> = {data:'T; location:Loc}


let addLocation height tree =
    let right = function
        | {Loc.height=h;loc=l} -> {height = h-1; loc = l ||| (1u <<< (h - 1))}
    let left = function
        | {Loc.height=h; loc=l} -> {height = h-1; loc=l}
    let rec inner = function
        | location, Leaf v -> Leaf {data=v; location=location}
        | {height=height;loc=loc} as location, Branch (branchData, leftTree, rightTree) ->
            Branch <|
            (
                {data=branchData;location=location},
                inner (left location, leftTree),
                inner (right location, rightTree)
            )
    inner ({height=height;loc=0u}, tree)

let locLocation<'L,'B> : (FullTree<LocData<'L>,LocData<'B>> -> Loc) = function
    | Leaf v -> v.location
    | Branch (data=data) -> data.location

let locHeight<'L,'B> : (FullTree<LocData<'L>,LocData<'B>> -> int32) = function
    | Leaf v -> v.location.height
    | Branch (data=data) -> data.location.height

let locLoc<'L,'B> : (FullTree<LocData<'L>,LocData<'B>> -> uint32) = function
    | Leaf v -> v.location.loc
    | Branch (data=data) -> data.location.loc

type OptTree<'L,'B> = FullTree<'L option,'B>

let complete (s:seq<'T>) =
    let defaultB = {data=None; location={height=0;loc=0u}}
    let defaultHeight n = {data=None; location={height=n;loc=0u}}
    let leaves = Seq.map (addLocation 0 << Leaf << Some) s
    let rec parent stack a = 
        match stack, a with
        | y::tl, x when locHeight y = locHeight x -> parent tl (Branch (defaultHeight <| locHeight x + 1,y,x))
        | _, _ -> a :: stack
    let rec withEmptyNodes l =
        let defaultLeaf = Leaf defaultB
        let rec insertOne h tree =
            match locHeight tree with
            | n when n=h -> tree
            | n ->
                insertOne h
                <| Branch
                   (
                   defaultHeight <| n + 1,
                   tree,
                   Leaf <| defaultHeight n
                   )
        let group left right =
            match locHeight left, locHeight right with
            | greater, lesser ->
                Branch (
                    defaultHeight <| greater+1,
                    left,
                    insertOne greater right
                       )
        match l with
        | [] -> defaultLeaf
        | x::[] -> x
        | x::y::tl -> withEmptyNodes <| (group y x) :: tl
    let treeWithoutLoc = withEmptyNodes << Seq.fold parent [] <| leaves
    let stripLocation = map (fun v -> v.data) (fun v -> v.data)
    addLocation <| locHeight treeWithoutLoc <| stripLocation treeWithoutLoc



//let fromSeq (s:seq<'T>) =
//    let toLeaf = Seq.map (Leaf << Some)
//    let rec parent stack a =
//        match stack, a with
//        | y::tl, x when height y = height x -> parent tl (Branch ((height x) + 1, lazy(y), lazy(x)))
//        | _, _ -> a::stack
//    let rec withEmptyNodes l =
//        match l with
//        | [] -> empty()
//        | x::[] -> x
//        | x::y::tl when height x = height y -> withEmptyNodes <| (Branch ((height x) + 1, lazy(y), lazy(x))) :: tl
//        | x::tl -> withEmptyNodes <| (Branch ((height x) + 1, lazy(emptyN (height x)), lazy(x))) :: tl
//    withEmptyNodes << Seq.fold parent [] << toLeaf <| s

//let empty = fun () -> Leaf None

//let emptyN<'T> =
//    let emptySeq =
//        Seq.unfold
//        <| fun (n,empN) ->
//            let nextEmp = Branch (n+1, lazy(empN), lazy(empN))
//            Some (empN,(n+1,nextEmp))
//        <| ((0, empty()) : int * LazyTree<'T option, _>)
//        |> Seq.cache
//    fun n -> Seq.item n emptySeq

//let height tree = match tree with
//                  | Leaf _ -> 0
//                  | Branch (n, _, _) -> n

//type LocBranchData = {height:int32; loc:uint32}
//type LocLeafData<'T> = {leafData:'T; loc:uint32}

//let addLoc : (LazyTree<'T,int32> -> LazyTree<LocLeafData<'T>,LocBranchData>) =
//    let right loc height = loc &&& (1u <<< (height - 1))
//    let rec innerAddLoc : (uint32 -> LazyTree<'T,int32> -> LazyTree<LocLeafData<'T>,LocBranchData>) =
//        fun loc tree ->
//            match tree with
//            | Leaf v -> Leaf {leafData=v; loc=loc}
//            | Branch (h, lTreeL, lTreeR) ->
//                Branch <|
//                (
//                {height=h;loc=loc},
//                lazy(innerAddLoc loc <| lTreeL.Force()),
//                lazy(innerAddLoc (right loc h) <| lTreeR.Force())
//                )
//    fun tree -> innerAddLoc 0u tree

//let heightLoc tree = match tree with
//                     | Leaf _ -> 0
//                     | Branch ({height=n},_,_) -> n


//type Tree<'L,'B> =
//    | Leaf of 'L
//    | Branch of data : 'B * left : Tree<'L,'B> option * right : Tree<'L,'B> option

//let rec cata fLeaf fBranch (tree : Tree<'L,'B>) =
//    let recurse = cata fLeaf fBranch
//    match tree with
//    | Leaf leafData -> fLeaf leafData
//    | Branch (branchData, leftTreeOpt, rightTreeOpt) ->
//        fBranch branchData
//        <| Option.map recurse leftTreeOpt
//        <| Option.map recurse rightTreeOpt

