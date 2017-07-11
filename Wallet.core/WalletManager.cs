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
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Sodium;

namespace Wallet.core
{
    public class WalletManager : ResourceOwner
    {
        private DBContext _DBContext;
        private BlockChain.BlockChain _BlockChain;
        private TxStore _TxStore;
        private KeyStore _KeyStore { get; set; }
        private List<Key> _Keys;

        //TODO: consider not using thread loops - * watchout from dbreeze threading limitation
        private EventLoopMessageListener<BlockChainMessage> _BlockChainListener;

        public List<TxDelta> TxDeltaList { get; private set; }
        public AssetsMetadata AssetsMetadata { get; private set; }

        public event Action<List<TxDelta>> OnItems;

        public WalletManager(BlockChain.BlockChain blockChain, string dbName)
        {
            _BlockChain = blockChain;

            _DBContext = new DBContext(dbName);

            _KeyStore = new KeyStore();
            _TxStore = new TxStore();

            TxDeltaList = new List<TxDelta>();
            AssetsMetadata = new AssetsMetadata();

            _BlockChainListener = new EventLoopMessageListener<BlockChainMessage>(OnBlockChainMessage, "Wallet Consumer");
            OwnResource(MessageProducer<BlockChainMessage>.Instance.AddMessageListener(_BlockChainListener));
            OwnResource(_DBContext);

            using (var dbTx = _DBContext.GetTransactionContext())
            {
                _Keys = _KeyStore.List(dbTx);
                var purgeList = new List<ulong>();

                _TxStore.All(dbTx).ToList().ForEach(item =>
                {
                    switch (item.Item2.TxState)
                    {
                        case TxStateEnum.Confirmed:
                            foreach (var output in item.Item2.Tx.outputs)
                            {
                                AssetsMetadata.GetMetadata(output.spend.asset);
                            }
                            TxDeltaList.Add(new TxDelta(item.Item2.TxState, item.Item2.TxHash, item.Item2.Tx, item.Item2.AssetDeltas, item.Item2.DateTime));
                            break;
                        case TxStateEnum.Unconfirmed:
                            purgeList.Add(item.Item1);
                            //TODO: implement 'smarter' mode: only after the node has been synced and is up-to-date, try to revalidate
                            //_BlockChain.HandleTransaction(txData.Tx);
                            break;
                    }

                    foreach (var key in purgeList)
                    {
                        _TxStore.Remove(dbTx, key);
                    }
                });
            }
        }

        public List<Key> GetKeys()
        {
            using (var context = _DBContext.GetTransactionContext())
            {
                return _KeyStore.List(context);
            }
        }

        /// <summary>
        /// Imports wallet file 
        /// </summary>
        public void Import(Key key)
        {
            _BlockChainListener.Pause();

            using (var context = _DBContext.GetTransactionContext())
            {
                _KeyStore.AddKey(context, key.PrivateAsString);
                context.Commit();
            }

			//TODO: store the key in DB?
            _Keys.Add(key);

            HashDictionary<List<Types.Output>> txOutputs;
            HashDictionary<Types.Transaction> txs;

			var result = new GetUTXOSetAction() { Predicate = IsMatch }.Publish().Result;

			txOutputs = result.Item1;
			txs = result.Item2;

            TxDeltaList.Clear();

            using (var dbTx = _DBContext.GetTransactionContext())
            {
                //dbTx.Transaction.SynchronizeTables(TxBalancesStore.INDEXES);
                _TxStore.Reset(dbTx);

                foreach (var item in txOutputs)
                {
                    var assetDeltas = new AssetDeltas();

                    foreach (var output in item.Value)
                    {
                        AddOutput(assetDeltas, output);
                        AssetsMetadata.GetMetadata(output.spend.asset);
                    }

                    _TxStore.Put(dbTx, item.Key, txs[item.Key], assetDeltas, TxStateEnum.Confirmed);
                    TxDeltaList.Add(new TxDelta(TxStateEnum.Confirmed, item.Key, txs[item.Key], assetDeltas));
                }

                _BlockChain.memPool.TxPool.ToList().ForEach(t => HandleTx(dbTx, t.Key, t.Value, TxDeltaList, TxStateEnum.Unconfirmed));

                dbTx.Commit();
            }

            if (OnItems != null)
                OnItems(TxDeltaList);

            _BlockChainListener.Continue();
        }

