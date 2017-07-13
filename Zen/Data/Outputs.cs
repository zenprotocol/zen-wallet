using System;
using System.Collections.Generic;

namespace Zen.Data
{
	public class Output
	{
		public int TestKeyIdx { get; set; }
		public ulong Amount { get; set; }
	}

	public class Outputs
	{
		public List<Output> Values { get; set; }

		public Outputs()
		{
			Values = new List<Output>();
		}
	}
}
