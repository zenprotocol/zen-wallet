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
	public class TxAddedMessage
	{
		public Keyed<Types.Transaction> Tx { get; set; }
		public bool IsConfirmed { get; set; }

		public static void Publish(Keyed<Types.Transaction> tx, bool isConfirmed)
		{
			MessageProducer<TxAddedMessage>.Instance.PushMessage(new TxAddedMessage()
			{
				Tx = tx,
				IsConfirmed = isConfirmed
			});
		}
	}
	
}
