using System.Collections.Generic;
using Consensus;

namespace Store
{
	public class Mempool : Dictionary<byte[], Types.Transaction>
	{
	}
}
