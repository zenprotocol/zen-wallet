//using System;
//using System.Text;

//namespace Compatibility
//{
//	class LightningDB
//	{
//		public static void start()
//		{
//			var env = new LightningEnvironment(".");
//			env.MaxDatabases = 2;
//			env.Open();

//			using (var tx = env.BeginTransaction())
//			using (var db = tx.OpenDatabase("custom", new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }))
//			{
//				tx.Put(db, Encoding.UTF8.GetBytes("hello"), Encoding.UTF8.GetBytes("world"));
//				tx.Commit();
//			}
//			using (var tx = env.BeginTransaction(TransactionBeginFlags.ReadOnly))
//			{
//				var db = tx.OpenDatabase("custom");
//				var result = tx.Get(db, Encoding.UTF8.GetBytes("hello"));
//				if (result == Encoding.UTF8.GetBytes("world"))
//				{
//					Console.WriteLine("success!");
//				}
//			}
//		}
//	}
//}
