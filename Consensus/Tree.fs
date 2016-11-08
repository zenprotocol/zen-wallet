module Consensus.Tree

type Tree<'LeafData,'INodeData> =
    | Leaf of 'LeafData
    | Branch of 'INodeData * Lazy<Tree<'LeafData,'INodeData>> * Lazy<Tree<'LeafData,'INodeData>>

let rec cata fLeaf fBranch (tree : Tree<'LeafData, 'INodeData>) : 'r =
    let recurse = cata fLeaf fBranch
    match tree with
    | Leaf leafData -> fLeaf leafData
    | Branch (branchData, lazyTreeL, lazyTreeR) ->
        let lazyRecurse : Lazy<'t> -> Lazy<'r> = fun lTree -> lazy(recurse <| lTree.Force())
        fBranch branchData <|| (lazyRecurse lazyTreeL, lazyRecurse lazyTreeR)

let map fLeaf fBranch =
    cata (Leaf << fLeaf)
         (fun branchData lazyTreeL lazyTreeR ->
             Branch (fBranch branchData, lazyTreeL, lazyTreeR))