using System;
using System.Collections.Generic;
using System.Text;
using Consensus;
using Microsoft.FSharp.Collections;

namespace Compatibility
{
	class Consensus
	{
		public static void start()
		{
			byte[] hashed = Merkle.transactionHasher.Invoke(GetNewTransaction());
			Console.WriteLine(Encoding.ASCII.GetString(hashed));
			Console.ReadLine ();
		}

		private static Types.Transaction GetNewTransaction()
		{
			var endpoints = new List<Types.Outpoint>();
			var outputs = new List<Types.Output>();
			var hashes = new List<byte[]>();

			endpoints.Add(new Types.Outpoint(new byte[] { 0x34 }, 222));

			Types.Transaction transaction =
				new Types.Transaction(1,
					ListModule.OfSeq(endpoints),
					ListModule.OfSeq(hashes),
					ListModule.OfSeq(outputs),
					null);

			return transaction;
		}
	}
}
