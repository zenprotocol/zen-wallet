//using System;
//using Store;
//using Consensus;
//using MsgPack;
//using MsgPack.Serialization;

//namespace BlockChain.Store
//{
//	public class UTXOStore : MsgPackStore<UTXO>
//	{
//		public UTXOStore() : base("utxo")
//		{
//			UTXOSerializer.Register();
//		}
//	}

//	public enum UTXOState {
//		Confirmed,
//		Unconfirmed,
//	}

//	public class UTXO  {
//		public Types.Output Output { get; set; }
//		public UTXOState UTXOState { get; set; }
//	}

//	public class UTXOSerializer : MessagePackSerializer<UTXO>
//	{
//		public static UTXOSerializer Register()
//		{
//			return Register(SerializationContext.Default);
//		}

//		public static UTXOSerializer Register(SerializationContext context)
//		{
//			var serializer = new UTXOSerializer(context);
//			context.Serializers.RegisterOverride(serializer);

//			return serializer;
//		}

//		private UTXOSerializer(SerializationContext context) : base(context)
//		{
//		}

//		protected override void PackToCore(Packer packer, UTXO objectTree)
//		{
//			packer.Pack(new Tuple<UTXOState, Types.Output>(objectTree.UTXOState, );
//			packer.PackRawBody(Consensus.Serialization.context.GetSerializer<Types.Output>().PackSingleObject(objectTree.Output));
//		}

//		protected override UTXO UnpackFromCore(Unpacker unpacker)
//		{
//			return new UTXO()
//			{
//				UTXOState = (UTXOState)unpacker.Unpack<UTXOState>(),
//				Output = Consensus.Serialization.context.GetSerializer<Types.Output>().UnpackFrom(unpacker)
//			};
//		}
//	}


//	//public class UTXOMiroringCache
//	//{
//	//	private readonly UTXOStore _UTXOStore;

//	//	public UTXOMiroringCache() 
//	//	{
//	//		_UTXOStore = new UTXOStore();
//	//	}
	
//	//	private void Put() {
//	//	}

//	//	private void Get() {
//	//	}


//	//	private class UTXOStore : Store<Types.Output>
//	//	{
//	//		public UTXOStore() : base("utxo") { }

//	//		protected override StoredItem<Types.Output> Wrap(Types.Output item)
//	//		{
//	//			var data = Merkle.serialize<Types.Output>(item);
//	//			var key = Merkle.outputHasher.Invoke(item);

//	//			return new StoredItem<Types.Output>(item, key, data);
//	//		}

//	//		protected override Types.Output FromBytes(byte[] data, byte[] key)
//	//		{
//	//			//TODO: encap unpacking in Consensus, so referencing MsgPack would become unnecessary 
//	//			return Serialization.context.GetSerializer<Types.Output>().UnpackSingleObject(data);
//	//		}
//	//	}
//	//}
//}