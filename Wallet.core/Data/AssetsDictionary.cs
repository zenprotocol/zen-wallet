using System;
using System.Collections.Generic;
using BlockChain.Data;
using Consensus;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Wallet.core
{
	public class AssetsDictionary : HashDictionary<AssetsList>
	{
		public void Add(Types.Output output)
		{
			var asset = output.spend.asset;

			if (!ContainsKey(asset))
				this [asset] = new AssetsList ();

			this [asset].Add (output.spend.amount, output);
		}

		public override string ToString ()
		{
			return JsonConvert.SerializeObject(
				this, Formatting.Indented,
				new JsonConverter[] {new StringEnumConverter()});
		}
	}

	public class AssetsList : SortedList<ulong, List<Types.Output>>
	{
		public void Add (ulong key, Types.Output value)
		{
			if (!ContainsKey(key))
				this [key] = new List<Types.Output> ();

			if (!this [key].Contains (value))
				this [key].Add (value);
		}
	}
}