//using System;
//using System.Collections.Generic;
//
//namespace Wallet
//{
//	public class EventBus
//	{
//		public delegate void Del(string str);
//
//		private static EventBus instance = null;
//
//		private IDictionary<String, List<Del>> handlers = new Dictionary<String, List<Del>>();
//
//		public static EventBus GetInstance() {
//			if (instance == null) {
//				instance = new EventBus ();
//			}
//
//			return instance;
//		}
//			
//		public void Register(String EventTag, Del handler) {
//			if (!handlers.ContainsKey(EventTag)) {
//				handlers [EventTag] = new List<Del> ();
//			}
//
//			handlers [EventTag].Add(handler);
//		}
//
//		public void Close() {
//		}
//
//		public void Dispatch(String EventTag, String Name) {
//
//			if (handlers.ContainsKey (EventTag)) {
//				Console.WriteLine("Handler found " + EventTag + " ## " + Name);
//
//				foreach (Del del in handlers [EventTag]) {
//					del (Name);
//				}
//			} else {
//				Console.WriteLine ("Handler not found " + EventTag + " ## " + Name);
//			}
//		}
//	}
//}
//
