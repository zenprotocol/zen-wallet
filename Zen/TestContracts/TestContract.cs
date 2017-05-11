using System;
using System.Collections.Generic;
using static Consensus.Types;
using Microsoft.FSharp.Core;

public class Test
{
	public static Tuple<IEnumerable<Outpoint>, IEnumerable<Output>, FSharpOption<ExtendedContract>> run(
		byte[] contractHash,
		SortedDictionary<Outpoint, Output> utxos,
		byte[] message
	)
	{
		Console.WriteLine("Hello, world!");

		foreach (var item in utxos)
		{
			Console.WriteLine("got an outpoint !");

			Console.WriteLine(item.Value.spend.amount);
			Console.WriteLine(item.Value.@lock);

		}

		return new Tuple<IEnumerable<Outpoint>, IEnumerable<Output>, FSharpOption<ExtendedContract>>(
			new List<Outpoint>(), new List<Output>(), FSharpOption<ExtendedContract>.None
		);
	}
}