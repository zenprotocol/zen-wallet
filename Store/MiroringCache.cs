using System;
using System.Collections.Generic;
using System.Linq;
using DBreeze;

namespace Store
{
	public abstract class MiroringCache<T> : Store<T> where T : class
	{
		protected MiroringCache(string tableName) : base(tableName)
		{
			
		}
	}
}