using System;
using System.Collections.Generic;
using BlockChain.Data;
using Consensus;
using Store;

namespace Wallet.core
{
	public interface IWalletMessage
	{
	}

	public class ResetMessage : UpdatedMessage
	{
	}

	public abstract class UpdatedMessage : WalletBalances, IWalletMessage
	{
	}

	public class WalletBalances : List<UpdateInfoItem>
	{
	}

	public class UpdateInfoItem
	{
		public Keyed<Types.Transaction> Transaction { get; set; }
		public HashDictionary<long> Balances { get; set; }

		public UpdateInfoItem(Keyed<Types.Transaction> transaction, HashDictionary<long> balances)
		{
			Transaction = transaction;
			Balances = balances;
		}
	}
}
