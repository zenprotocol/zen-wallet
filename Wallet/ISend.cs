//using System;
//using System.Threading.Tasks;
//using System.Collections.Generic;
//
//namespace Wallet
//{
//	public struct SignedTx
//	{
//		public string bareTx;
//		public List<string> signatures;
//	}
//
//	public struct TxMetadata
//	{
//		public string assetId;
//		public int amount;
//		public string destination;
//		public int fee;
//		public List<string> inputIds;
//		public List<string> changeAddress;
//	}
//
//	public interface ISend
//	{
//		Task<Tuple<SignedTx, TxMetadata>> RequestSend (string assetId, int amount, string destination, int fee);
//	}
//}
//
