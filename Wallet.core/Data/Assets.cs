﻿using System;
using System.Collections.Generic;
using BlockChain.Data;
using Consensus;
using Wallet.core.Data;

namespace Wallet.core
{
    public class Asset : IComparable<Asset>
	{
		public Types.Outpoint Outpoint { get; set; }
		public Types.Output Output { get; set; }
		public TxStateEnum TxState { get; set; }
		public Key Key { get; set; }

        public int CompareTo(Asset obj)
        {
            return 4 * (TxState - obj.TxState) + (int)(obj.Output.spend.amount - Output.spend.amount);
        }
    }
}