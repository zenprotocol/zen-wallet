using System;
using Consensus;
using System.Linq;
using Store;
using System.Collections.Generic;
using Microsoft.FSharp.Collections;
using BlockChain.Data;

namespace BlockChain.Store
{
	public enum LocationEnum
	{
		Main = 0,
		Branch = 1,
		Orphans = 2
	}

    public class Block
    {
        public Types.BlockHeader BlockHeader { get; set; }
        public List<byte[]> TxHashes { get; set; }
    }

	public class Transaction
	{
		public Types.Transaction Tx { get; set; }
        public bool InMainChain { get; set; }
	}

    public class TransactionsStore : ConsensusTypeStore<Transaction>
    {
        public TransactionsStore(string tableName) : base(tableName)
        {
        }

        public void Put(
            TransactionContext transactionContext,
            byte[] txHash,
            Types.Transaction tx,
            bool inMainChain)
        {
            Put(transactionContext, txHash, new Transaction { Tx = tx, InMainChain = inMainChain });
        }
    }

	public class BlockStore : ConsensusTypeStore<Block>
	{
		private const string BLOCK_HEADERS = "bk-headers";
		private const string CHILDREN = "bk-children";
		private const string LOCATIONS = "bk-locations";
		private const string TOTAL_WORK = "bk-totalwork";
		private const string TRANSACTIONS = "transactions";
		private const string BLOCK_UNDO = "block_undo";
		private const string COINBASETX_BLOCK = "coinbasetx-bk";

		public TransactionsStore TxStore { get; private set; }
		public ConsensusTypeStore<BlockUndoData> BlockUndo { get; private set; }

		public BlockStore() : base(BLOCK_HEADERS)
		{
			TxStore = new TransactionsStore(TRANSACTIONS);
			BlockUndo = new ConsensusTypeStore<BlockUndoData>(BLOCK_UNDO);
		}

        public void Put(
            TransactionContext transactionContext,
            byte[] BkHash,
            Types.Block block,
            LocationEnum location,
            double totalWork)
        {
            var txs = block.transactions.Select(t =>
            {
                return new KeyValuePair<byte[], Types.Transaction>(Merkle.transactionHasher.Invoke(t), t);
            });

            Put(transactionContext, BkHash, new Block
            {
                BlockHeader = block.header,
                TxHashes = txs.Select(t => t.Key).ToList()
            });

			//children
			var children = new HashSet<byte[]>();
            children.Add(BkHash);

			transactionContext.Transaction.InsertHashSet<byte[], byte[]>(
				CHILDREN,
				block.header.parent,
				children,
				0,
				false
			);

			//location
			transactionContext.Transaction.Insert<byte[], int>(LOCATIONS, BkHash, (int)location);

			//total work
			transactionContext.Transaction.Insert<byte[], double>(TOTAL_WORK, BkHash, totalWork);

			//transactions
			var isFirstTx = true;

			foreach (var tx in txs)
			{
                if (!TxStore.ContainsKey(transactionContext, tx.Key))
				{
                    TxStore.Put(transactionContext, tx.Key, 
                        new Transaction { 
	                        Tx = tx.Value, 
	                        InMainChain = false 
	                    });

					if (isFirstTx)
					{
                        transactionContext.Transaction.Insert<byte[], byte[]>(COINBASETX_BLOCK, tx.Key, BkHash);
						isFirstTx = false;
					}
				}
			}
		}

		public bool IsCoinbaseTx(TransactionContext transactionContext, byte[] txHash, out Types.BlockHeader blockHeader)
		{
			var record = transactionContext.Transaction.Select<byte[], byte[]>(COINBASETX_BLOCK, txHash);

			blockHeader = record.Exists ? Get(transactionContext, record.Value).Value.BlockHeader : null;

			return record.Exists;
		}

		public void SetUndoData(
			TransactionContext transactionContext,
			byte[] block,
			BlockUndoData blockUndoData)
		{
			BlockUndo.Put(transactionContext, block, blockUndoData);
		}

		public BlockUndoData GetUndoData(
			TransactionContext transactionContext,
			byte[] block)
		{
			var rec = BlockUndo.Get(transactionContext, block);
			return rec == null ? null : rec.Value;
		}

		public Keyed<Types.Block> GetBlock(TransactionContext transactionContext, byte[] key)
		{
			var _block = Get(transactionContext, key);
            var txs = _block.Value.TxHashes.Select(t => TxStore.Get(transactionContext, t).Value).ToList();

            var block = new Types.Block(_block.Value.BlockHeader, ListModule.OfSeq<Types.Transaction>(txs.Select(t => t.Tx)));

			return new Keyed<Types.Block>(key, block);
		}

		public bool IsLocation(TransactionContext transactionContext, byte[] item, LocationEnum location)
		{
			return transactionContext.Transaction.Select<byte[], int>(LOCATIONS, item).Value == (int) location;
		}

		public LocationEnum GetLocation(TransactionContext transactionContext, byte[] item)
		{
			return (LocationEnum) transactionContext.Transaction.Select<byte[], int>(LOCATIONS, item).Value;
		}

		public void SetLocation(TransactionContext transactionContext, byte[] item, LocationEnum location)
		{
			transactionContext.Transaction.Insert<byte[], int>(LOCATIONS, item, (int)location);

            var block = Get(transactionContext, item).Value;

            foreach (var txHash in block.TxHashes)
            {
                var tx = TxStore.Get(transactionContext, txHash);
                TxStore.Put(transactionContext, txHash, tx.Value.Tx, false);
			}
		}

		public double TotalWork(TransactionContext transactionContext, byte[] item)
		{
			return transactionContext.Transaction.Select<byte[], double>(TOTAL_WORK, item).Value;
		}

		public IEnumerable<Keyed<Types.Block>> Orphans(TransactionContext transactionContext, byte[] parent)
		{
			foreach (var child in transactionContext.Transaction.SelectHashSet<byte[], byte[]>(CHILDREN, parent, 0))
			{
				if (IsLocation(transactionContext, child, LocationEnum.Orphans))
				{
					yield return GetBlock(transactionContext, child);
				}
			}
		}

		public bool HasChildren(TransactionContext transactionContext, byte[] parent)
		{
			var hashSet = transactionContext.Transaction.SelectHashSet<byte[], byte[]>(CHILDREN, parent, 0);

			return hashSet.Count() > 0;
		}
	}
}