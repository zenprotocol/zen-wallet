using System;
using Sodium;
using Store;
using System.Linq;
using Consensus;
using Microsoft.FSharp.Collections;

namespace Wallet.core.Data
{
	public enum AddressType
	{
		PK = '1',
		Contract = 'c',
		//ContractSacrifice = ''
	}

	public class Address
	{
		public AddressType AddressType { get; set; }
		public byte[] Bytes { get; set; }
		public byte[] Data { get; set; }

		public Types.OutputLock GetLock() {
			switch (AddressType)
			{
				case AddressType.PK:
					return Types.OutputLock.NewPKLock(Bytes);
				case AddressType.Contract:
					if (Data != null)
					{
						return Types.OutputLock.NewContractLock(Bytes, Data);
					}
					//case AddressType.ContractSacrifice:
					else
					{
						return Types.OutputLock.NewContractSacrificeLock(
							new Types.LockCore(0, ListModule.OfSeq(new byte[][] { Bytes }))
						);
					}
			}

			return null;
		}

		public Address(string raw)
		{
			AddressType = (AddressType)raw[0];
			Data = null;
			Bytes = Convert.FromBase64String(raw.Substring(1));
		}

		public Address(byte[] bytes, AddressType addressType)
		{
			Data = null;
			AddressType = addressType;
			Bytes = bytes;
		}

		public bool IsMatch(Types.OutputLock outputLock)
		{
			if (outputLock is Types.OutputLock.PKLock)
			{
				var pkLock = outputLock as Types.OutputLock.PKLock;

				return Bytes != null && Bytes.SequenceEqual(pkLock.pkHash);
			}
			else 
			{
				return false;
				//throw new NotImplementedException();
			}
		}

		public override string ToString()
		{
			return string.Format("{0}{1}", ((char)AddressType).ToString(), Convert.ToBase64String(Bytes));
		}
	}
	
	public class Key
	{
		public byte[] Public { get; set; }
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

		/// <summary>
		/// Public parameterless ctor is needed by MsgPack
		/// </summary>
		public Key() { }

		public Key(byte[] privateKey, byte[] publicKey)
		{
			Private = privateKey;
			Public = publicKey;
			Change = false;
			Used = false;
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

			return new Key(privateKey, publicKey);
		}

		public static byte[] FromBase64String(string base64Encoded)
		{
			return Convert.FromBase64String(base64Encoded);
		}

		public Address Address
		{
			get
			{
				return new Address(Merkle.innerHash(Public), AddressType.PK);
			}
		}
	}
}
