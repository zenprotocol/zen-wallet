using System;
using Sodium;
using Store;
using System.Linq;
using Consensus;

namespace Wallet.core.Data
{
	public class Key
	{
		public byte[] Address { get; set; }
		public byte[] Private { get; set; }
		public bool Used { get; set; }
		public bool Change { get; set; }

		//public override string ToString()
		//{
		//	//var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
		//	return System.Convert.ToBase64String(Private);
		//}

		public string PrivateAsString
		{
			get 
			{
				return System.Convert.ToBase64String(Private);
			}
		}

		public string AddressAsString 
		{
			get
			{
				return System.Convert.ToBase64String(Address);
			}
		}

		Key()
		{
		}

		public static Key Create(string base64EncodedPrivateKey = null)
		{
			byte[] privateKey;
			byte[] publicKey;

			if (string.IsNullOrEmpty(base64EncodedPrivateKey))
			{
				var keyPair = PublicKeyAuth.GenerateKeyPair();

				privateKey = keyPair.PrivateKey;
				publicKey = keyPair.PublicKey;
			}
			else
			{
				privateKey = FromBase64String(base64EncodedPrivateKey);
				publicKey = PublicKeyAuth.ExtractEd25519PublicKeyFromEd25519SecretKey(privateKey);
				//return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
			}

			return new Key()
			{
				Private = privateKey,
				Address = Merkle.hashHasher.Invoke(publicKey),
				Change = false,
				Used = false
			};
		}

		public bool IsMatch(Types.OutputLock outputLock)
		{
			var pkLock = outputLock as Types.OutputLock.PKLock;

			return pkLock != null && Address != null && Address.SequenceEqual(pkLock.pkHash);
		}

		public static byte[] FromBase64String(string base64Encoded)
		{
			return Convert.FromBase64String(base64Encoded);
		}
	}
}
