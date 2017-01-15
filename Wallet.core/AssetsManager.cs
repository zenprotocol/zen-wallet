using System;
using Consensus;
using System.Collections.Generic;

namespace Wallet.core
{
	public class AssetsManager
	{
		private Assets _Assets;

		public AssetsManager ()
		{
			_Assets = new Assets ();
		}

		public void Add(Tuple<Types.Outpoint, Types.Output> value) 
		{
			var asset = AssetsHelper.Find(value.Item2.spend.asset).Caption;
			WalletTrace.Information($"Asset addition: {value.Item2.spend.amount} {asset}");
			_Assets.Add(value);
		}

		public void Remove(Tuple<Types.Outpoint, Types.Output> value)
		{
			var asset = AssetsHelper.Find(value.Item2.spend.asset).Caption;
			WalletTrace.Information($"Asset removal: {value.Item2.spend.amount} {asset}");
			_Assets.Remove(value);
		}

		public Tuple<List<Types.Outpoint>, ulong> Get(byte[] asset, ulong amount) {
			var assets = _Assets [asset];
			var spendList = new List<Types.Outpoint> ();
			ulong total = 0;

			foreach (var item in assets) {
				spendList.Add (item.Item1);
				total += item.Item2.spend.amount;

				if (total >= amount) {
					break;
				}
			}

			return total < amount ? null : new Tuple<List<Types.Outpoint>, ulong>(spendList, total - amount);
		}
	}
}