        private void OnBlockChainMessage(BlockChainMessage m)
        {
            var deltas = new List<TxDelta>();

            using (var dbTx = _DBContext.GetTransactionContext())
            {
                if (m is TxMessage)
                {
                    var newTxStateMessage = m as TxMessage;
                    HandleTx(dbTx, newTxStateMessage.TxHash, newTxStateMessage.Ptx, deltas, newTxStateMessage.State);
                    dbTx.Commit();
                }
                else if (m is BlockMessage)
                {
                    foreach (var item in (m as BlockMessage).PointedTransactions)
                    {
                        HandleTx(dbTx, item.Key, item.Value, deltas, TxStateEnum.Confirmed);
                    }

                    dbTx.Commit();
                }
            }

            if (deltas.Count > 0)
            {
                TxDeltaList.AddRange(deltas);

                if (OnItems != null)
                    OnItems(deltas);
            }
        }

        void HandleTx(TransactionContext dbTx, byte[] txHash, TransactionValidation.PointedTransaction ptx, List<TxDelta> deltas, TxStateEnum txState)
        {
            var isValid = txState != TxStateEnum.Invalid;
            var _deltas = new AssetDeltas();

            if (!isValid)
            {
                foreach (var item in _TxStore.All(dbTx).Where(t => t.Item2.TxHash.SequenceEqual(txHash)))
                {
                    item.Item2.TxState = txState;
                    //TODO: handle ui consistency
                    _TxStore.Put(dbTx, item.Item1, item.Item2);
                }
                return;
            }

            ptx.outputs.Where(IsMatch).ToList().ForEach(o =>
            {
                AddOutput(_deltas, o, !isValid);
                AssetsMetadata.GetMetadata(o.spend.asset);
            });

            ptx.pInputs.ToList().ForEach(pInput =>
            {
                var key = GetKey(pInput.Item2);

                if (key != null)
                {
                    AddOutput(_deltas, pInput.Item2, isValid);
                    _KeyStore.Used(dbTx, key, true);
                }
            });

            if (_deltas.Count > 0)
            {
                var tx = TransactionValidation.unpoint(ptx);

                _TxStore.Put(dbTx, txHash, tx, _deltas, txState);
                deltas.Add(new TxDelta(txState, txHash, tx, _deltas));
            }
        }

        private bool IsMatch(Tuple<Types.Outpoint, Types.Output> pointedOutput)
        {
            return IsMatch(pointedOutput.Item2);
        }

        private bool IsMatch(Types.Output output)
        {
            return GetKey(output) != null;
        }

        private Key GetKey(Types.Output output)
        {
            foreach (var key in _Keys)
            {
                if (key.Address.IsMatch(output.@lock))
                {
                    return key;
                }
            }

            return null;
        }

        private void AddOutput(AssetDeltas balances, Types.Output output, bool isSpending = false)
        {
            if (!balances.ContainsKey(output.spend.asset))
            {
                balances[output.spend.asset] = 0;
            }

            balances[output.spend.asset] += isSpending ? -1 * (long)output.spend.amount : (long)output.spend.amount;
        }

        //    public void Sync()
        //    {
        //_HandledTransactions.Clear();
        //var utxoSet = _BlockChain.GetUTXOSet();
        //WalletTrace.Information($"loading blockchain's {utxoSet.Count()} utxos");
        //var transactions = new List<Types.Transaction>();


        //var tipItr = _BlockChain.Tip.Key;

        //while (tipItr != null && !tipItr.SequenceEqual(new byte[] { }))
        //{
        //    using (var context = _DBContext.GetTransactionContext()) // TODO: encap
        //    {
        //        foreach (var transaction in _BlockChain.BlockStore.Transactions(context, tipItr))
        //        {
        //            transactions.Add(transaction.Value);
        //        }
        //    }

        //    tipItr = _BlockChain.GetBlockHeader(tipItr).parent;
        //}

        //using (var context = _DBContext.GetTransactionContext())
        //{
        //    //_OutpointAssetsStore.RemoveAll(context);
        //    _UTXOStore.RemoveAll(context);
        //    _BalanceStore.RemoveAll(context);

        //    foreach (var item in utxoSet)
        //    {
        //        if (_KeyStore.Find(context, item.Value, true))
        //        {
        //            _UTXOStore.Put(context, item);

