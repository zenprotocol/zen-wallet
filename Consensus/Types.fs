module Consensus.Types
open Froto.Serialization
open Froto.Serialization.Encoding

// Length: 32
type Hash = byte[]

type LockCore = {
    version: uint32;
    lockData: byte[] list
    }

type OutputLock =
    public
    | CoinbaseLock of LockCore
    | FeeLock of LockCore
    | ContractSacrificeLock of LockCore
    | PKLock of pkHash: Hash
    | ContractLock of contractHash : Hash * data : byte[]
    | HighVLock of lockCore : LockCore * typeCode : int

type OutputLockP =
    | FeeLockP
    | PKLockP of Hash
    | ContractSacrificeLockP of Hash option
    | ContractLockP of Hash * byte[]
    | OtherLockP of RawField list // Allow future locks w/ multiple fields
    with
    member this.Version = match this with
                          | OtherLockP _ -> 1u  //TODO global "max defined version"+1
                          | _ -> 0u
    
    static member Default = OtherLockP []

    static member Serializer (m, zcb) =
        match m with
        | OtherLockP rawFieldList ->
            if List.exists (fun (raw:RawField) ->
                Set.contains (raw.FieldNum) <| set [1;3;5;7;] ) rawFieldList
            then failwith "can't serialize known lock as unknown lock"
            Encode.fromRawFields rawFieldList
        | FeeLockP ->
            Encode.fromBool 1 true
        | PKLockP hash ->
            if hash.Length <> 32 then failwith "bad pubkey"
            hash |> System.ArraySegment |> Encode.fromBytes 3
        | ContractSacrificeLockP optHash ->
            optHash
            |> Option.map (fun h ->
                if Array.length h <> 32 then failwith "bad contract hash"
                else h)
            |> Option.defaultValue [||]
            |> System.ArraySegment |> Encode.fromBytes 5
        | ContractLockP (hash, data) ->
            if Array.length hash <> 32 then failwith "bad contract hash"
            let bs = Array.append hash data
            bs |> System.ArraySegment |> Encode.fromBytes 7
        <| zcb

    static member DecoderRing =
        let standardFailure = fun () -> failwith "bad recognized output lock"
        [
            0, fun m rawField ->
                match m with
                | OtherLockP rawFieldList ->
                    OtherLockP (rawField :: rawFieldList)
                | _ -> failwith "Unknown fields not allowed on known output locks"
            1, fun m rawField ->
                match m, Decode.toBool rawField with
                | OtherLockP [], true ->
                    FeeLockP
                | _ -> standardFailure()
            3, fun m rawField ->
                match m, Decode.toBytes rawField with
                | OtherLockP [], hash when Array.length hash = 32 -> PKLockP hash
                | _ -> standardFailure()
            5, fun m rawField ->
                match m, Decode.toBytes rawField with
                | OtherLockP [], hash ->
                    if Array.length hash = 0 then ContractSacrificeLockP None
                    elif Array.length hash = 32 then ContractSacrificeLockP (Some hash)
                    else standardFailure()
                | _ -> standardFailure()
            7, fun m rawField ->
                match m, Decode.toBytes rawField with
                | OtherLockP [], bs when Array.length bs >= 32 ->
                    let hash, data = Array.splitAt 32 bs
                    ContractLockP (hash, data)
                | _ -> standardFailure()
        ]
        |> Map.ofList

    static member RememberFound (m, found:FieldNum) = m
    static member DecodeFixup m =
        match m with
        | OtherLockP rawFieldList -> OtherLockP (List.rev rawFieldList)
        | _ -> m
    static member RequiredFields = Set.empty
    static member UnknownFields m =
        match m with
        | OtherLockP rawFieldList -> rawFieldList
        | _ -> List.empty
    static member FoundFields _ = Set.empty
    
(*    static member FoundFields m =
        match m with
        | FeeLockP -> set [1]
        | PKLockP _-> set [3]
        | ContractSacrificeLockP _ -> set [5]
        | ContractLockP _ -> set [7]
        | OtherLockP rawFieldList ->
            List.map (fun (f:RawField) -> f.FieldNum) rawFieldList |> Set.ofList
*)

let lockVersion : (OutputLock -> uint32) = function
    | CoinbaseLock lCore
    | FeeLock lCore
    | ContractSacrificeLock lCore
    | HighVLock(lockCore = lCore) -> lCore.version
    | PKLock _ -> 0u
    | ContractLock _ -> 0u

let typeCode : (OutputLock -> int) = function
    | CoinbaseLock _ -> 0
    | FeeLock _ -> 1
    | ContractSacrificeLock _ -> 2
    | PKLock _ -> 3
    | ContractLock (_,_) -> 4
    | HighVLock (typeCode = tCode) -> tCode

