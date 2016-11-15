using System;
using System.Collections.Generic;
using System.Linq;
using DBreeze;

namespace Store
{
	public abstract class MiroringCache<T, Y> where T : Store<Y>
	{
		protected MiroringCache(string dbName, string tableName) : base(dbName, tableName)
		{
			
		}
	}
}