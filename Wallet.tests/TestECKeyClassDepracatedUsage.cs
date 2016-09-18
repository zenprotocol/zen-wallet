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
	public class TestECKeyClassDepracatedUsage
	{
		[Test ()]
		public void test ()
		{
			uint256 hash = 1234567890;
			byte[] data = { 0, 0, 0, 25 };



			X9ECParameters Secp256k1 = NBitcoin.BouncyCastle.Crypto.EC.CustomNamedCurves.Secp256k1;
			ECDomainParameters DomainParameter;
			DomainParameter = new ECDomainParameters(Secp256k1.Curve, Secp256k1.G, Secp256k1.N, Secp256k1.H);



			//			int count = data.Length;
			//			byte[] vch = data.SafeSubarray(0, count); //SafeSubarray?
			byte[] vch = data;


			ECPrivateKeyParameters privatKey = new ECPrivateKeyParameters(new NBitcoin.BouncyCastle.Math.BigInteger(1, vch), DomainParameter);


			// Public key
			ECPoint ECPoint = Secp256k1.G.Multiply(privatKey.D);
			ECPublicKeyParameters publicKey = new ECPublicKeyParameters("EC", ECPoint, DomainParameter);



			var signer = new DeterministicECDSA();
			signer.setPrivateKey(privatKey);

			var signatureTemp = ECDSASignature.FromDER(signer.signHash(hash.ToBytes())).MakeCanonical();
			ECDSASignature signature = signatureTemp.MakeCanonical();



			////////////////////////////////////
			// Verifying
			////////////////////////////////////



			bool isSuccess = Verify (hash, signature, publicKey);

			Assert.AreEqual(true, isSuccess);
		}

		private bool Verify(uint256 hash, ECDSASignature sig, ECPublicKeyParameters key)
		{
			var signer = new ECDsaSigner();
			signer.Init(false, key);
			return signer.VerifySignature(hash.ToBytes(), sig.R, sig.S);
		}
	}
}

