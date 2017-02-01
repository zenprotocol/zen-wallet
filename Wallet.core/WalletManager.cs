using System;
using System.Collections.Generic;
using Infrastructure;
using Store;
using System.Linq;
using Wallet.core.Store;
using Wallet.core.Data;
using Consensus;
using BlockChain.Data;
using BlockChain;

namespace Wallet.core
{
	public class WalletManager : ResourceOwner
	{
		private DBContext _DBContext;
		private BlockChain.BlockChain _BlockChain;
		private TxBalancesStore _TxBalancesStore;
		private KeyStore _KeyStore { get; set; }
		private List<Key> _Keys;

		public WalletBalances WalletBalances { get; private set; }

		private EventLoopMessageListener<TxAddedMessage> _BlockChainTxListener;
		private EventLoopMessageListener<BkAddedMessage> _BlockChainBkListener;

		public WalletManager(BlockChain.BlockChain blockChain, string dbName)
		{
			_BlockChain = blockChain;

			_DBContext = new DBContext(dbName);
			OwnResource(_DBContext);

			_KeyStore = new KeyStore();
			_TxBalancesStore = new TxBalancesStore();

			WalletBalances = new WalletBalances();

			_BlockChainTxListener = new EventLoopMessageListener<TxAddedMessage>(HandleTx, false);
			_BlockChainBkListener = new EventLoopMessageListener<BkAddedMessage>(OnBlock, false);

			OwnResource(MessageProducer<TxAddedMessage>.Instance.AddMessageListener(_BlockChainTxListener));
			OwnResource(MessageProducer<BkAddedMessage>.Instance.AddMessageListener(_BlockChainBkListener));

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_Keys = _KeyStore.List(dbTx);
				_TxBalancesStore.All(dbTx).ToList().ForEach(t =>
				{
					var balances = _TxBalancesStore.Balances(dbTx, t.Key);
					WalletBalances.Add(new UpdateInfoItem(t, balances));
				});
			}
		}

