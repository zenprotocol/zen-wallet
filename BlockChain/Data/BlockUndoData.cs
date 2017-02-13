using System;
using Consensus;
using System.Linq;
using Store;
using System.Collections.Generic;
using Microsoft.FSharp.Collections;
using BlockChain.Data;

namespace BlockChain.Data
{
	public class BlockUndoData
	{
		public List<Tuple<Types.Outpoint, Types.Output>> AddedUTXO { get; set; }
		public List<Tuple<Types.Outpoint, Types.Output>> RemovedUTXO { get; set; }
		public HashDictionary<ACSItem> ACSDeltas { get; set; }

		public BlockUndoData()
		{
			AddedUTXO = new List<Tuple<Types.Outpoint, Types.Output>>();
			RemovedUTXO = new List<Tuple<Types.Outpoint, Types.Output>>();
			ACSDeltas = new HashDictionary<ACSItem>();
		}
	}
}