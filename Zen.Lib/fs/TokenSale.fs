#light "off"
module TokenSale
open Prims
open FStar.Pervasives
type command =
| Initialize
| Buy
| Fail


let uu___is_Initialize : command  ->  Prims.bool = (fun ( projectee  :  command ) -> (match (projectee) with
| Initialize -> begin
true
end
| uu____7 -> begin
false
end))


let uu___is_Buy : command  ->  Prims.bool = (fun ( projectee  :  command ) -> (match (projectee) with
| Buy -> begin
true
end
| uu____14 -> begin
false
end))


let uu___is_Fail : command  ->  Prims.bool = (fun ( projectee  :  command ) -> (match (projectee) with
| Fail -> begin
true
end
| uu____21 -> begin
false
end))


let zen : Consensus.Types.hash = (Prims.unsafe_coerce (fun ( uu____25  :  Consensus.Types.hash ) -> (failwith "Not yet implemented:zen")))


let token : Consensus.Types.hash = (Prims.unsafe_coerce (fun ( uu____31  :  Consensus.Types.hash ) -> (failwith "Not yet implemented:token")))


let ownerlock : Consensus.Types.hash = (Prims.unsafe_coerce (fun ( uu____37  :  Consensus.Types.hash ) -> (failwith "Not yet implemented:ownerlock")))


let price : FStar.UInt64.t = (Prims.unsafe_coerce (fun ( uu____45  :  FStar.UInt64.t ) -> (failwith "Not yet implemented:price")))


let mkspend : Consensus.Types.hash  ->  FStar.UInt64.t  ->  Consensus.Types.spend = (fun ( asset  :  Consensus.Types.hash ) ( amount  :  FStar.UInt64.t ) -> {Consensus.Types.asset = asset; Consensus.Types.amount = amount})


let mkoutput : Consensus.Types.outputLock  ->  Consensus.Types.spend  ->  Consensus.Types.output = (fun ( lock  :  Consensus.Types.outputLock ) ( spend  :  Consensus.Types.spend ) -> {Consensus.Types.lock = lock; Consensus.Types.spend = spend})


let parse_commands : Consensus.Types.opcode  ->  (command, Prims.unit) Zen.Cost.cost = (fun ( opcode  :  Consensus.Types.opcode ) -> (Zen.Cost.incRet (Prims.parse_int "5") (match ((opcode = (FStar.UInt8.uint_to_t (Prims.parse_int "0")))) with
| true -> begin
Initialize
end
| uu____83 -> begin
(match ((opcode = (FStar.UInt8.uint_to_t (Prims.parse_int "1")))) with
| true -> begin
Buy
end
| uu____84 -> begin
Fail
end)
end)))


