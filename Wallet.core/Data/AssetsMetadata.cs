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

	public class AssetsMetadata : HashDictionary<AssetType>
	{
		const int DEFAULT_SIZE = 64;

		public event Action<byte[]> AssetChanged;

		readonly JsonLoader<Dictionary<string, AssetMetadata>> _CacheLoader = JsonLoader<Dictionary<string, AssetMetadata>>.Instance;
		readonly JsonLoader<Dictionary<string, Uri>> _AssetsLoader = JsonLoader<Dictionary<string, Uri>>.Instance;
		readonly HttpClient _HttpClient = new HttpClient();

		string Config(string key) { return ConfigurationManager.AppSettings.Get(key); }
		string StringifyHash(byte[] hash) { return BitConverter.ToString(hash)/*.Replace("-", string.Empty)*/; }
		string LocalImage(string hash, string imageUrl) { return PathCombine(_CacheDirectory, hash, "" + DEFAULT_SIZE + Path.GetExtension(imageUrl)); }
		byte[] ParseHash(string hash) { return Array.ConvertAll<string, byte>(hash.Split('-'), s => Convert.ToByte(s, 16)); }
		string PathCombine(params string[] values)
		{
			var value =Path.Combine(values);
			if (!Directory.Exists(Path.GetDirectoryName(value)))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(value));
			}
			return value;
		}

		readonly string _Directory;
		readonly string _CacheDirectory;

		public AssetsMetadata() {
			_Directory = PathCombine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), Config("assetsDir"));
			_CacheDirectory = PathCombine(_Directory, "cache");
			_AssetsLoader.FileName = PathCombine(_Directory, Config("assetsFile"));
			_CacheLoader.FileName = PathCombine(_CacheDirectory, "metadata.json");

			Add(Consensus.Tests.zhash, new AssetType("Zen", "zen.png"));

			foreach (var item in _AssetsLoader.Value)
			{
				if (_CacheLoader.Value.ContainsKey(item.Key))
				{
					var metaData = _CacheLoader.Value[item.Key];
					Add(ParseHash(item.Key), new AssetType(metaData.name, LocalImage(item.Key, metaData.imageUrl)));
				}

				ProcessURLAsync(item.Key, item.Value);
			}
		}

		public void Add(byte[] hash, Uri uri)
		{
			var _hash = StringifyHash(hash);
			_AssetsLoader.Value[_hash] = uri;
			_AssetsLoader.Save();

			if (!uri.IsFile)
			{
				ProcessURLAsync(_hash, uri);
			}
		}

		async Task ProcessURLAsync(string hash, Uri uri)
		{
			string value;

			WalletTrace.Information($"Loading asset metadata from web: {uri.AbsoluteUri}");
			value = await _HttpClient.GetStringAsync(uri.AbsoluteUri);

			var remoteJson = JsonConvert.DeserializeObject<AssetMetadata>(value);
			var currentVersion = _CacheLoader.Value.ContainsKey(hash) ? new Version(_CacheLoader.Value[hash].version) : new Version();
			var remoteVersion = remoteJson.version == null ? new Version() : new Version(remoteJson.version);

			if (remoteVersion > currentVersion)
			{
				WalletTrace.Information($"Updateing asset metadata: {remoteJson.name}");

				lock (_CacheLoader)
				{
					_CacheLoader.Value[hash] = remoteJson;
					_CacheLoader.Save();
				}

				var _hash = ParseHash(hash);

				WalletTrace.Information($"New asset metadata: {remoteJson.name}");
				Update(_hash, remoteJson.name, null);

				if (!string.IsNullOrEmpty(remoteJson.imageUrl))
				{
					ProcessImageAsync(hash, remoteJson);
				}
			}
			else
			{
				WalletTrace.Information($"Asset metadata is alreay up to date: {remoteJson.name}");
			}
		}

		async Task ProcessImageAsync(string hash, AssetMetadata metadata)
		{
			var imageFile = LocalImage(hash, metadata.imageUrl);

			using (var response = await _HttpClient.GetAsync(metadata.imageUrl))
			{
				response.EnsureSuccessStatusCode();

				using (var inputStream = await response.Content.ReadAsStreamAsync())
				{
					using (var fileStream = File.Create(imageFile))
					{
						inputStream.CopyTo(fileStream);
					}
				}
			}

			WalletTrace.Information($"Image downloaded for asset: {metadata.name}");
			Update(ParseHash(hash), metadata.name, imageFile);
		}

		void Update(byte[] hash, string name, string image)
		{
			this[hash] = new AssetType(name, image);

			if (AssetChanged != null)
			{
				AssetChanged(hash);
			}
		}
	}
}