        //            //_AssetsManager.Add(item);
        //            //AddToRunningBalance(item.Value);
        //            //if (!myTransactions.Contains(item.Item1.txHash))
        //            //{
        //            //    myTransactions.Add(item.Item1.txHash);
        //            //}
        //        }
        //    }

        //    context.Commit();
        //}

        //foreach (var transaction in transactions)
        //{
        //    HandleTransaction(new Keyed<Types.Transaction>(Merkle.transactionHasher.Invoke(transaction), transaction), true);
        //}
        //    }

        /// <summary>
        /// get a set of outpoints with matching keys using greedy algorithm 
        /// </summary>
        /// <returns>false if could not satisfy</returns>
        /// <param name="asset">Asset.</param>
        /// <param name="amount">Amount.</param>
        private bool Require(TransactionContext dbTx, byte[] asset, ulong amount, out ulong change, Assets assets)
        {
            var matchingAssets = new Assets();

            var spendableOutputs = new List<Types.Output>();

            _TxStore.All(dbTx).Select(t=>t.Item2).ToList().ForEach(txData =>
            {
                uint idx = 0;
                txData.Tx.outputs.ToList().ForEach(o =>
                {
                    if (o.spend.asset.SequenceEqual(asset))
                    {
                        var key = GetKey(o);

                        if (key != null)
                        {
                            if (txData.TxState != TxStateEnum.Invalid)
                            {
                                matchingAssets.Add(new Asset()
                                {
                                    Key = key,
                                    TxState = txData.TxState,
                                    Outpoint = new Types.Outpoint(txData.TxHash, idx),
                                    Output = o
                                });
                            }
                        }
                    }
                    idx++;
                });
            });

            var unspentMatchingAssets = new Assets();

			foreach (Asset matchingAsset in matchingAssets)
			{
				bool canSpend = false;
				switch (matchingAsset.TxState)
				{
					case TxStateEnum.Confirmed:
						var isConfirmedUTXOExist = new GetIsConfirmedUTXOExistAction() { Outpoint = matchingAsset.Outpoint }.Publish().Result;

						canSpend = isConfirmedUTXOExist &&
							!_BlockChain.memPool.TxPool.ContainsOutpoint(matchingAsset.Outpoint);
						break;
					case TxStateEnum.Unconfirmed:
						canSpend = !_BlockChain.memPool.TxPool.ContainsOutpoint(matchingAsset.Outpoint) &&
							_BlockChain.memPool.TxPool.Contains(matchingAsset.Outpoint.txHash);
						break;
				}

				WalletTrace.Information($"require: output with amount {matchingAsset.Output.spend.amount} spendable: {canSpend}");

				if (canSpend)
				{
					unspentMatchingAssets.Add(matchingAsset);
				}
			}

            ulong total = 0;

            foreach (var unspentMatchingAsset in unspentMatchingAssets)
            {
                if (total >= amount)
                {
                    break;
                }

                assets.Add(unspentMatchingAsset);
                total += unspentMatchingAsset.Output.spend.amount;
            }

            change = total - amount;
            return total >= amount;
        }

        public bool CanSpend(byte[] asset, ulong amount)
        {
            using (TransactionContext dbTx = _DBContext.GetTransactionContext())
            {
                ulong change;
                var assets = new Assets();

                return Require(dbTx, asset, amount, out change, assets);
            }
        }

        public bool Parse(byte[] rawTxBytes, out Types.Transaction tx)
        {
            try
            {
                tx = Serialization.context.GetSerializer<Types.Transaction>().UnpackSingleObject(rawTxBytes);
                return true;
            }
            catch
            {
                tx = null;
                return false;
            }
        }

        public bool Sign(Address address, byte[] asset, ulong amount, out Types.Transaction signedTx)
        {
            var output = new Types.Output(address.GetLock(), new Types.Spend(asset, amount));
            return Sign(output, asset, amount, out signedTx);
        }

        bool Sign(Types.Output output, byte[] asset, ulong amount, out Types.Transaction signedTx)
        {
            ulong change;
            var assets = new Assets();

            var outputs = new List<Types.Output>();

            using (TransactionContext dbTx = _DBContext.GetTransactionContext())
            {
                if (!Require(dbTx, asset, amount, out change, assets))
                {
                    signedTx = null;
                    return false;
                }
                else if (change > 0)
                {
                    Key key;

                    if (_KeyStore.GetUnusedKey(dbTx, out key, true))
                    {
                        _Keys.Add(key);
                        dbTx.Commit();
                    }
            
                    outputs.Add(new Types.Output(key.Address.GetLock(), new Types.Spend(asset, change)));
                }
            }

            outputs.Add(output);

            signedTx = TransactionValidation.signTx(new Types.Transaction(
                1,
                ListModule.OfSeq(assets.Select(t => t.Outpoint)),
                ListModule.OfSeq(new List<byte[]>()),
                ListModule.OfSeq(outputs),
                null), ListModule.OfSeq(assets.Select(i => i.Key.Private)));

            return true;
        }

