using NUnit.Framework;
using System;
using NBitcoin;
using NBitcoin.Crypto;
using NBitcoin.BouncyCastle.Crypto.Parameters;
using NBitcoin.BouncyCastle.Crypto.Signers;
using NBitcoin.BouncyCastle.Asn1.X9;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Math.EC;
using NBitcoin.BouncyCastle.Math.EC.Custom.Sec;

namespace Wallet.tests
{
	[TestFixture ()]
	public class TestECKeyClass
	{
		[Test ()]
		public void test ()
		{
			uint256 hash = 1234567890;
			byte[] keyBytes = { 0, 0, 0, 25 };

			ECKey signningECKey = new ECKey (keyBytes, true);
			ECDSASignature signature = signningECKey.Sign (hash);

			bool isSuccess = signningECKey.Verify (hash, signature);

			Assert.AreEqual(true, isSuccess);
		}
	}
}

