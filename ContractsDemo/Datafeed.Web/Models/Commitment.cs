using System;
using System.Collections.Generic;

namespace Datafeed.Web.Models
{
	public class Commitment
	{
		public byte[] merkelRoot { get; set; }
		public List<Ticker> items { get; set; }
	}
}
