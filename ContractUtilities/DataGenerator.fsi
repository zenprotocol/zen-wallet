namespace ContractUtilities
  module DataGenerator = begin
    type ContractJsonData = FSharp.Data.JsonProvider<...>
    val makeData :
      meta:ContractExamples.Execution.ContractMetadata ->
        utxos:seq<Consensus.Types.Outpoint * Consensus.Types.Output> ->
          opcode:byte -> m:Map<string,string> -> string option
  end


