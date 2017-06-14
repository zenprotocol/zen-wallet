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
using System.Linq;
using System.Web;

namespace Wallet.core
{
    class AssetMetadata
    {
        public string name;
        public string imageUrl;
        public string version;
    }

    public class AssetsMetadata
    {
        const int DEFAULT_SIZE = 64;
        const string ZEN = "Zen";
        //static readonly AssetType ZEN_ASSET_TYPE = new AssetType("Zen", "zen.png");

        public event Action<byte[]> AssetChanged;

        readonly JsonLoader<Dictionary<string, AssetMetadata>> _Cache = JsonLoader<Dictionary<string, AssetMetadata>>.Instance;
        //readonly JsonLoader<Dictionary<string, Uri>> _Assets = JsonLoader<Dictionary<string, Uri>>.Instance;

        string Config(string key) { return ConfigurationManager.AppSettings.Get(key); }
        //TODO: directly serialize HashDictionary (handle the keys serialization), remove StringifyHash
        string StringifyHash(byte[] hash) { return BitConverter.ToString(hash)/*.Replace("-", string.Empty)*/; }
        string LocalImage(string hash, string imageUrl) { return PathCombine(_CacheDirectory, hash, "" + DEFAULT_SIZE + Path.GetExtension(imageUrl)); }
        //byte[] ParseHash(string hash) { return Array.ConvertAll<string, byte>(hash.Split('-'), s => Convert.ToByte(s, 16)); }
        string PathCombine(params string[] values)
        {
            var value = Path.Combine(values);
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
            //_Assets.FileName = PathCombine(_Directory, Config("assetsFile"));
            _Cache.FileName = PathCombine(_CacheDirectory, "metadata.json");

            //foreach (var item in _Assets.Value)
            //{
            //    if (_Cache.Value.ContainsKey(item.Key))
            //    {
            //        var metaData = _Cache.Value[item.Key];
            //        Add(ParseHash(item.Key), new AssetType(metaData.name, LocalImage(item.Key, metaData.imageUrl)));
            //    }

            //    ProcessURLAsync(item.Key, item.Value);
            //}

            Keys.Add(Consensus.Tests.zhash);
        }

		//public IEnumerable<Tuple<byte[], string>> Assets
		//{
		//    get {
		//        var zen = new Tuple<byte[], string>(Consensus.Tests.zhash, ZEN);
		//        var others = _Cache.Value.Keys.Select(t => new Tuple<byte[], string>(Convert.FromBase64String(t), _Cache.Value[t].name));
		//        return new Tuple<byte[], string>[] { zen }.Concat(others);
		//    }
		//}


		public async Task<String> Get(byte[] asset)
        {
            if (asset.SequenceEqual(Consensus.Tests.zhash))
            {
                return ZEN;
            }

            var key = Convert.ToBase64String(asset);

            if (_Cache.Value.ContainsKey(key))
            {
                return _Cache.Value[key].name;
            }

			Add(asset);
			return await pendingTasks[asset];
        }

        HashDictionary<Task<String>> pendingTasks = new HashDictionary<Task<String>>();

        public HashSet Keys = new HashSet();

        public void Add(byte[] asset)
        {
            Keys.Add(asset);

            if (asset.SequenceEqual(Consensus.Tests.zhash) || _Cache.Value.ContainsKey(Convert.ToBase64String(asset)))
            {
                return;
            }

            lock (pendingTasks)
            {                
                if (pendingTasks.ContainsKey(asset))
                {
                    return;
                }

                var uri = new Uri(string.Format($"http://{Config("assetsDiscovery")}/AssetMetadata/Index/" + HttpServerUtility.UrlTokenEncode(asset)));
                pendingTasks.Add(asset, ProcessRequestAsync(asset, uri));
            }
        }

        async Task<String> ProcessRequestAsync(byte[] hash, Uri uri)
        {
            string _hash = Convert.ToBase64String(hash);
            string remoteString = null;

            WalletTrace.Information($"Loading asset metadata from web: {uri.AbsoluteUri}");

            var response = await new HttpClient().GetAsync(uri.AbsoluteUri);

            if (response.IsSuccessStatusCode)
			{
			    remoteString = await response.Content.ReadAsStringAsync();
			}
			else
			{
                return "Not found";
            }

            AssetMetadata remoteJson = null;
            Version currentVersion = null;
            Version remoteVersion = null;

            try
            {
                remoteJson = JsonConvert.DeserializeObject<AssetMetadata>(remoteString);
                currentVersion = _Cache.Value.ContainsKey(_hash) ? new Version(_Cache.Value[_hash].version) : new Version();
                remoteVersion = remoteJson.version == null ? new Version() : new Version(remoteJson.version);
            } catch (Exception e)
            {
				WalletTrace.Information($"Error in asset metadata: {uri.AbsoluteUri}");
                return "Error";
			}

			if (remoteVersion > currentVersion)
			{
				WalletTrace.Information($"Updating asset metadata: {remoteJson.name}");

				lock (_Cache)
				{
					_Cache.Value[_hash] = remoteJson;
					_Cache.Save();
				}

				WalletTrace.Information($"New asset metadata: {remoteJson.name}");
				//Update(hash, remoteJson.name, null);

				if (!string.IsNullOrEmpty(remoteJson.imageUrl))
				{
					//         ProcessImageAsync(hash, remoteJson);
				}
			}
			else if (remoteVersion.Build != -1)
			{
				WalletTrace.Information($"Asset metadata is alreay up to date: {remoteJson.name}");
			}
			else
			{ 
				WalletTrace.Information($"Asset metadata not found: {Convert.ToBase64String(hash)}");
			}

            return remoteJson.name ?? Convert.ToBase64String(hash);
        }

        //async Task ProcessImageAsync(byte[] hash, AssetMetadata metadata)
        //{
        //    var imageFile = LocalImage(hash, metadata.imageUrl);

        //    using (var response = await _HttpClient.GetAsync(metadata.imageUrl))
        //    {
        //        response.EnsureSuccessStatusCode();

        //        using (var inputStream = await response.Content.ReadAsStreamAsync())
        //        {
        //            using (var fileStream = File.Create(imageFile))
        //            {
        //                inputStream.CopyTo(fileStream);
        //            }
        //        }
        //    }

        //    WalletTrace.Information($"Image downloaded for asset: {metadata.name}");
        //    Update(hash, metadata.name, imageFile);
        //}

        //void Update(byte[] hash, string name, string image)
        //{
        //    this[hash] = new AssetType(name, image);

        //    if (AssetChanged != null)
        //    {
        //        AssetChanged(hash);
        //    }
        //}
    }
}