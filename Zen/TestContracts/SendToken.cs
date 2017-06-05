// { "message": "This is a demo call-option", "publicKey": "xxxx", "type": "call-option", "expiry": 1, "strike": 1.34, "oracle": "xxx", "underlying": "GOOG" }

using System;
using System.Collections.Generic;
using static Consensus.Types;
using Microsoft.FSharp.Core;
using System.Linq;
using Consensus;

public class TestContract
{
	public static Tuple<IEnumerable<Outpoint>, IEnumerable<Output>, byte[]> main(
		List<byte> message,
		byte[] contractHash,
		Func<Outpoint, FSharpOption<Types.Output>> tryFindUTXO
	)
	{
		var outputs = new List<Output>();
		var outpoints = new List<Outpoint>();

        outputs.Add(new Output(OutputLock.NewPKLock(message.ToArray()), new Spend(contractHash, 5555)));

		return new Tuple<IEnumerable<Outpoint>, IEnumerable<Output>, byte[]>(
            outpoints, outputs, new byte[] {}
		);
	}
}