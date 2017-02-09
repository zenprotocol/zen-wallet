using System;
using System.Collections.Generic;
using BlockChain.Data;
using Consensus;
using System.Linq;
using Wallet.core.Data;
using BlockChain.Data;

namespace Wallet.core
{
	internal class Asset
	{
		public Types.Outpoint Outpoint { get; set; }
		public Types.Output Output { get; set; }
		public TxStateEnum TxState { get; set; }
		public Key Key { get; set; }
	}

	internal class Assets : List<Asset>
	{
		public static Assets Sort(Assets assets)
		{
			return (Assets) assets.OrderBy(c => c.TxState).ThenBy(c => c.Output.spend.amount);
		}
	}
}