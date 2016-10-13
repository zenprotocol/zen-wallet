//using System;
//using System.Threading.Tasks;
//
//namespace Wallet
//{
//	public class SendStub : ISend
//	{
//		public SendStub ()
//		{
//		}
//
//		public async Task<Tuple<SignedTx, TxMetadata>> RequestSend (string assetId, int amount, string destination, int fee) {
//			System.Threading.Thread.Sleep (10000);
//
//			SignedTx signedTx = new SignedTx (); 
//			TxMetadata txMetadata = new TxMetadata ();
//
//			Tuple<SignedTx, TxMetadata> x = new Tuple<SignedTx, TxMetadata>(signedTx, txMetadata);
//
//			Task.Delay (1000);
//		}
//	}
//}
//