let lockData : (OutputLock -> byte[] list) = function
    | CoinbaseLock lCore
    | FeeLock lCore
    | ContractSacrificeLock lCore
    | HighVLock(lockCore = lCore) -> lCore.lockData
    | PKLock pkHash ->
        [pkHash]
    | ContractLock (hash,data) ->
        [hash;data]

type Spend =
    {asset: Hash; amount: uint64}
    with
    static member Default = {asset = [||]; amount = 0UL}
    static member Serializer (m, zcb) =
        (m.asset |> System.ArraySegment |>
            Encode.fromBytes 1) >>
        (m.amount |>
            Encode.fromVarint 2)
        <| zcb
    static member DecoderRing =
        [
            0, fun m rawField -> failwith "unknown fields not allowed!"
            1, fun m rawField -> {m with asset = rawField |> Decode.toBytes} : Spend
            2, fun m rawField -> {m with amount = (rawField |> Decode.toUInt64)}
        ]
        |> Map.ofList
    
    static member RememberFound (m, found:FieldNum) : Spend = m
    static member DecodeFixup m = m
    static member RequiredFields = Set.empty
    static member UnknownFields (m:Spend) = []:RawField list
    static member FoundFields _ = Set.empty
    //static member FoundFields (m:Spend) : Set<FieldNum> = set [1;2;]


type Output = {lock: OutputLock; spend: Spend}

type OutputP =
    {lockP: OutputLockP; spendP:Spend}
    with
    static member Default = {lockP=OutputLockP.Default; spendP=Spend.Default}

    static member Serializer (m, zcb) =
        (m.lockP |> Encode.fromMessage Serialize.toZcbLD 1) >>
        (m.spendP |> Encode.fromMessage Serialize.toZcbLD 2)
        <| zcb

    static member DecoderRing =
        [
            0, fun m rawField -> failwith "unknown fields not allowed!"
            1, fun m rawField -> {m with lockP = rawField |> Deserialize.fromRawField OutputLockP.Default}:OutputP
            2, fun m rawField -> {m with spendP = rawField |> Deserialize.fromRawField Spend.Default}
        ]
        |> Map.ofList

    static member RememberFound (m, found:FieldNum) : OutputP = m
    static member DecodeFixup m = m
    static member RequiredFields = Set.empty
    static member UnknownFields (m:OutputP) = []:RawField list
    static member FoundFields _ = Set.empty
    //static member FoundFields (m:OutputP) : Set<FieldNum> = set[1;2;]

type Outpoint =
    {txHash: Hash; index: uint32}
    with
    static member Default = {txHash = [||]; index = 0u}
    static member Serializer (m, zcb) =
        (m.txHash |> System.ArraySegment |>
            Encode.fromBytes 1) >>
        (m.index |>
            Encode.fromVarint 2)
        <| zcb
    static member DecoderRing =
        [
            0, fun m rawField -> failwith "unknown fields not allowed!"
            1, fun m rawField -> {m with txHash = rawField |> Decode.toBytes} : Outpoint
            2, fun m rawField -> {m with index = (rawField |> Decode.toUInt32)}
        ]
        |> Map.ofList
    
    static member RememberFound (m, found:FieldNum) : Outpoint = m
    static member DecodeFixup m = m
    static member RequiredFields = Set.empty
    static member UnknownFields (m:Outpoint) = []:RawField list
    static member FoundFields _ = Set.empty
    //static member FoundFields (m:Outpoint) : Set<FieldNum> = set [1;2;]

// Length: variable
type Witness = byte[]

type Contract = {code: byte[]; bounds: byte[]; hint: byte[]}    //erasable

type ExtendedContract =                                         //erasable
    | Contract of Contract
    | HighVContract of version : uint32 * data : byte[]

let contractVersion : (ExtendedContract -> uint32) = function
    | Contract _ -> 0u
    | HighVContract(version=version) -> version

let ContractHashBytes = 32
let PubKeyHashBytes = 32
let TxHashBytes = 32

type ContractP =
    {version: uint32; code: byte[]; hint: byte[]; _unknownFields: RawField list}
    with
    static member Default = {version=0u; code=[||];hint=[||]; _unknownFields=[]}

    static member Serializer (m, zcb) =
        if m.version = 0u && not <| List.isEmpty m._unknownFields
        then failwith "Version 0 contract cannot have extra fields"
        else
        (m.version |> Encode.fromVarint 1) >>
        (m.code |> System.ArraySegment |> Encode.fromBytes 2) >>
        (m.hint |> System.ArraySegment |> Encode.fromBytes 3) >>
        (m._unknownFields |> Encode.fromRawFields)
        <| zcb

    static member DecoderRing =
        [
            0, fun m rawField -> {m with _unknownFields = rawField :: m._unknownFields}
            1, fun m rawField -> {m with version = rawField |> Decode.toUInt32}
            2, fun m rawField -> {m with code = rawField |> Decode.toBytes}
            3, fun m rawField -> {m with hint = rawField |> Decode.toBytes}
        ]
        |> Map.ofList

    static member RememberFound (m, found:FieldNum) : ContractP = m
    static member DecodeFixup m =
        if m.version = 0u && not <| List.isEmpty m._unknownFields
        then failwith "Version 0 contract cannot have extra fields"
        else
        {m with _unknownFields = List.rev m._unknownFields}
    static member RequiredFields = Set.empty
    static member UnknownFields m = m._unknownFields
    static member FoundFields _ = Set.empty

