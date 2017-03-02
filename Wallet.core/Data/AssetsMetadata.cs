using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using BlockChain.Data;
using Infrastructure;
using Newtonsoft.Json;

namespace Wallet.core
{
	struct AssetMetadata
	{
		public string name;
		public string imageUrl;
		public string version;
	}

	public class AssetsHelper : HashDictionary<AssetType>
	{
		public event Action<byte[]> AssetChanged;
		readonly JsonLoader<Dictionary<string, AssetMetadata>> _MetadataCacheloader = JsonLoader<Dictionary<string, AssetMetadata>>.Instance;
		readonly HttpClient _HttpClient = new HttpClient();

		public AssetsHelper() {
			var loader = JsonLoader<Dictionary<string, Uri>>.Instance;

			loader.FileName = ConfigurationManager.AppSettings.Get("assetsFile");
			_MetadataCacheloader.FileName = "assetsCache.json";

			if (loader.IsNew)
			{
				var zenJson = Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), "zen.json");
				loader.Value[BitConverter.ToString(Consensus.Tests.zhash)/*.Replace("-", string.Empty)*/] = new Uri(zenJson);
				loader.Save();
			}

			Add(new byte[] { }, new AssetTypeAll());

			foreach (var item in loader.Value)
			{
				//tasks.Add(
				ProcessURLAsync(item.Key, item.Value);
				//);
			}

			//Task.WaitAll(tasks.ToArray());
		}

		async Task ProcessURLAsync(string hash, Uri uri)
		{
			string value;

			if (uri.IsFile)
			{
				value = File.ReadAllText(uri.AbsolutePath);
			}
			else
			{
				value = await _HttpClient.GetStringAsync(uri.AbsoluteUri);
			}

			var remoteJson = JsonConvert.DeserializeObject<AssetMetadata>(value);
			var currentVersion = _MetadataCacheloader.Value.ContainsKey(hash) ? new Version(_MetadataCacheloader.Value[hash].version) : new Version();
			var remoteVersion = new Version(remoteJson.version);

			if (!uri.IsFile && remoteVersion > currentVersion)
			{
				lock (_MetadataCacheloader)
				{
					_MetadataCacheloader.Value[hash] = remoteJson;
				//	_MetadataCacheloader.Save();
				}
			}

			var _hash = Array.ConvertAll<string, byte>(hash.Split('-'), s => Convert.ToByte(s, 16));
			Add(_hash, new AssetType(remoteJson.name, null));

			if (AssetChanged != null)
				AssetChanged(_hash);
		}
	}
}