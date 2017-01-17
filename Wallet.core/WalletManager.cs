using System;
using System.Collections.Generic;
using Infrastructure;
using Store;
using System.Linq;
using Wallet.core.Store;
using Wallet.core.Data;
using Consensus;
using Microsoft.FSharp.Collections;
using BlockChain.Data;
using BlockChain.Store;

namespace Wallet.core
{
	public class Balances : HashDictionary<List<long>>
	{
	}

	public class WalletManager : ResourceOwner
	{
		private DBContext _DBContext;
		private BlockChain.BlockChain _BlockChain;
		private Store.UTXOStore _UTXOStore;
		private UTXOMem _UTXOMem;
		private KeyStore _KeyStore { get; set; }
		private HashDictionary<bool> _HandledTransactions;
		private BalanceStore _BalanceStore;

		public event Action<HashDictionary<List<long>>> OnNewBalance;

//#if DEBUG
//		public void Reset()
//		{
//			WalletTrace.Information("reset");
//		}
//#endif

		public WalletManager(BlockChain.BlockChain blockChain, string dbName)
		{
			_BlockChain = blockChain;

			_HandledTransactions = new HashDictionary<bool>();
			_DBContext = new DBContext(dbName);
			OwnResource(_DBContext);

			_KeyStore = new KeyStore();
			_UTXOStore = new Store.UTXOStore();
			_UTXOMem = new UTXOMem();
			_BalanceStore = new BalanceStore();

			using (var context = _DBContext.GetTransactionContext())
			{
				WalletTrace.Information($"Keystore contains {_KeyStore.List(context, false).Count} unused, {_KeyStore.List(context, true).Count} used");
			}

			OwnResource(MessageProducer<TxMempool.AddedMessage>.Instance.AddMessageListener(
				new EventLoopMessageListener<TxMempool.AddedMessage>(m =>
				{
					HandleTransaction(m.Transaction, false);
				})
			));

			OwnResource(MessageProducer<TxStore.AddedMessage>.Instance.AddMessageListener(
				new EventLoopMessageListener<TxStore.AddedMessage>(m =>
				{
					HandleTransaction(m.Transaction, true);
				})
			));

			//JsonLoader<Balances>.Instance.FileName = "balances.json";
		}

		public HashDictionary<List<long>> Load()
		{
			var balances = new HashDictionary<List<long>>();

			using (var context = _DBContext.GetTransactionContext())
			{
				foreach (var item in _BalanceStore.All(context))
				{
					balances[item.Key] = item.Value;
				}
			}

			return balances;

			//	return JsonLoader<Balances>.Instance.Value;

			//var balances = new HashDictionary<List<long>>();

			//using (var context = _DBContext.GetTransactionContext())
			//{
			//	foreach (var output in _UTXOStore.All(context).Select(t => t.Value))
			//	{
			//		AddBalance(balances, output);
			//	}
			//}

			//return balances;
		}

		public void Sync()
		{
			_HandledTransactions.Clear();
			var utxoSet = _BlockChain.GetUTXOSet();
			WalletTrace.Information($"loading blockchain's {utxoSet.Count()} utxos");
			var transactions = new List<Types.Transaction>();

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
					transactions.Add(transaction);
				}

				block = _BlockChain.GetBlock(block.header.parent);
			}

			using (var context = _DBContext.GetTransactionContext())
			{
				//_OutpointAssetsStore.RemoveAll(context);
				_UTXOStore.RemoveAll(context);

				foreach (var item in utxoSet)
				{
					if (_KeyStore.Find(context, item.Value, true))
					{
						_UTXOStore.Put(context, item);

						//_AssetsManager.Add(item);
						//AddToRunningBalance(item.Value);
						//if (!myTransactions.Contains(item.Item1.txHash))
						//{
						//	myTransactions.Add(item.Item1.txHash);
						//}
					}
				}

				context.Commit();
			}
				