//type Transaction = {version: uint32; inputs: Outpoint list; witnesses: Witness list; outputs: Output list; contract: Contract option}
type Transaction = {                                            //erasable
    version: uint32;
    inputs: Outpoint list;
    witnesses: Witness list;
    outputs: Output list;
    contract: ExtendedContract option
    }

type TransactionP =
    {
        version: uint32;
        inputsP: Outpoint list;
        witnessesP: Witness list;
        outputsP: OutputP list;
        contractP: ContractP option;
        _unknownFields: RawField list;
    }
    with
    static member Default =
        {version = 0u; inputsP = []; witnessesP = []; outputsP = []; contractP = None; _unknownFields = []}
    
    static member Serializer (m, zcb) =
        if m.version = 0u && not <| List.isEmpty m._unknownFields
        then failwith "Version 0 contract cannot have extra fields"
        else
        (m.version |> Encode.fromVarint 1) >>
        (m.inputsP |> Encode.fromRepeated (Encode.fromMessage Serialize.toZcbLD) 2) >>
        (m.witnessesP |> List.map System.ArraySegment |> Encode.fromRepeated Encode.fromBytes 3) >>
        (m.outputsP |> Encode.fromRepeated (Encode.fromMessage Serialize.toZcbLD) 4) >>
        (m.contractP |> Encode.fromOptionalMessage Serialize.toZcbLD 5) >>
        (m._unknownFields |> Encode.fromRawFields)
        <| zcb

    static member DecoderRing =
        [
            0, fun m rawField -> {m with _unknownFields = rawField :: m._unknownFields}:TransactionP
            1, fun m rawField -> {m with version = rawField |> Decode.toUInt32}
            2, fun m rawField ->
                { m with
                    inputsP = (rawField |> Deserialize.fromRawField Outpoint.Default)
                             :: m.inputsP
                }
            3, fun m rawField ->
                { m with
                    witnessesP = (rawField |> Decode.toBytes)
                                :: m.witnessesP
                }
            4, fun m rawField ->
                { m with
                    outputsP = (rawField |> Deserialize.fromRawField OutputP.Default)
                              :: m.outputsP
                }
            5, fun m rawField ->
                { m with
                    contractP = (rawField |> Deserialize.optionalMessage ContractP.Default)
                }
        ]
        |> Map.ofList
    
    static member RememberFound (m, _) = m
    static member DecodeFixup m =
        if m.version = 0u && not <| List.isEmpty m._unknownFields
        then failwith "Version 0 transaction cannot have extra fields"
        else
        { m with
            _unknownFields = List.rev m._unknownFields
            inputsP = List.rev m.inputsP
            witnessesP = List.rev m.witnessesP
            outputsP = List.rev m.outputsP
        }
    static member RequiredFields = Set.empty
    static member UnknownFields m = m._unknownFields
    static member FoundFields _ = Set.empty

//Length: 64
type Nonce = byte[]

type Commitments = {
    txMerkleRoot: Hash;
    witnessMerkleRoot: Hash;
    contractMerkleRoot: Hash;
    extraCommitments: Hash list
    }
    with
        member this.ToList = this.txMerkleRoot :: this.witnessMerkleRoot :: this.contractMerkleRoot :: this.extraCommitments

        static member Default = {
            txMerkleRoot = [||];
            witnessMerkleRoot = [||];
            contractMerkleRoot = [||];
            extraCommitments = [];
            }

        static member Serializer (m:Commitments, zcb) =
            ((List.map System.ArraySegment m.ToList |>
                Encode.fromRepeated Encode.fromBytes 1))
            <| zcb
        static member DecoderRing =
            [
                0, fun m rawField -> failwith "unknown fields not allowed!"
                1, fun m rawField -> {m with extraCommitments = (rawField |> Decode.toBytes) :: m.extraCommitments}
            ]
            |> Map.ofList
        static member RememberFound (m, found:FieldNum) : Commitments = m

        static member DecodeFixup m =
            match List.rev m.extraCommitments with
            | t::w::c::extra ->
                {
                    txMerkleRoot = t;
                    witnessMerkleRoot = w;
                    contractMerkleRoot = c;
                    extraCommitments = extra;
                }
            | _ -> failwith "too few commitments!"  // should never happen

        static member RequiredFields = Set.empty
        static member UnknownFields (m:Commitments) = []:RawField list
        static member FoundFields _ = Set.empty
        //static member FoundFields (m:Commitments) : Set<FieldNum> = set [1]

