using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Consensus;

namespace NBitcoin.Protocol
{
	/// <summary>
	/// Represents a transaction being sent on the network, is sent after being requested by a getdata (of Transaction or MerkleBlock) message.
	/// </summary>
	public class TxPayload : Payload
	{
		public Types.Transaction Transaction { get; set; }

		public TxPayload()
		{

		}
		public TxPayload(Types.Transaction transaction)
		{
			Transaction = transaction;
		}
	}
}
