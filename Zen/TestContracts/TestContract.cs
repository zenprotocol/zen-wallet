// { "message": "This is a demo call-option", "publicKey": "xxxx", "type": "call-option", "expiry": 1, "strike": 1.34, "oracle": "xxx", "underlying": "GOOG" }

using System;
using System.Collections.Generic;
using static Consensus.Types;
using Microsoft.FSharp.Core;
using System.Linq;

public class TestContract
{
	public static Tuple<IEnumerable<Outpoint>, IEnumerable<Output>, byte[]> main(
		List<byte> message,
		byte[] contractHash,
		Func<Outpoint, FSharpOption<Types.Output>> tryFindUTXO
	)
	{
		Console.WriteLine($"Hello, world!");
		var messageStr = BitConverter.ToString(message.ToArray());
		Console.WriteLine("my message is: " + messageStr);

		var outputs = new List<Output>();
		var outpoints = new List<Outpoint>();

		//foreach (var item in utxos)
		//{
		//	if (item.Key.txHash.SequenceEqual(message)) {
		//		var data = ((OutputLock.ContractLock)item.Value.@lock).data;
		//		outputs.Add(new Output(OutputLock.NewPKLock(contractHash), new Spend(item.Value.spend.asset, item.Value.spend.amount)));
		//		outputs.Add(new Output(OutputLock.NewPKLock(data), new Spend(contractHash, item.Value.spend.amount)));
		//		outpoints.Add(item.Key);
		//	}
		//}

		return new Tuple<IEnumerable<Outpoint>, IEnumerable<Output>, byte[]>(
            outpoints, outputs, new byte[] {}
		);
	}
}