let parse_outpoint : (Prims.nat, Prims.unit Consensus.Types.inputData) Prims.dtuple2  ->  (Consensus.Types.outpoint FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost = (fun ( uu___667_123  :  (Prims.nat, Prims.unit Consensus.Types.inputData) Prims.dtuple2 ) -> (match (uu___667_123) with
| Prims.Mkdtuple2 (_0_35, Consensus.Types.Data2 (uu____148, uu____149, Consensus.Types.Outpoint (outPoint), uu____151)) when (_0_35 = (Prims.parse_int "2")) -> begin
(Zen.OptionT.incSome (Prims.parse_int "4") outPoint)
end
| uu____168 -> begin
(Zen.OptionT.incNone (Prims.parse_int "4"))
end))


let parse_outputLock : (Prims.nat, Prims.unit Consensus.Types.inputData) Prims.dtuple2  ->  (Consensus.Types.outputLock FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost = (fun ( uu___668_223  :  (Prims.nat, Prims.unit Consensus.Types.inputData) Prims.dtuple2 ) -> (match (uu___668_223) with
| Prims.Mkdtuple2 (_0_36, Consensus.Types.Data2 (uu____248, uu____249, uu____250, Consensus.Types.OutputLock (outputLock))) when (_0_36 = (Prims.parse_int "2")) -> begin
(Zen.OptionT.incSome (Prims.parse_int "4") outputLock)
end
| uu____268 -> begin
(Zen.OptionT.incNone (Prims.parse_int "4"))
end))


let totalInputZen : Consensus.Types.utxo  ->  Consensus.Types.outpoint  ->  (FStar.UInt64.t FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost = (fun ( utxo  :  Consensus.Types.utxo ) ( outpoint  :  Consensus.Types.outpoint ) -> (match ((utxo outpoint)) with
| FStar.Pervasives.Native.Some (output) -> begin
(match ((output.spend.asset = zen)) with
| true -> begin
(Zen.OptionT.incSome (Prims.parse_int "5") output.spend.amount)
end
| uu____327 -> begin
(Zen.OptionT.incNone (Prims.parse_int "5"))
end)
end
| FStar.Pervasives.Native.None -> begin
(Zen.OptionT.incNone (Prims.parse_int "5"))
end))


let issueTokens : FStar.UInt64.t  ->  (FStar.UInt64.t, Prims.unit) Zen.Cost.cost = (fun ( zen1  :  FStar.UInt64.t ) -> (Zen.Cost.incRet (Prims.parse_int "2") (FStar.UInt64.div zen1 price)))


let mkTx : Prims.nat  ->  (Consensus.Types.outpoint, Prims.unit) Zen.Vector.t  ->  (Consensus.Types.output, Prims.unit) Zen.Vector.t  ->  (Consensus.Types.transactionSkeleton, Prims.unit) Zen.Cost.cost = (fun ( n1  :  Prims.nat ) ( returnOutpoints  :  (Consensus.Types.outpoint, Prims.unit) Zen.Vector.t ) ( outputs  :  (Consensus.Types.output, Prims.unit) Zen.Vector.t ) -> (Zen.Cost.incRet (Prims.parse_int "2") (Consensus.Types.Tx ((Prims.parse_int "2"), returnOutpoints, n1, outputs, (Prims.parse_int "0"), Consensus.Types.Empty))))


let main : Consensus.Types.inputMsg  ->  (Consensus.Types.transactionSkeleton FStar.Pervasives.Native.option, Prims.unit) Zen.Cost.cost = (fun ( inputMsg  :  Consensus.Types.inputMsg ) -> (match ((Zen.Option.bind inputMsg.lastTx inputMsg.utxo)) with
| FStar.Pervasives.Native.None -> begin
(Zen.Cost.inc (Prims.parse_int "36") (Zen.OptionT.bindLift2 (Prims.parse_int "7") (Prims.parse_int "27") (Prims.parse_int "2") (Zen.OptionT.bindLift2 (Prims.parse_int "0") (Prims.parse_int "4") (Prims.parse_int "3") (Zen.Cost.ret inputMsg.lastTx) (parse_outpoint inputMsg.data) (Zen.Tuple.curry Zen.Vector.of_t2)) (Zen.OptionT.bindLift2 (Prims.parse_int "9") (Prims.parse_int "15") (Prims.parse_int "3") (Zen.OptionT.map (Prims.parse_int "9") (mkoutput (Consensus.Types.PKLock (ownerlock))) (Zen.OptionT.map (Prims.parse_int "9") (mkspend zen) (Zen.OptionT.bind (Prims.parse_int "4") (Prims.parse_int "5") (parse_outpoint inputMsg.data) (totalInputZen inputMsg.utxo)))) (Zen.OptionT.ap (Prims.parse_int "4") (Prims.parse_int "11") (Zen.OptionT.map (Prims.parse_int "4") mkoutput (parse_outputLock inputMsg.data)) (Zen.OptionT.map (Prims.parse_int "11") (mkspend inputMsg.contractHash) (Zen.OptionT.bindLift (Prims.parse_int "2") (Prims.parse_int "9") (Zen.OptionT.bind (Prims.parse_int "4") (Prims.parse_int "5") (parse_outpoint inputMsg.data) (totalInputZen inputMsg.utxo)) issueTokens))) (Zen.Tuple.curry Zen.Vector.of_t2)) (mkTx (Prims.parse_int "2"))) (Prims.parse_int "1"))
end
| FStar.Pervasives.Native.Some (output0) -> begin
(Zen.OptionT.bindLift2 (Prims.parse_int "7") (Prims.parse_int "28") (Prims.parse_int "2") (Zen.OptionT.bindLift2 (Prims.parse_int "0") (Prims.parse_int "4") (Prims.parse_int "3") (Zen.Cost.ret inputMsg.lastTx) (parse_outpoint inputMsg.data) (Zen.Tuple.curry Zen.Vector.of_t2)) (Zen.OptionT.bindLift2 (Prims.parse_int "9") (Prims.parse_int "15") (Prims.parse_int "4") (Zen.OptionT.map (Prims.parse_int "9") (mkoutput (Consensus.Types.PKLock (ownerlock))) (Zen.OptionT.map (Prims.parse_int "9") (mkspend zen) (Zen.OptionT.bind (Prims.parse_int "4") (Prims.parse_int "5") (parse_outpoint inputMsg.data) (totalInputZen inputMsg.utxo)))) (Zen.OptionT.ap (Prims.parse_int "4") (Prims.parse_int "11") (Zen.OptionT.map (Prims.parse_int "4") mkoutput (parse_outputLock inputMsg.data)) (Zen.OptionT.map (Prims.parse_int "11") (mkspend inputMsg.contractHash) (Zen.OptionT.bindLift (Prims.parse_int "2") (Prims.parse_int "9") (Zen.OptionT.bind (Prims.parse_int "4") (Prims.parse_int "5") (parse_outpoint inputMsg.data) (totalInputZen inputMsg.utxo)) issueTokens))) (Zen.Tuple.curry3 Zen.Vector.of_t3 output0)) (mkTx (Prims.parse_int "3")))
end))