        /// <summary>
        /// Constract and sign a transaction activating a contract
        /// </summary>
        /// <returns>The sign.</returns>
        /// <param name="address">Address.</param>
        /// <param name="asset">Asset.</param>
        /// <param name="amount">Amount.</param>
        public bool SacrificeToContract(byte[] code, ulong zenAmount, out Types.Transaction signedTx, byte[] secureTokenHash = null)
        {
            ulong change;
            var assets = new Assets();

            var outputs = new List<Types.Output>();

            using (TransactionContext dbTx = _DBContext.GetTransactionContext())
            {
                if (!Require(dbTx, Tests.zhash, zenAmount, out change, assets))
                {
                    signedTx = null;
                    return false;
                }
                else if (change > 0)
                {
                    Key key;

                    if (_KeyStore.GetUnusedKey(dbTx, out key, true))
                    {
                        _Keys.Add(key);
                        dbTx.Commit();
                    }

                    outputs.Add(new Types.Output(key.Address.GetLock(), new Types.Spend(Tests.zhash, change)));
                }

                if (secureTokenHash != null)
                {
					ulong secureTokenChange;
                   // var secureTokenAssets = new Assets();

                    if (!Require(dbTx, secureTokenHash, 1, out secureTokenChange, assets))
					{
						signedTx = null;
						return false;
					}
					else if (secureTokenChange > 0)
					{
						Key key;

						if (_KeyStore.GetUnusedKey(dbTx, out key, true))
						{
							_Keys.Add(key);
							dbTx.Commit();
						}

						outputs.Add(new Types.Output(key.Address.GetLock(), new Types.Spend(secureTokenHash, secureTokenChange)));
					}
				}
            }

            var output = new Types.Output(
                Types.OutputLock.NewContractSacrificeLock(
                    new Types.LockCore(0, ListModule.OfSeq(new byte[][] { }))
                ),
                new Types.Spend(Tests.zhash, zenAmount)
            );

            outputs.Add(output);

            if (secureTokenHash != null)
            {
                outputs.Add(new Types.Output(Types.OutputLock.NewContractLock(Merkle.innerHash(code), new byte[] { }), new Types.Spend(secureTokenHash, 1)));
            }

            signedTx = TransactionValidation.signTx(new Types.Transaction(
                1,
                ListModule.OfSeq(assets.Select(t => t.Outpoint)),
                ListModule.OfSeq(new List<byte[]>()),
                ListModule.OfSeq(outputs),
                new Microsoft.FSharp.Core.FSharpOption<Types.ExtendedContract>(
                    Types.ExtendedContract.NewContract(new Types.Contract(code, new byte[] { }, new byte[] { }))
                )
              ), ListModule.OfSeq(assets.Select(i => i.Key.Private)));

            return true;
        }

		public bool SendContract(byte[] contractHash, byte[] data, out Types.Transaction autoTx)
		{
			var result = new ExecuteContractAction() { ContractHash = contractHash, Message = data }.Publish().Result;

			autoTx = result.Item2;

			return result.Item1 && autoTx != null;
        }

        public Key GetUnusedKey()
        {
            Key key;

            using (var context = _DBContext.GetTransactionContext())
            {
                if (_KeyStore.GetUnusedKey(context, out key))
                {
                    _Keys.Add(key);
                    context.Commit();
                }
            }

            return key;
        }

        public bool IsContractActive(byte[] contractHash)
        {
			return new GetIsContractActiveAction(contractHash).Publish().Result;
        }

		public bool SignData(byte[] publicKey, byte[] data, out byte[] result)
		{
			var key = _Keys.Find(t => t.Public.SequenceEqual(publicKey));

            if (key == null)
            {
                result = null;
                return false;
            }
            else
            {
                result = PublicKeyAuth.SignDetached(data, key.Private);
                return true;
            }
		}
    }
}