using System;
using System.Collections.Generic;
using Store;

namespace BlockChain
{
	public class ACSItem
	{
		public byte[] Hash { get; set; }
		public string AssemblyFile { get; set; }
		public byte[] CostFn { get; set; }
		public ulong KalapasPerBlock { get; set; }
		public UInt32 LastBlock { get; set; }
	}

	public class ActiveContractSet : MsgPackStore<ACSItem>
	{
		private readonly string LAST_BLOCK = "last-block";
		public ActiveContractSet() : base("contract-set") { }

		//public void Add(TransactionContext dbTx, byte[] contractHash, ulong extention)
		//{
		//	dbTx.Transaction.Insert<byte[], ulong>(LAST_BLOCK, contractHash, extention);
		//	//Put(dbTx, new Keyed<ActiveContractSetItem>(acsItem.Hash, acsItem));
		//}

		public void Add(TransactionContext dbTx, ACSItem item)
		{
			Put(dbTx, new Keyed<ACSItem>(item.Hash, item));
		}

		public bool IsActive(TransactionContext dbTx, byte[] contractHash, ulong height)
		{
			return ContainsKey(dbTx, contractHash) && Get(dbTx, contractHash).Value.LastBlock >= height;
		}

		//public bool IsActive(TransactionContext dbTx, byte[] contractHash, ulong height)
		//{
		//	var lastBlock = dbTx.Transaction.Select<byte[], ulong>(LAST_BLOCK, contractHash);
		//	return (lastBlock.Exists ? lastBlock.Value : 0) >= height;
		//}

		//internal void Extend(TransactionContext dbTx, byte[] contractHash, /*ulong currentHeight,*/ ulong extention)
		//{
		//	var lastBlock = dbTx.Transaction.Select<byte[], ulong>(LAST_BLOCK, contractHash);
		//	//if (lastBlock.Exists /*&& lastBlock.Value >= currentHeight*/)
		//		dbTx.Transaction.Insert<byte[], ulong>(LAST_BLOCK, contractHash, lastBlock.Value + extention);
		//}

		internal void Extend(TransactionContext dbTx, byte[] contractHash, ulong kalapas)
		{
			var acsItem = Get(dbTx, contractHash);
			acsItem.Value.LastBlock += (uint)(kalapas / acsItem.Value.KalapasPerBlock);

			Put(dbTx, acsItem);
		}

		//internal void UndoExtend(TransactionContext dbTx, byte[] contractHash, ulong extention)
		//{
		//	var lastBlock = dbTx.Transaction.Select<byte[], ulong>(LAST_BLOCK, contractHash);
		//	if (extention > lastBlock.Value)
		//		throw new Exception();
		//	dbTx.Transaction.Insert<byte[], ulong>(LAST_BLOCK, contractHash, lastBlock.Value - extention);
		//}

		internal List<byte[]> GetExpiringList(TransactionContext dbTx, uint blockNumber)
		{
			var list = new List<byte[]>();

			foreach (var contractLastBlock in dbTx.Transaction.SelectForward<byte[], ulong>(LAST_BLOCK))
				if (contractLastBlock.Value >= blockNumber)
					list.Add(contractLastBlock.Key);

			return list;
		}

		internal void DeactivateContracts(TransactionContext dbTx, uint blockNumber, out List<byte[]> purgedList)
		{
			purgedList = new List<byte[]>();

			foreach (var contractLastBlock in dbTx.Transaction.SelectForward<byte[], ulong>(LAST_BLOCK))
				if (contractLastBlock.Value == blockNumber)
					purgedList.Add(contractLastBlock.Key);

			foreach (byte[] contractHash in purgedList)
			{
				dbTx.Transaction.RemoveKey<byte[]>(LAST_BLOCK, contractHash);
			}
		}
	}
}
