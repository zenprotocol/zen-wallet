using System;
using System.Collections.Generic;
using BlockChain.Data;
using Consensus;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Wallet.core
{
	public class AssetsDictionary : HashDictionary<ISet<Types.Output>>
	{
		public void Add(Types.Output output)
		{
			var asset = output.spend.asset;

			if (!ContainsKey(asset))
				this [asset] = GetSet();

			this [asset].Add (output);
		}

		public override string ToString ()
		{
			return this.GetType() + "\n" + JsonConvert.SerializeObject(
				this, Formatting.Indented,
				new JsonConverter[] {new StringEnumConverter()});
		}

		private ISet<Types.Output> GetSet() {
			return new SortedSet<Types.Output> (new OutputComparer ());
		}
	}

//	public class AssetsList : SortedSet<Types.Output>
//	{
//		public void Add (ulong key, Types.Output value)
//		{
//			if (!ContainsKey(key))
//				this [key] = new List<Types.Output> ();
//
//			if (!this [key].Contains (value)) //TODO: may need to have IComparable implemented
//				this [key].Add (value);
//		}
//	}

//	public class AssetsList : SortedList<ulong, List<Types.Output>>
//	{
//		public void Add (ulong key, Types.Output value)
//		{
//			if (!ContainsKey(key))
//				this [key] = new List<Types.Output> ();
//
//			if (!this [key].Contains (value)) //TODO: may need to have IComparable implemented
//				this [key].Add (value);
//		}
//	}

	internal class OutputComparer : IComparer<Types.Output>
	{
		public int Compare(Types.Output o1, Types.Output o2)
		{
			return o1.spend.amount.CompareTo (o2.spend.amount);
		}
	}
}