using System;

namespace NodeConsole
{
	public class EnsureSettingAction<T>
	{
		public EnsureSettingAction (Object o, Predicate<T> Predicate, Action<T> x)
		{
			if (Predicate ((T)o))
				x ((T)o);
		}
	}
}

