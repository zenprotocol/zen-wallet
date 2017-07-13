using System.Collections.Generic;

namespace Zen.Data
{
	public class TestKey
	{
		public string Private { get; set; }
		public string Desc { get; set; }
	}

	public class TestKeys
	{
		public List<TestKey> Values { get; set; }

		public TestKeys()
		{
			Values = new List<TestKey>();
		}
	}
}
