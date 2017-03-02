using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Configuration;

namespace Wallet.Tests
{
	[TestFixture()]
	public class AssetTagsTests : WalletTestsBase
	{
		[SetUp]
		public void SetUp()
		{
			OneTimeSetUp();
		}

		[OneTimeTearDown]
		public new void Dispose()
		{
			var assetsDir = ConfigurationManager.AppSettings.Get("assetsDir");

			if (Directory.Exists(assetsDir))
			{
				Directory.Delete(assetsDir, true);
			}

			base.Dispose();
		}

		[Test(), Order(1)]
		public void ShouldContainZen()
		{
			Assert.That(_WalletManager.AssetsMetadata, Contains.Key(Consensus.Tests.zhash));
			Assert.That(_WalletManager.AssetsMetadata[Consensus.Tests.zhash].Caption, Is.EqualTo("Zen"));
		}

		[Test(), Order(2)]
		public void ShouldLoadFromWeb()
		{
			var AssetsMetadata = _WalletManager.AssetsMetadata;
			var evt = new ManualResetEvent(false);
			var hash = new byte[] { 0x01 };
			byte[] evtValue = null;

			AssetsMetadata.AssetChanged += a =>
			{
				evtValue = a;
				evt.Set();
			};

			AssetsMetadata.Add(hash, new Uri("http://pastebin.com/raw/pscmbwym"));

			Assert.That(evt.WaitOne(1000), Is.True);
			Assert.That(evtValue, Is.EqualTo(hash));
			Assert.That(AssetsMetadata, Contains.Key(hash));
			Assert.That(_WalletManager.AssetsMetadata[hash].Caption, Is.EqualTo("Test"));
		}
	}
}
