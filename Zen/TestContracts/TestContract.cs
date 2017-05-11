using System;
using System.Collections.Generic;
using static Consensus.Types;
using Microsoft.FSharp.Core;
using System.Linq;

public class Test
{
	public static Tuple<IEnumerable<Outpoint>, IEnumerable<Output>, FSharpOption<ExtendedContract>> run(
		byte[] contractHash,
		SortedDictionary<Outpoint, Output> utxos,
		byte[] message
	)
	{
		Console.WriteLine($"Hello, world! i can see { utxos.Keys.Count } utxo(s)");
		var messageStr = BitConverter.ToString(message);
		Console.WriteLine("my message is: " + messageStr);
		foreach (var item in utxos)
		{
			Console.WriteLine($" data: { System.Text.Encoding.UTF8.GetString(((OutputLock.ContractLock) item.Value.@lock).data) } with amount of { item.Value.spend.amount }");
		}

		var outputs = new List<Output>();
		var outpoints = new List<Outpoint>();

	//	var output = utxos[new Outpoint(message, 0)];

	//	outpoints.Add(new Outpoint(message, 0));
	//	outputs.Add(output);
		//if (messageStr == "issue_token")
		//{
			foreach (var item in utxos)
			{
				if (item.Key.txHash.SequenceEqual(message)) {
					Console.WriteLine("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");
					var data = ((OutputLock.ContractLock)item.Value.@lock).data;
					outputs.Add(new Output(OutputLock.NewPKLock(contractHash), new Spend(item.Value.spend.asset, item.Value.spend.amount)));
					outputs.Add(new Output(OutputLock.NewPKLock(data), new Spend(contractHash, item.Value.spend.amount)));
					outpoints.Add(item.Key);
				}
			}



				//var data = System.Text.Encoding.UTF8.GetString(((OutputLock.ContractLock)item.Value.@lock).data);
 			//	var data = ((OutputLock.ContractLock)item.Value.@lock).data;
				//outputs.Add(new Output(OutputLock.NewPKLock(data), new Spend(item.Value.spend.asset, item.Value.spend.amount)));
				//outpoints.Add(item.Key);
		//	}
		//}

		return new Tuple<IEnumerable<Outpoint>, IEnumerable<Output>, FSharpOption<ExtendedContract>>(
			outpoints, outputs, FSharpOption<ExtendedContract>.None
		);
	}
}