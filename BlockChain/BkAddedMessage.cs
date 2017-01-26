using System;
using Consensus;
using BlockChain.Store;
using Store;
using Infrastructure;
using System.Collections.Generic;
using Microsoft.FSharp.Collections;
using System.Linq;
using BlockChain.Data;
using System.Collections.Concurrent;

namespace BlockChain
{

	public class BkAddedMessage
	{
		public Keyed<Types.Block> Bk { get; set; }

		public static void Publish(Keyed<Types.Block> bk)
		{
			MessageProducer<BkAddedMessage>.Instance.PushMessage(new BkAddedMessage()
			{
				Bk = bk
			});
		}
	}
	
}
