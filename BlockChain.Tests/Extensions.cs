using System;
using Consensus;
using BlockChain.Store;
using Store;
using Infrastructure;
using System.Text;
using System.Collections.Generic;
using Microsoft.FSharp.Collections;
using System.Linq;
using BlockChain.Data;
using NUnit.Framework;
using System.IO;
using System.Reflection;
using Infrastructure.Testing;

namespace BlockChain
{
	public static class Extensions
	{
		public static Types.Block Tag(this Types.Block block, string value)
		{
			BlockChainTrace.SetTag(block, value);
			return block;
		}

		public static Types.Transaction Tag(this Types.Transaction tx, string value)
		{
			BlockChainTrace.SetTag(tx, value);
			return tx;
		}
	}
}
