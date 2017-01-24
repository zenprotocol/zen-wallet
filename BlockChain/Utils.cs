using System;
using System.Collections.Generic;
using BlockChain.Data;
using Consensus;
using Microsoft.FSharp.Collections;
using NUnit.Framework;
using System.Linq;
using Store;

namespace BlockChain
{
	public class Utils
	{
		public static Keyed<Types.Block> FindFork(
			Keyed<Types.Block> mainChainBlock, 
			Keyed<Types.Block> sideChainBlock,
			Func<byte[], Keyed<Types.Block>> getBlock)
		{
			var passedThroughList = new List<byte[]>();

			while (true)
			{
				mainChainBlock = getBlock(mainChainBlock.Value.header.parent);
				passedThroughList.Add(mainChainBlock.Key);

				sideChainBlock = getBlock(sideChainBlock.Value.header.parent);

				if (passedThroughList.Contains(sideChainBlock.Key))
				{
					return sideChainBlock;
				}
			}
		}

		public static List<Keyed<Types.Block>> BlocksList(
			Keyed<Types.Block> from, 
			byte[] to, 
			Func<byte[], Keyed<Types.Block>> getBlock)
		{
			var list = new List<Keyed<Types.Block>>();

			while (!from.Key.SequenceEqual(to))
			{
				list.Add(from);
				from = getBlock(from.Value.header.parent);
			}

			return list;
		}
	}

	[TestFixture()]
	public class UtilTests
	{
		private readonly Random _Random = new Random();

		[Test()]
		public void CanFindFork()
		{
			var block1 = GetBlock();
			var block2 = GetBlock(block1);
			var block3 = GetBlock(block2);
			var block4 = GetBlock(block2);

			var dict = new HashDictionary<Keyed<Types.Block>>();
			dict[block1.Key] = block1;
			dict[block2.Key] = block2;
			dict[block3.Key] = block3;
			dict[block4.Key] = block4;

			var fork = Utils.FindFork(block3, block4, hash => dict[hash] );

			Assert.That(fork, Is.EqualTo(block2));
		}
        
		[Test()]
		public void CanGetBlocksList()
		{
			var block1 = GetBlock();
			var block2 = GetBlock(block1);
			var block3 = GetBlock(block2);

			var dict = new HashDictionary<Keyed<Types.Block>>();
			dict[block1.Key] = block1;
			dict[block2.Key] = block2;
			dict[block3.Key] = block3;

			var expectedList = new List<Keyed<Types.Block>>();
			expectedList.Add(block2);
			expectedList.Add(block3);

			var list = Utils.BlocksList(block3, block1.Key, hash => dict[hash]);

			Assert.That(list, Is.EquivalentTo(expectedList));
		}

        private byte[] GetHash(Types.Block block)
		{
			return Merkle.blockHeaderHasher.Invoke(block.header);
		}

		private Keyed<Types.Block> GetBlock(Keyed<Types.Block> parent)
		{
			return GetBlock(parent.Key);
		}

		private Keyed<Types.Block> GetBlock(Types.Block parent)
		{
			return GetBlock(GetHash(parent));
		}

		private Keyed<Types.Block> GetBlock(byte[] parent = null)
		{
			var nonce = new byte[10];

			_Random.NextBytes(nonce);

			var header = new Types.BlockHeader(
				0,
				parent ?? new byte[] { },
				0,
				new byte[] { },
				new byte[] { },
				new byte[] { },
				ListModule.OfSeq<byte[]>(new List<byte[]>()),
				0,
				0,
				nonce
			);

			var block = new Types.Block(header, ListModule.OfSeq<Types.Transaction>(new List<Types.Transaction>()));
			var key = Merkle.blockHeaderHasher.Invoke(header);

			return new Keyed<Types.Block>(key, block); 
		}
	}
}
