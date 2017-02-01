//using Consensus;
//using BlockChain.Store;
//using Store;
//using MsgPack.Serialization;
//using MsgPack;

//namespace Wallet.core.Store
//{
//	public enum TxTypeEnum
//	{
//		NewTx = 1,
//		RefreshContract = 2,
//		ExpireContract = 3,
//	}

//	public class Tx
//	{
//		public TxTypeEnum TxType { get; set; }
//		public byte[] Hash { get; set; }
//		public byte[] Key { get; set; }
//	}

//	public class TxSerializer : MessagePackSerializer<Tx>
//	{
//		public TxSerializer(SerializationContext ownerContext) : base(ownerContext) { }

//		protected override void PackToCore(Packer packer, Tx objectTree)
//		{
//			packer.Pack(objectTree.Key);
//			packer.Pack(objectTree.Hash);
//			packer.Pack(objectTree.TxType);
//		}

//		protected override Tx UnpackFromCore(Unpacker unpacker)
//		{
//			var tx = new Tx();

//			tx.Key = unpacker.Unpack<byte[]>();
//			tx.Hash = unpacker.Unpack<byte[]>();
//			tx.TxType = unpacker.Unpack<TxTypeEnum>();

//			return tx;
//		}
//	}

//	public class TxStore : MsgPackStore<Tx>
//	{
//		public TxStore() : base("txstore") {
//			new TxSerializer(_Context);
//		}
//	}
//}