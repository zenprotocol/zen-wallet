using System;
using System.Collections.Generic;
using Infrastructure;
using Store;
using System.Linq;
using Wallet.core.Store;
using Wallet.core.Data;
using Consensus;

namespace Wallet.core
{
	public class WalletManager : ResourceOwner
	{
		private const string DB_NAME = "wallet";
		public  KeyStore KeyStore { get; private set; }
		private DBContext _DBContext;
		private BlockChain.BlockChain _BlockChain;
		private TxHistoryStore _TxHistoryStore;
		private AssetsManager _AssetsManager;

		public WalletManager(BlockChain.BlockChain blockChain)
		{
			_BlockChain = blockChain;
			_AssetsManager = new AssetsManager ();
			_DBContext = new DBContext(DB_NAME);
			OwnResource (_DBContext);

			KeyStore = new KeyStore(_DBContext);
			_TxHistoryStore = new TxHistoryStore(_DBContext);

			_BlockChain.OnAddedToMempool += t => {
				_AssetsManager.AddTransactionOutputs(t);
			};

			_BlockChain.OnAddedToStore += t => {
				if (IsMine(t)) {
					_TxHistoryStore.Put(t);
				}
				_AssetsManager.AddTransactionOutputs(t);
			};
		}

		private bool IsMine(Types.Transaction transaction) 
		{
			foreach (Types.Output output in transaction.outputs) 
			{
				if (KeyStore.IsMatch(output)) {
					return true;
				}
			}

			return false;
		}
	}
}