		public void OnBlock(BkAddedMessage m)
		{
			var txs = new List<TransactionValidation.PointedTransaction>();

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				m.Bk.transactions.ToList().ForEach(tx=>txs.Add(_BlockChain.GetPointedTransaction(dbTx, tx)));
			}

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				txs.ForEach(tx => HandleTx(dbTx, tx));
			}
		}

		public void Import()
		{
			var utxoSetTxs = _BlockChain.GetUTXOSet(IsMatch); // TODO: use linq, return enumerator, remove predicate

			WalletBalances.Clear();
			using (var dbTx = _DBContext.GetTransactionContext())
			{
				dbTx.Transaction.SynchronizeTables(TxBalancesStore.INDEXES);
				_TxBalancesStore.Reset(dbTx);

				foreach (var tx in utxoSetTxs)
				{
					var balances = new HashDictionary<long>();

					tx.Key.Value.outputs.Where(IsMatch).ToList().ForEach(o => Add(balances, o));
					_TxBalancesStore.Put(dbTx, tx.Key, balances, TxStateEnum.Confirmed);
					WalletBalances.Add(new UpdateInfoItem(tx.Key, balances));
				}

				_BlockChain.TxMempool.GetAll().ForEach(ptx => HandleTx(ptx));

				dbTx.Commit();
			}

			_BlockChainBkListener.Start();
			_BlockChainTxListener.Start();

			MessageProducer<IWalletMessage>.Instance.PushMessage(new ResetMessage());
		}


	//	public void Sync()
	//	{
			//_HandledTransactions.Clear();
			//var utxoSet = _BlockChain.GetUTXOSet();
			//WalletTrace.Information($"loading blockchain's {utxoSet.Count()} utxos");
			//var transactions = new List<Types.Transaction>();


			//var tipItr = _BlockChain.Tip.Key;

			//while (tipItr != null && !tipItr.SequenceEqual(new byte[] { }))
			//{
			//	using (var context = _DBContext.GetTransactionContext()) // TODO: encap
			//	{
			//		foreach (var transaction in _BlockChain.BlockStore.Transactions(context, tipItr))
			//		{
			//			transactions.Add(transaction.Value);
			//		}
			//	}

			//	tipItr = _BlockChain.GetBlockHeader(tipItr).parent;
			//}

			//using (var context = _DBContext.GetTransactionContext())
			//{
			//	//_OutpointAssetsStore.RemoveAll(context);
			//	_UTXOStore.RemoveAll(context);
			//	_BalanceStore.RemoveAll(context);

			//	foreach (var item in utxoSet)
			//	{
			//		if (_KeyStore.Find(context, item.Value, true))
			//		{
			//			_UTXOStore.Put(context, item);

			//			//_AssetsManager.Add(item);
			//			//AddToRunningBalance(item.Value);
			//			//if (!myTransactions.Contains(item.Item1.txHash))
			//			//{
			//			//	myTransactions.Add(item.Item1.txHash);
			//			//}
			//		}
			//	}

			//	context.Commit();
			//}
				
			//foreach (var transaction in transactions)
			//{
			//	HandleTransaction(new Keyed<Types.Transaction>(Merkle.transactionHasher.Invoke(transaction), transaction), true);
			//}
	//	}

		public bool Spend(string address, byte[] asset, ulong amount) //TODO: sign it.
		{
			//var assets = new SortedSet<Tuple<Types.Outpoint, Types.Output>>(new OutputComparer());

			//using (TransactionContext context = _DBContext.GetTransactionContext())
			//{
			//	// add confirmed utxos first
			//	foreach (var utxo in _UTXOStore.All(context))
			//	{
			//		if (utxo.Value.spend.asset.SequenceEqual(asset))
			//		{
			//			//TODO: this probably should be done using msgpack
			//			byte[] txHash = new byte[utxo.Key.Length - 1];
			//			Array.Copy(utxo.Key, txHash, txHash.Length);
			//			uint index = utxo.Key[utxo.Key.Length - 1];
			//			var outpoint = new Types.Outpoint(txHash, index);
			//			assets.Add(new Tuple<Types.Outpoint, Types.Output>(outpoint, utxo.Value));
			//		}
			//	}

			//	// ..then add mem utxos
			//	foreach (var utxo in _UTXOMem)
			//	{
			//		if (utxo.Value.spend.asset.SequenceEqual(asset))
			//		{
			//			//TODO: this probably should be done using msgpack
			//			byte[] txHash = new byte[utxo.Key.Length - 1];
			//			Array.Copy(utxo.Key, txHash, txHash.Length);
			//			uint index = utxo.Key[utxo.Key.Length - 1];
			//			var outpoint = new Types.Outpoint(txHash, index);
			//			assets.Add(new Tuple<Types.Outpoint, Types.Output>(outpoint, utxo.Value));
			//		}
			//	}
			//}

			//var inputs = new List<Types.Outpoint>();
			//ulong total = 0;

			//foreach (var item in assets)
			//{
			//	inputs.Add(item.Item1);
			//	total += item.Item2.spend.amount;

			//	if (total >= amount)
			//	{
			//		break;
			//	}
			//}

			//if (total < amount)
			//{
			//	return false;
			//}

			//var outputs = new List<Types.Output>();

			//outputs.Add(new Types.Output(Types.OutputLock.NewPKLock(Key.FromBase64String(address)), new Types.Spend(Tests.zhash, amount)));

			//if (total - amount > 0)
			//{
			//	outputs.Add(new Types.Output(Types.OutputLock.NewPKLock(GetUnusedKey(true).Address), new Types.Spend(Tests.zhash, total - amount)));
			//}

			//var hashes = new List<byte[]>();
			//var version = (uint)1;

			//Types.Transaction transaction = new Types.Transaction(version,
			//	ListModule.OfSeq(inputs),
			//	ListModule.OfSeq(hashes),
			//	ListModule.OfSeq(outputs),
			//	null);

			////TODO:		Consensus.TransactionValidation.signTx(
			//return _BlockChain.HandleNewTransaction(transaction);
			return false;
		}

		public bool AddKey(string base64EncodedPrivateKey)
		{
			bool result = false;

			using (var context = _DBContext.GetTransactionContext())
			{
				result = _KeyStore.AddKey(context, base64EncodedPrivateKey);
				context.Commit();
			}

			return result;
		}

		public Key GetUnusedKey()
		{
			Key result;

			using (var context = _DBContext.GetTransactionContext())
			{
				result = _KeyStore.GetUnusedKey(context);
				context.Commit();
			}

			return result;
		}

		public List<Key> ListKeys(bool? used = null, bool? isChange = null)
		{
			using (var context = _DBContext.GetTransactionContext())
			{
				return _KeyStore.List(context, used, isChange);
			}
		}

		private void HandleTx(TxAddedMessage m)
		{
			HandleTx(m.Tx);
		}

		private void HandleTx(TransactionValidation.PointedTransaction ptx)
		{
			using (var context = _DBContext.GetTransactionContext())
			{
				HandleTx(context, ptx);
			}
		}

		private void HandleTx(TransactionContext dbTx, TransactionValidation.PointedTransaction ptx)
		{
			var balances = new HashDictionary<long>();

			ptx.outputs.Where(IsMatch).ToList().ForEach(o => Add(balances, o));
			ptx.pInputs.Where(IsMatch).ToList().ForEach(o => Add(balances, o));

			if (balances.Count > 0)
			{
				var tx = TransactionValidation.unpoint(ptx);
				var keyedTx = new Keyed<Types.Transaction>(Merkle.transactionHasher.Invoke(tx), tx);

				_TxBalancesStore.Put(dbTx, keyedTx, balances, TxStateEnum.Unconfirmed);
				WalletBalances.Add(new UpdateInfoItem(keyedTx, balances));
			}
		}

		private bool IsMatch(Tuple<Types.Outpoint, Types.Output> pointedOutput)
		{
			return IsMatch(pointedOutput.Item2);
		}

		private bool IsMatch(Types.Output output)
		{
			foreach (var key in _Keys)
			{
				if (key.IsMatch(output.@lock))
				{
					return true;
				}
			}

			return false;
		}

		private void Add(HashDictionary<long> balances, Types.Output output, bool isSpending = false)
		{
			if (!balances.ContainsKey(output.spend.asset))
			{
				balances[output.spend.asset] = 0;
			}

			balances[output.spend.asset] += isSpending  ? (long)output.spend.amount : (long)output.spend.amount * -1;
		}

		private void Add(HashDictionary<long> balances, Tuple<Types.Outpoint, Types.Output> pointedOutput)
		{
			Add(balances, pointedOutput.Item2, true);
		}
	}
}