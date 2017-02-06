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
		public TransactionValidation.PointedTransaction Tx { get; set; }

		public static void Publish(TransactionValidation.PointedTransaction tx)
		{
			MessageProducer<TxAddedMessage>.Instance.PushMessage(new TxAddedMessage()
			{
				Tx = tx,
			});
		}
	}
	
	public class TxInvalidatedMessage
	{
		public TransactionValidation.PointedTransaction Tx { get; set; }

		public static void Publish(TransactionValidation.PointedTransaction tx)
		{
			MessageProducer<TxInvalidatedMessage>.Instance.PushMessage(new TxInvalidatedMessage()
			{
				Tx = tx,
			});
		}
	}
}
