using System;
using Consensus;
using System.Collections.Generic;

namespace Wallet.core
{
	public class AssetsManager
	{
		private AssetsDictionary _AssetsDictionary;

		public AssetsManager ()
		{
			_AssetsDictionary = new AssetsDictionary ();
		}

		public void AddTransactionOutputs(Types.Transaction transaction) 
		{
			foreach (Types.Output output in transaction.outputs) {
				_AssetsDictionary.Add (output);
			}

			Console.WriteLine(_AssetsDictionary);
		}

		public List<Types.Output> Spend(byte[] asset, ulong amount) {
			var assets = _AssetsDictionary [asset];
			var spendList = new List<Types.Output> ();
			ulong total = 0;

			foreach (var output in assets) {
			//	foreach (var output in outputs) {
					spendList.Add (output);
					total += output.spend.amount;

					if (total >= amount) {
						break;
					}
		//		}
			}

			return spendList;
		}
	}
}

