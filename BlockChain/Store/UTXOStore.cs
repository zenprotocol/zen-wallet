using System;
using Store;
using Consensus;
using MsgPack;
using MsgPack.Serialization;

namespace BlockChain.Store
{
	public class UTXOStore : ConsensusTypeStore<Types.Outpoint, Types.Output>
	{
		public UTXOStore() : base("utxo")
		{
		}

		public void Remove(TransactionContext dbTx, byte[] txHash, uint index)
		{
			Remove(dbTx, new Types.Outpoint(txHash, index));
		}

		public void Put(TransactionContext dbTx, byte[] txHash, uint index, Types.Output value) //TODO: use Keyed?
		{
			Put(dbTx, new Types.Outpoint(txHash, index), value);
		}

		public Keyed<Types.Outpoint, Types.Output> Get(TransactionContext dbTx, byte[] txHash, uint index)
		{
			return Get(dbTx, new Types.Outpoint(txHash, index));
		}

		public bool ContainsKey(TransactionContext dbTx, byte[] txHash, uint index)
		{
			return ContainsKey(dbTx, new Types.Outpoint(txHash, index));
		}
	}
}