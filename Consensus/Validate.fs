module Consensus.Validate

type Output =
    PKOutput | ContractOutput | HighVOutput

type Witness = uint8 list

type PayToPKH = {inputs: PKOutput list, outputs: Output list, witnesses: Witness list}

type Transaction =
    PayToPKH
    | PayToCH of {callingInput: CallContractOutput, otherInputs: ContractOutput, witnesses: Witness list}
    | HighVTx of {version: uint8, inputs: HighVOutput list, outputs: Output list, witnesses: Witness list}

let validateTx context transaction = match transaction with
                                     | PayToPKH tx -> validatePayToPKH context tx
                                     | PayToCH tx -> validatePayToCH context tx

