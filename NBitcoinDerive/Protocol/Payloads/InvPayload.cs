using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Consensus;

namespace NBitcoin.Protocol
{
	/// <summary>
	/// Announce the hash of a transaction or block
	/// </summary>
	public class InvPayload : Payload, IEnumerable<InventoryVector>
	{
		//demo
		private static byte[] GetHash(Types.Transaction transaction)
		{
			return Merkle.transactionHasher.Invoke(transaction);
		}

		public InvPayload()
		{

		}

		public InvPayload(params Types.Transaction[] transactions)
			: this(transactions.Select(tx => new InventoryVector(InventoryType.MSG_TX, GetHash(tx))).ToArray())
		{

		}
		//public InvPayload(params Block[] blocks)
		//	: this(blocks.Select(b => new InventoryVector(InventoryType.MSG_BLOCK, b.GetHash())).ToArray())
		//{
		//
		//}
		public InvPayload(InventoryType type, params byte[][] hashes)
			: this(hashes.Select(h => new InventoryVector(type, h)).ToArray())
		{

		}
		public InvPayload(params InventoryVector[] invs)
		{
			_Inventory.AddRange(invs);
		}
		List<InventoryVector> _Inventory = new List<InventoryVector>();
		public List<InventoryVector> Inventory
		{
			get
			{
				return _Inventory;
			}
		}
		
		public override string ToString()
		{
			return "Inventory count: " + Inventory.Count.ToString();
		}

		#region IEnumerable<byte[]> Members

		public IEnumerator<InventoryVector> GetEnumerator()
		{
			return Inventory.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
