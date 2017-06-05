// { "message": "This is a demo call-option", "publicKey": "", "type": "call-option", "expiry": 1, "strike": 1.34, "oracle": "", "underlying": "GOOG", "controlAsset": "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAE=", "numeraire": "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=" }

using System;
using System.Collections.Generic;
using static Consensus.Types;
using System.Linq;
using Microsoft.FSharp.Core;
using Consensus;

namespace Zen
{
	public class MockCallOption
	{
		public static Tuple<IEnumerable<Outpoint>, IEnumerable<Output>, byte[]> main(
			List<byte> message,
			byte[] contractHash,
			Func<Outpoint, FSharpOption<Types.Output>> tryFindUTXO
		)
		{
			var outputs = new List<Output>();
			var outpoints = new List<Outpoint>();

			return new Tuple<IEnumerable<Outpoint>, IEnumerable<Output>, byte[]>(
				outpoints, outputs, new byte[] { }
			);
		}
	}
}