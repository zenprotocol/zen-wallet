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
		public KeyStore KeyStore { get; private set; }
		private DBContext _DBContext;
		private BlockChain.BlockChain _BlockChain;
		private TxHistoryStore _TxHistoryStore;

		public event Action<Types.Transaction, List<Types.Output>> OnMyOutputAdded;

		//dev
		public AssetsManager _AssetsManager;

		public WalletManager(BlockChain.BlockChain blockChain, string dbName)
		{
			_BlockChain = blockChain;
			OwnResource (_BlockChain);

			_AssetsManager = new AssetsManager ();
			_DBContext = new DBContext(dbName);
			OwnResource (_DBContext);

			KeyStore = new KeyStore(_DBContext);
			_TxHistoryStore = new TxHistoryStore(_DBContext);

			_BlockChain.TxMempool.OnAdded += t => {
				_AssetsManager.AddTransactionOutputs(t);
			};

			_BlockChain.TxStore.OnAdded += t =>
			{
				HandleTransaction(t);
			};
		}

		public void SyncHistory()
		{
			var tip = _BlockChain.Tip;

			if (tip == null)
			{
				return;
			}

			var block = tip.Value;

			while (block != null)
			{
				foreach (var transaction in block.transactions)
				{
					HandleTransaction(transaction);
				}

				block = _BlockChain.GetBlock(block.header.parent);
			}
		}

		private void HandleTransaction(Types.Transaction transaction) {
			var myOutputs = IsMine(transaction);

			if (myOutputs.Count > 0)
			{
				_TxHistoryStore.Put(transaction);

				try
				{
					if (OnMyOutputAdded != null)
					{
						OnMyOutputAdded(transaction, myOutputs);
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
			}
		}

		private List<Types.Output> IsMine(Types.Transaction transaction) 
		{
			var myOutputs = new List<Types.Output>();

			foreach (Types.Output output in transaction.outputs) 
			{
				if (KeyStore.IsMatch(output)) {
					myOutputs.Add(output);
				}
			}

			return myOutputs;
		}
	}
}
