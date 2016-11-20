using System;
namespace Wallet
{
	public class Key
	{
		public byte[] Public { get; set; }
		public byte[] Private { get; set; }
		public bool Used { get; set; }
		public bool IsChange { get; set; }
	}
}