			foreach (var transaction in transactions)
			{
				HandleTransaction(new Keyed<Types.Transaction>(Merkle.transactionHasher.Invoke(transaction), transaction), true);
			}
		}

		private void AddBalance(HashDictionary<List<long>> balances, Types.Output output, bool isNegative = false)
		{
			if (!balances.ContainsKey(output.spend.asset))
			{
				balances[output.spend.asset] = new List<long>();
			}

			WalletTrace.Information($"adding {output.spend.amount} {AssetsHelper.Find(output.spend.asset)}`");

			balances[output.spend.asset].Add((long)output.spend.amount * (isNegative ? -1 : 1));
		}

		//private void HandleOutput(TransactionContext context, byte[] transaction, Types.Output output, bool confirmed, bool isSent = false)
		//{
		//	byte[] key = new byte[transaction.Length + 1];
		//	transaction.CopyTo(key, 0);
		//	key[transaction.Length] = (byte)i;

		//	if (confirmed)
		//	{
		//		_UTXOStore.Put(context, new Keyed<Types.Output>(key, output));

		//		if (_UTXOMem.ContainsKey(key))
		//		{
		//			_UTXOMem.Remove(key);
		//		}
		//	}
		//	else
		//	{
		//		_UTXOMem[key] = output;
		//	}
		//}

		private void HandleTransaction(Keyed<Types.Transaction> transaction, bool confirmed)
		{
			var handled = _HandledTransactions.ContainsKey(transaction.Key);
			var balances = new HashDictionary<List<long>>();

			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				byte i = 0;
				bool shouldCommit = false;

				foreach (var output in transaction.Value.outputs)
				{
					if (_KeyStore.Find(context, output, true))
					{
						shouldCommit = true;

						byte[] key = new byte[transaction.Key.Length + 1];
						transaction.Key.CopyTo(key, 0);
						key[transaction.Key.Length] = (byte)i;

						if (confirmed)
						{
							_UTXOStore.Put(context, new Keyed<Types.Output>(key, output));

							if (_UTXOMem.ContainsKey(key))
							{
								_UTXOMem.Remove(key);
							}
						}
						else
						{
							_UTXOMem[key] = output;
						}

						if (!handled)
						{
							AddBalance(balances, output);
						}
					}

					i++;
				}

				foreach (var input in transaction.Value.inputs)
				{
					byte[] key = new byte[input.txHash.Length + 1];
					input.txHash.CopyTo(key, 0);
					key[input.txHash.Length] = (byte)input.index;

					Types.Output output = null;

					if (confirmed)
					{
						if (_UTXOMem.ContainsKey(key))
						{
							output = _UTXOMem[key];
							_UTXOMem.Remove(key);
						}
						else if (_UTXOStore.ContainsKey(context, key))
						{
							output = _UTXOStore.Get(context, key).Value;
							_UTXOStore.Remove(context, key);
							shouldCommit = true;
						}
					}
					else if (_UTXOMem.ContainsKey(key))
					{
						output = _UTXOMem[key];
						_UTXOMem.Remove(key);
					}
				}

				if (shouldCommit)
				{
					foreach (var item in balances)
					{
						var values = _BalanceStore.ContainsKey(context, item.Key) ? _BalanceStore.Get(context, item.Key).Value : new List<long>();

						foreach (var _value in item.Value)
						{
							values.Add(_value);
						}

						_BalanceStore.Put(context, new Keyed<List<long>>(item.Key, values));
					}
					context.Commit();
				}
			}

			if (!handled)
			{
				_HandledTransactions[transaction.Key] = true;
				try
				{
					if (OnNewBalance != null)
					{
						OnNewBalance(balances);
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e); //TODO
				}
			}
		}

		public bool Spend(string address, byte[] asset, ulong amount) //TODO: sign it.
		{
			var assets = new SortedSet<Tuple<Types.Outpoint, Types.Output>>(new OutputComparer());

			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				// add confirmed utxos first
				foreach (var utxo in _UTXOStore.All(context))
				{
					if (utxo.Value.spend.asset.SequenceEqual(asset))
					{
						//TODO: this probably should be done using msgpack
						byte[] txHash = new byte[utxo.Key.Length - 1];
						Array.Copy(utxo.Key, txHash, txHash.Length);
						uint index = utxo.Key[utxo.Key.Length - 1];
						var outpoint = new Types.Outpoint(txHash, index);
						assets.Add(new Tuple<Types.Outpoint, Types.Output>(outpoint, utxo.Value));
					}
				}

				// ..then add mem utxos
				foreach (var utxo in _UTXOMem)
				{
					if (utxo.Value.spend.asset.SequenceEqual(asset))
					{
						//TODO: this probably should be done using msgpack
						byte[] txHash = new byte[utxo.Key.Length - 1];
						Array.Copy(utxo.Key, txHash, txHash.Length);
						uint index = utxo.Key[utxo.Key.Length - 1];
						var outpoint = new Types.Outpoint(txHash, index);
						assets.Add(new Tuple<Types.Outpoint, Types.Output>(outpoint, utxo.Value));
					}
				}
			}

			var inputs = new List<Types.Outpoint>();
			ulong total = 0;

			foreach (var item in assets)
			{
				inputs.Add(item.Item1);
				total += item.Item2.spend.amount;

				if (total >= amount)
				{
					break;
				}
			}

			if (total < amount)
			{
				return false;
			}

			var outputs = new List<Types.Output>();

			outputs.Add(new Types.Output(Types.OutputLock.NewPKLock(Key.FromBase64String(address)), new Types.Spend(Tests.zhash, amount)));

			if (total - amount > 0)
			{
				outputs.Add(new Types.Output(Types.OutputLock.NewPKLock(GetUnsendKey(true).Address), new Types.Spend(Tests.zhash, total - amount)));
			}

			var hashes = new List<byte[]>();
			var version = (uint)1;

			Types.Transaction transaction = new Types.Transaction(version,
				ListModule.OfSeq(inputs),
				ListModule.OfSeq(hashes),
				ListModule.OfSeq(outputs),
				null);

			//TODO:		Consensus.TransactionValidation.signTx(
			return _BlockChain.HandleNewTransaction(transaction);
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

		public Key GetUnsendKey(bool isChange = false)
		{
			Key result;

			using (var context = _DBContext.GetTransactionContext())
			{
				result = _KeyStore.GetUnsendKey(context, isChange);
				context.Commit();
			}

			return result;
		}

		//public void Used(Key key)
		//{
		//	using (var context = _DBContext.GetTransactionContext())
		//	{
		//		_KeyStore.Used(context, key);
		//		context.Commit();
		//	}
		//}

		public List<Key> ListKeys(bool? used = null, bool? isChange = null)
		{
			using (var context = _DBContext.GetTransactionContext())
			{
				return _KeyStore.List(context, used, isChange);
			}
		}

								//private void InvalidateKeys(TransactionContext context, params Key[] keys)
		//{
		//	foreach (var key in keys)
		//	{
		//		if (!key.Used)
		//		{
		//			WalletTrace.Information($"Key used: {key.PrivateAsString}");
		//			_KeyStore.Used(context, key);
		//		}
		//	}
		//}
	}
}
