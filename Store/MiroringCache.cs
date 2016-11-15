using System;
using System.Collections.Generic;
using System.Linq;
using DBreeze;

namespace Store
{
	public abstract class MiroringCache<T> : Store<T> 
	{
		protected MiroringCache(string tableName) : base(tableName)
		{
			
		}
	}
}