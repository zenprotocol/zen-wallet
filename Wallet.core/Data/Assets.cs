using System;
using System.Collections.Generic;
using BlockChain.Data;
using Consensus;
using System.Linq;

namespace Wallet.core
{
	public class Assets : HashDictionary<ISet<Tuple<Types.Outpoint, Types.Output>>>
	{
		public void Add(Tuple<Types.Outpoint, Types.Output> value)
		{
			var asset = value.Item2.spend.asset;

			if (!ContainsKey(asset))
				this[asset] = new SortedSet<Tuple<Types.Outpoint, Types.Output>>(new OutputComparer());

			this [asset].Add (value);
		}	

		public void Remove(Tuple<Types.Outpoint, Types.Output> value)
		{
			var asset = value.Item2.spend.asset;
			var set = this[asset];

			foreach (var item in set)
			{
				if (item.Item1.index == value.Item1.index &&
					item.Item1.txHash.SequenceEqual(value.Item1.txHash) &&
					item.Item2.spend.amount == value.Item2.spend.amount) //TODO
				{
					set.Remove(item);
					return;
				}
			}

			throw new Exception();
		}
	}

	internal class OutputComparer : IComparer<Tuple<Types.Outpoint,Types.Output>>
	{
		public int Compare(Tuple<Types.Outpoint, Types.Output> o1, Tuple<Types.Outpoint, Types.Output> o2)
		{
			return o1.Item2.spend.amount.CompareTo (o2.Item2.spend.amount);
		}
	}
}