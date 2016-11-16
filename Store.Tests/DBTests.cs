using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Store.Tests
{
	//TOOD: benchmark compare with iBoxDb?

	[TestFixture()]
	public class DBTests : TestBase
	{
		private class TestClass
		{
			public String x { get; set; }
		}

		[Test()]
		public void CanStoreRefObject()
		{
			//TestClass obj = new TestClass() { x = "1234" };

			//using (DBreezeEngine Engine { get; private set; }

			//String dbName = "test-" + new Random().Next(0, 1000);

			//Directory.Delete(dbName, true);
		}
	}
}
