//using System;
//using Newtonsoft.Json;
//using System.IO;
//using System.Collections.Generic;
//using Infrastructure;
//
//namespace NodeCore
//{
////	public class Settings {
//////		public List<String> DNSSeeds = new List<string>();
////		public List<String> IPSeeds = new List<string>();
////		public int PeersToFind;
////		public int MaximumNodeConnection;
////		public int ServerPort;
////		public bool AutoConfigure;
////	}
//
//	public class JsonLoader<T> : Singleton<JsonLoader<T>>
//	{
//		private String fileName;
//		private T _t = null;
//
//		public JsonLoader(String fileName)
//		{
//			this.fileName = fileName;
//		}
//
//		private bool _IsNew;
//		public bool IsNew { 
//			get {
//				Value;
//				return _IsNew;
//			}
//		}
//
//		public T Value {
//			get {
//				if (_t == null)
//				{
//					_t = Load();
//				}
//
//				return _t; 
//			} 
//		}
//
//		private T Load ()
//		{
//			T t = null;
//
//			if (File.Exists (fileName)) {
//				try {
//					t = JsonConvert.DeserializeObject<T> (File.ReadAllText (fileName));
//				} catch (Exception e) {
//					Trace.Error("Load JSON file", e);
//				}
//			}
//
//			if (t == null)
//			{
//				_IsNew = true;
//				t = new T();
//			}
//
//			return t;
//		}
//
//		public void Save() {
//			File.WriteAllText (fileName, JsonConvert.SerializeObject (_t));
//		}
//
//		public void Delete() {
//			File.WriteAllText (fileName, JsonConvert.SerializeObject (_t));
//		}
//	}
//}
//
