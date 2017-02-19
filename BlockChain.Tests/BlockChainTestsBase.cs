using System;
using Consensus;
using BlockChain.Store;
using Infrastructure;
using System.Collections.Generic;
using System.Linq;
using BlockChain.Data;
using NUnit.Framework;
using System.IO;
using System.Reflection;

namespace BlockChain
{
	public class BlockChainTestsBase
	{
		const string DB = "temp";
		byte[] _GenesisBlockHash;
		IDisposable _TxMessagesListenerScope;
		Stack<BlockChainMessage> _BlockChainMessage = new Stack<BlockChainMessage>();

		protected BlockChain _BlockChain;
		protected Types.Block _GenesisBlock;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			Dispose();

			_TxMessagesListenerScope = MessageProducer<BlockChainMessage>.Instance.AddMessageListener(
				new MessageListener<BlockChainMessage>(m => _BlockChainMessage.Push(m)));

			_GenesisBlock = Infrastructure.Testing.Utils.GetGenesisBlock();
			_GenesisBlockHash = Merkle.blockHeaderHasher.Invoke(_GenesisBlock.header);
			_BlockChain = new BlockChain(DB, _GenesisBlockHash);
		}

		[OneTimeTearDown]
		public void Dispose()
		{
			if (_TxMessagesListenerScope != null)
			{
				_TxMessagesListenerScope.Dispose();
			}
			if (_BlockChain != null)
			{
				_BlockChain.Dispose();
			}
			if (Directory.Exists(DB))
			{
				Directory.Delete(DB, true);
			}
		}

		protected LocationEnum Location(Types.Block block)
		{
			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				var key = Merkle.blockHeaderHasher.Invoke(block.header);
				return _BlockChain.BlockStore.GetLocation(dbTx, key);
			}
		}

		protected TxStateEnum? LastTxState(Types.Transaction tx)
		{
			if (_BlockChainMessage.Count == 0)
				return null;
			
			var message = _BlockChainMessage.Pop();

			if (message is TxMessage)
			{
				var newTxStateMessage = (TxMessage)message;

				if (newTxStateMessage.TxHash.SequenceEqual(Merkle.transactionHasher.Invoke(tx)))
					return newTxStateMessage.State;
			}
			else if (message is BlockMessage)
			{
				return LastTxState(tx);
			}

			return null;
		}
	}
}
