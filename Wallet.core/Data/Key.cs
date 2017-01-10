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

		public override string ToString()
		{
			//var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
			return System.Convert.ToBase64String(Private);
		}

		public Key()
		{
		}

		public static Key Create(string base64EncodedPrivateKey = null)
		{
			var key = new Key();
			byte[] publicKey;

			if (base64EncodedPrivateKey == null)
			{
				var keyPair = PublicKeyAuth.GenerateKeyPair();

				key.Private = keyPair.PrivateKey;
				publicKey = keyPair.PublicKey;
			}
			else
			{
				key.Private = Convert.FromBase64String(base64EncodedPrivateKey);
				publicKey = PublicKeyAuth.ExtractEd25519PublicKeyFromEd25519SecretKey(key.Private);
				//return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
			}

			key.Address = Merkle.hashHasher.Invoke(publicKey);

			key.Change = false;
			key.Used = false;

			return key;
		}

		public bool IsMatch(Types.OutputLock outputLock)
		{
			var pkLock = outputLock as Types.OutputLock.PKLock;

			return pkLock != null && Address.SequenceEqual(pkLock.pkHash);
		}
	}
}
