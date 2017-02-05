//using NUnit.Framework;
//using System;
//using Infrastructure.Testing;
//using Sodium;
//using Consensus;

//namespace BlockChain.Tests
//{
//	[TestFixture()]
//	public class BlockChainSimpleTests : BlockChainTestsBase
//	{
//		Key _Key;

//		[Test(), Order(1)]
//		public void ShouldAddGenesisWithTx()
//		{
//			_Key = Key.Create();

//			_GenesisBlock = _GenesisBlock.AddTx(Utils.GetTx().AddOutput(_Key.Address, Consensus.Tests.zhash, 100));
//			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock), Is.EqualTo(AddBk.Result.Added));
//		}

//		[Test(), Order(2)]
//		public void ShouldAddSpendingTxToMempool()
//		{
//			var spendingTx = Utils.GetTx().AddInput(_GenesisBlock.transactions[0], 0, _Key.Private);

//			Assert.That(_BlockChain.HandleNewTransaction(spendingTx), Is.EqualTo(AddBk.Result.Added));
//		}
//	}

//	public class Key
//	{
//		public byte[] Address { get; set; }
//		public byte[] Private { get; set; }
//		public bool Used { get; set; }
//		public bool Change { get; set; }

//		//public override string ToString()
//		//{
//		//	//var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
//		//	return System.Convert.ToBase64String(Private);
//		//}

//		public string PrivateAsString
//		{
//			get
//			{
//				return System.Convert.ToBase64String(Private);
//			}
//		}

//		public string AddressAsString
//		{
//			get
//			{
//				return System.Convert.ToBase64String(Address);
//			}
//		}

//		public Key()
//		{
//		}

//		public static Key Create(string base64EncodedPrivateKey = null)
//		{
//			byte[] privateKey;
//			byte[] publicKey;

//			if (string.IsNullOrEmpty(base64EncodedPrivateKey))
//			{
//				var keyPair = PublicKeyAuth.GenerateKeyPair();

//				privateKey = keyPair.PrivateKey;
//				publicKey = keyPair.PublicKey;
//			}
//			else
//			{
//				privateKey = FromBase64String(base64EncodedPrivateKey);
//				publicKey = PublicKeyAuth.ExtractEd25519PublicKeyFromEd25519SecretKey(privateKey);
//				//return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
//			}

//			return new Key()
//			{
//				Private = privateKey,
//				Address = Merkle.hashHasher.Invoke(publicKey),
//				Change = false,
//				Used = false
//			};
//		}

//		//public bool IsMatch(Types.OutputLock outputLock)
//		//{
//		//	var pkLock = outputLock as Types.OutputLock.PKLock;

//		//	return pkLock != null && Address != null && Address.SequenceEqual(pkLock.pkHash);
//		//}

//		public static byte[] FromBase64String(string base64Encoded)
//		{
//			return Convert.FromBase64String(base64Encoded);
//		}
//	}
//}
