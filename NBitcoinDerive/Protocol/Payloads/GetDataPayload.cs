using System;
using System.Collections.Generic;
using MsgPack;


namespace NBitcoin.Protocol
{
	/// <summary>
	/// Ask for transaction, block or merkle block
	/// </summary>
	public class GetDataPayload
	{
		public GetDataPayload()
		{
		}
		public GetDataPayload(params InventoryVector[] vectors)
		{
			inventory.AddRange(vectors);
		}
		List<InventoryVector> inventory = new List<InventoryVector>();

		public List<InventoryVector> Inventory
		{
			set
			{
				inventory = value;
			}
			get
			{
				return inventory;
			}
		}
	}
}