type BlockHeader = {
    version: uint32;
    parent: Hash;
    blockNumber: uint32;
    txMerkleRoot: Hash;
    witnessMerkleRoot: Hash;
    contractMerkleRoot: Hash;
    extraData: byte[] list;
    timestamp: int64;
    pdiff: uint32;
    nonce: Nonce;
    }
    with
        static member Default = {
            version = 0u;
            parent = [||];
            blockNumber = 0u;
            txMerkleRoot = [||];
            witnessMerkleRoot = [||];
            contractMerkleRoot = [||];
            extraData = [];
            timestamp = 0L;
            pdiff = 0u;
            nonce = [||];
        }

        member this.Commitments = {
            txMerkleRoot = this.txMerkleRoot;
            witnessMerkleRoot = this.witnessMerkleRoot;
            contractMerkleRoot = this.contractMerkleRoot;
            extraCommitments = this.extraData;
        }

        static member Serializer (m, zcb) =
            (m.version |>
                Encode.fromVarint 1) >>
            (m.parent |> System.ArraySegment |>
                Encode.fromBytes 2) >>
            (m.blockNumber |>
                Encode.fromVarint 3) >>
            (m.Commitments |>
                Encode.fromMessage Serialize.toZcbLD 4) >>
            (m.timestamp |>
                Encode.fromSFixed64 5) >>
            (m.pdiff |>
                Encode.fromFixed32 6) >>
            (m.nonce |> System.ArraySegment |>
                Encode.fromBytes 7)
            <| zcb
        
        static member DecoderRing =
            [
                0, fun m rawField -> failwith "unknown fields not allowed!"
                1, fun m rawField -> {m with version = rawField |> Decode.toUInt32} : BlockHeader
                2, fun m rawField -> {m with parent = (rawField |> Decode.toBytes)}
                3, fun m rawField -> {m with blockNumber = rawField |> Decode.toUInt32}
                4, fun m rawField ->
                    let commitments = Deserialize.fromRawField Commitments.Default rawField
                    {m with
                        txMerkleRoot = commitments.txMerkleRoot
                        witnessMerkleRoot = commitments.witnessMerkleRoot
                        contractMerkleRoot = commitments.contractMerkleRoot
                        extraData = commitments.extraCommitments
                        }
                5, fun m rawField -> {m with timestamp = rawField |> Decode.toSFixed64}
                6, fun m rawField -> {m with pdiff = rawField |> Decode.toFixed32}
                7, fun m rawField -> {m with nonce = rawField |> Decode.toBytes}
            ]
            |> Map.ofList
        
        static member RememberFound (m, found:FieldNum) : BlockHeader = m

        static member DecodeFixup m = m

        static member RequiredFields = Set.empty
        static member UnknownFields m = []:RawField list
        static member FoundFields _ = Set.empty
        //static member FoundFields m :Set<FieldNum> = set [1;2;3;4;5;6;7;]

type ContractContext = {contractId: byte[]; utxo: Map<Outpoint, Output>; tip: BlockHeader; }

let merkleData (bh : BlockHeader) =
    bh.txMerkleRoot :: bh.witnessMerkleRoot :: bh.contractMerkleRoot :: bh.extraData

type Block = {
    header: BlockHeader;
    transactions: Transaction list
    }

type BlockP = {
    headerP: BlockHeader;
    transactionsP: TransactionP list
    }
    with
    static member Default = {headerP = BlockHeader.Default; transactionsP = []}
    static member Serializer (m, zcb) =
        (m.headerP |> Encode.fromMessage Serialize.toZcbLD 1) >>
        (m.transactionsP |>
            Encode.fromRepeated (Encode.fromMessage Serialize.toZcbLD) 2)
        <| zcb
    static member DecoderRing =
        [
            0, fun m rawField -> failwith "unknown fields not allowed!"
            1, fun m rawField -> {m with headerP = rawField |> Deserialize.fromRawField BlockHeader.Default} : BlockP
            2, fun m rawField ->
                {m with
                    transactionsP = (rawField |> Deserialize.fromRawField TransactionP.Default)
                                   :: m.transactionsP}
        ]
        |> Map.ofList
    
    static member RememberFound (m, _)  = m
    static member DecodeFixup m:BlockP = { m with transactionsP = List.rev m.transactionsP }
    static member RequiredFields = Set.empty
    static member UnknownFields (m:Outpoint) = []:RawField list
    static member FoundFields _ = Set.empty
    //static member FoundFields (m:Outpoint) : Set<FieldNum> = set [1;2;]
