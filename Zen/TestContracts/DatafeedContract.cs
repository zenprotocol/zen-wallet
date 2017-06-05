// { "message": "This is a demo oracle datafeed (oracle) contract", "type": "oracle-datafeed" }

using System;
using System.Collections.Generic;
using static Consensus.Types;
using System.Linq;
using Microsoft.FSharp.Core;
using Consensus;

public class DatafeedContract
{
	public static Tuple<IEnumerable<Outpoint>, IEnumerable<Output>, byte[]> main(
		List<byte> message,
		byte[] contractHash,
        Func<Outpoint, FSharpOption<Types.Output>> tryFindUTXO
	)
	{
		var outputs = new List<Output>();
		var outpoints = new List<Outpoint>();

		var data = message.Take(32).ToArray();
		var index = message.Skip(32).Take(1).ToArray()[0];
		var txHash = message.Skip(33).Take(32).ToArray();

		var outpointToSpend = new Outpoint(txHash, (uint)index);
        var utxo = tryFindUTXO(outpointToSpend);

        if (FSharpOption<Types.Output>.get_IsSome(utxo))
        {
            outpoints.Add(outpointToSpend);
            outputs.Add(utxo.Value);

            outputs.Add(
                new Output(OutputLock.NewContractLock(contractHash, data),
                   new Spend(contractHash, 0))
            );
        }

		return new Tuple<IEnumerable<Outpoint>, IEnumerable<Output>, byte[]>(
            outpoints, outputs, new byte[] {}
		);
	}
}