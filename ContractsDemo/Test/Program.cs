using System;
using BlockChain.Data;
using Infrastructure;

namespace Test
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var y = JsonLoader<HashDictionary<string>>.Instance;
			y.FileName = "yyy.txt";
				
			var key = new byte[] { 0x02, 0x03 };

			y.Value[key] = "xxx";

			y.Save();

			Console.WriteLine("Hello World!");
		}
	}
}
