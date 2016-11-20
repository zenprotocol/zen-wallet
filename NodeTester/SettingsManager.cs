//using System;
//using Newtonsoft.Json;
//using System.IO;
//using Gtk;
//using System.Collections.Generic;
//using Infrastructure;

//namespace NodeTester
//{
//	public class Settings {
////		public List<String> DNSSeeds = new List<string>();
//		public List<String> IPSeeds = new List<string>();
//		public int PeersToFind;
//		public int MaximumNodeConnection;
//		public int ServerPort;
////		public String ExternalEndpoint;
//		public bool AutoConfigure;

//		public override string ToString ()
//		{
//			return IPSeeds.Count.ToString () + " seed(s)";
//		}
//	}

//	public class SettingsManager : Singleton<SettingsManager>
//	{
//		LogMessageContext LogMessageContext = new LogMessageContext("Settings");
//		private String settingsFile = "settings.json";
//		private Settings _Settings = null;

//		public Settings Settings {
//			get {
//				if (_Settings == null)
//				{
//					Init();
//				}

//				return _Settings; 
//			} 
//		}

//		private void Init ()
//		{
//			if (File.Exists (settingsFile)) {
//				try {
//					_Settings = JsonConvert.DeserializeObject<Settings> (File.ReadAllText (settingsFile));
//					LogMessageContext.Create ("loaded: " + _Settings);

//					MessageProducer<Settings>.Instance.PushMessage(_Settings);
//				} catch {
//					LogMessageContext.Create ("file corrupted");
//				}
//			} else {
//				LogMessageContext.Create("file missing");
//			}

//			if (_Settings == null) {
//				_Settings = new Settings ();
//				new SettingsWindow().Show();
//			}
//		}

//		public void Save() {
//			File.WriteAllText (settingsFile, JsonConvert.SerializeObject (_Settings));
//			MessageProducer<Settings>.Instance.PushMessage(_Settings);
//			LogMessageContext.Create ("Saved: " + _Settings);
//		}
//	}
//}

