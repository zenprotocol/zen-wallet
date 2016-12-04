//using System;
//using System.Collections.Generic;

//namespace NodeConsole
//{
//	public class Interactive<T>
//	{
//		public T Value { get; set; }
//		public bool IsValid { get; set; }
//		public bool IsBreak { get; set; }
//	}
		
//	public abstract class InteractiveConsole
//	{
//		protected static List<T> GetList<T>(String x) 
//		{
//			List<T> returnValue = new List<T> ();

//			while (true)
//			{
//				Interactive<T> t = GetSingle<T>(x);

//				if (t.IsBreak) {
//					break;
//				} else if (t.IsValid) {
//					returnValue.Add (t.Value);
//				}
//			}

//			return returnValue;
//		}

//		protected static Interactive<T> GetSingle<T>(String x) 
//		{
//			Console.WriteLine($"Enter '{x}' (no-value to break)");

//			String result = Console.ReadLine ();

//			Interactive<T> interactive = new Interactive<T> () { 
//				IsBreak = result == "",
//			};

//			try {
//				interactive.Value = (T)Convert.ChangeType (result, typeof(T));
//				interactive.IsValid = true;
//			} catch {
//				interactive.Value = default(T);
//				interactive.IsValid = false;
//			}

//			return interactive;
//		}

//		protected static Interactive<Boolean> YesNo(String Message) {
//			Console.WriteLine (Message + " (y/N)");

//			String input = Console.ReadLine ().ToLower ();

//			return new Interactive<Boolean> () { 
//				IsValid = input == "y" || input == "n",
//				Value = input == "y"
//			};
//		}

//		protected static void If<T> (Object o, Predicate<T> predicate, params Action<T>[] actions)
//		{
//			if (predicate ((T)o))
//				foreach(Action<T> action in actions)
//					action ((T)o);
//		}	
//	}
//}

