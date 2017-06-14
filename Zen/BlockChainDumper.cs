using System;
using System.Collections.Generic;
using BlockChain.Data;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using Wallet.core;
using Consensus;

namespace Zen
{
	public enum GraphNodeTypeEnum
	{
		UTXOSet,
		MyWallet,
		MemPool,
		Genesis,
		Block,
		Tx,
		Output,
		Outpoint,
		Input,
		Missing
	}

	public class GraphNode
	{
		public string Title { get; set; }
		public GraphNodeTypeEnum GraphNodeType { get; set; }

		public GraphNode(GraphNodeTypeEnum graphNodeType, string title = null)
		{
			GraphNodeType = graphNodeType;
			Title = title;
		}
	}

	public class GraphLink
	{
		public byte[] GraphNode1 { get; set; }
		public byte[] GraphNode2 { get; set; }
	}

	public class Graph
	{
		public List<GraphNode> GraphNodes { get; set; }
		public List<GraphLink> GraphLinks { get; set; }

		public Graph()
		{
			GraphNodes = new List<GraphNode>();
			GraphLinks = new List<GraphLink>();
		}

		public void Reset()
		{
			GraphNodes.Clear();
			GraphLinks.Clear();
		}
	}

	public class D3Node
	{
		public string atom { get; set; }
		public int size { get; set; }
		public int color { get; set; }
	}

	public class D3Link
	{
		public int source { get; set; }
		public int target { get; set; }
		public int bond { get; set; }
	}

	public class D3Graph
	{
		public List<D3Node> nodes { get; set; }
		public List<D3Link> links { get; set; }

		public D3Graph()
		{
			nodes = new List<D3Node>();
			links = new List<D3Link>();
		}
	}

	public class BlockChainDumper
	{
		readonly BlockChain.BlockChain _BlockChain;
        readonly WalletManager _WalletManager;
		readonly Graph _Graph = new Graph();
		readonly HashDictionary<GraphNode> _Keys = new HashDictionary<GraphNode>();
		readonly HashDictionary<int> _Indexes = new HashDictionary<int>();
		readonly HashDictionary<byte[]> _Nodes = new HashDictionary<byte[]>();
		readonly HashDictionary<Types.Transaction> _Txs = new HashDictionary<Types.Transaction>();

		byte[] memPool;
		byte[] myWallet;
		//byte[] utxoSet;

		public BlockChainDumper(BlockChain.BlockChain blockChain, WalletManager walletManager)
		{
			_BlockChain = blockChain;
            _WalletManager = walletManager;
		}

		public void Populate()
		{
			_Graph.Reset();

			memPool = Encoding.ASCII.GetBytes(GraphNodeTypeEnum.MemPool.ToString());
			Add(memPool, new GraphNode(GraphNodeTypeEnum.MemPool));

			myWallet = Encoding.ASCII.GetBytes(GraphNodeTypeEnum.MyWallet.ToString());
			Add(myWallet, new GraphNode(GraphNodeTypeEnum.MyWallet));

			//utxoSet = Encoding.ASCII.GetBytes(GraphNodeTypeEnum.UTXOSet.ToString());
			//Add(utxoSet, new GraphNode(GraphNodeTypeEnum.UTXOSet));

			var tip = _BlockChain.Tip;

			if (tip != null)
			{
				var bkHash = tip.Key;
				var bk = tip.Value;
				byte[] lastKey = null;

				while (bk != null)
				{
					var graphNode = new GraphNode(bk.header.parent.Length == 0 ? GraphNodeTypeEnum.Genesis : GraphNodeTypeEnum.Block, "" + bk.header.blockNumber);
					Add(bkHash, graphNode);

					if (lastKey != null)
					{
						Link(bkHash, lastKey);
					}

					foreach (var tx in bk.transactions)
					{
						var txHash = Consensus.Merkle.transactionHasher.Invoke(tx);
						AddTx(txHash, tx, TxStateEnum.Confirmed);
						Link(txHash, bkHash);
					}

					lastKey = bkHash;
					bkHash = bk.header.parent;
					bk = new GetBlockAction() { BkHash = bkHash }.Publish().Result;
				}
			}

			foreach (var i in _BlockChain.memPool.TxPool)
			{
				AddTx(i.Key, Consensus.TransactionValidation.unpoint(i.Value), TxStateEnum.Unconfirmed);
			}

			LinkTxOutpoints();
			LinkMyWallet();
			LinkUTXO();
		}

		void LinkTxOutpoints()
		{
			foreach (var tx in _Txs.Values)
			{
				foreach (var outpoint in tx.inputs)
				{
					var outpointHash = Consensus.Merkle.outpointHasher.Invoke(outpoint);

					if (_Nodes.ContainsKey(outpointHash))
						Link(outpointHash, _Nodes[outpointHash]);
				}
			}
		}

		void LinkMyWallet()
		{
			var keys = _WalletManager.GetKeys();

			foreach (var tx in _Txs.Values)
			{
				foreach (var output in tx.outputs)
				{
					foreach (var key in keys)
					{
						if (key.Address.IsMatch(output.@lock))
						{
							var outputHash = Consensus.Merkle.outputHasher.Invoke(output);

							Link(outputHash, myWallet);
						}
					}
				}
			}
		}

		void LinkUTXO()
		{
			HashDictionary<List<Types.Output>> txOutputs;
			HashDictionary<Types.Transaction> txs;

			var result = new GetUTXOSetAction() { Predicate = null }.Publish().Result;

			txOutputs = result.Item1;
			txs = result.Item2;

			foreach (var tx in _Txs)
			{
				if (txs.ContainsKey(tx.Key))
				{
					foreach (var output in txOutputs[tx.Key])
					{
						var outputHash = Consensus.Merkle.outputHasher.Invoke(output);

						//Add(outputHash, new GraphNode(GraphNodeTypeEnum.Output), utxoSet);
					}
				}
			}
		}

		void AddTx(byte[] txHash, Consensus.Types.Transaction tx, TxStateEnum txState)
		{
			Add(txHash, new GraphNode(GraphNodeTypeEnum.Tx, txState.ToString()));

			uint i = 0;
			foreach (var output in tx.outputs)
			{
				var outputHash = Consensus.Merkle.outputHasher.Invoke(output);

				string text = output.@lock.IsContractLock ? "C" : "P";

                text += " " + _WalletManager.AssetsMetadata.Get(output.spend.asset).Result;
                text += " " + output.spend.amount;

				if (output.spend.asset.SequenceEqual(Consensus.Tests.zhash))
                {
					text += " Kalapas";
				}

				if (output.@lock is Types.OutputLock.ContractLock)
				{
					var data = ((Types.OutputLock.ContractLock)output.@lock).data;

					if (data != null)
						text += " " + Convert.ToBase64String(data);
				}

				var outputGraphNode = new GraphNode(GraphNodeTypeEnum.Output, text);
				Add(outputHash, outputGraphNode, txHash);

				var outpointHash = Consensus.Merkle.outpointHasher.Invoke(new Consensus.Types.Outpoint(txHash, i));
				_Nodes[outpointHash] = outputHash;
				i++;
			}

			foreach (var outpoint in tx.inputs)
			{
				var outpointHash = Consensus.Merkle.outpointHasher.Invoke(outpoint);
				Add(outpointHash, new GraphNode(GraphNodeTypeEnum.Outpoint), txHash);
			}

			if (txState == TxStateEnum.Unconfirmed)
				Link(txHash, memPool);

			_Txs.Add(txHash, tx);
		}

		public string Generate()
		{
			var d3Graph = new D3Graph();

			foreach (var graphNode in _Graph.GraphNodes)
			{
				d3Graph.nodes.Add(new D3Node()
				{
					atom = graphNode.GraphNodeType.ToString() + (graphNode.Title == null ? "" : " (" + graphNode.Title + ")"),
					color = GetColor(graphNode.GraphNodeType),
					size = GetSize(graphNode.GraphNodeType)
				});
			}

			foreach (var graphLink in _Graph.GraphLinks)
			{
				d3Graph.links.Add(new D3Link()
				{
					bond = 1,
					source = _Indexes[graphLink.GraphNode1],
					target = _Indexes[graphLink.GraphNode2]
				});
			}

			return JsonConvert.SerializeObject(d3Graph, Formatting.Indented);
		}

		void Add(byte[] key, GraphNode graphNode, byte[] parent = null)
		{
			if (!_Keys.ContainsKey(key))
			{
				_Indexes.Add(key, _Indexes.Count);
				_Keys.Add(key, graphNode);
				_Graph.GraphNodes.Add(graphNode);
			}

			if (parent != null)
				Link(parent, key);
		}

		void Link(byte[] ref1, byte[] ref2)
		{
			foreach (var _graphLink in _Graph.GraphLinks)
			{
				if (_graphLink.GraphNode1.SequenceEqual(ref1) &&
					_graphLink.GraphNode2.SequenceEqual(ref2) ||
				   _graphLink.GraphNode1.SequenceEqual(ref2) &&
					_graphLink.GraphNode2.SequenceEqual(ref1))
					return;
			}

			if (!_Keys.ContainsKey(ref1))
			{
				Add(ref1, new GraphNode(GraphNodeTypeEnum.Missing));
			}

			if (!_Keys.ContainsKey(ref2))
			{
				Add(ref2, new GraphNode(GraphNodeTypeEnum.Missing));
			}

			_Graph.GraphLinks.Add(new GraphLink()
			{
				GraphNode1 = ref1,
				GraphNode2 = ref2
			});
		}

		int GetColor(GraphNodeTypeEnum graphNodeType)
		{
			switch (graphNodeType)
			{
				case GraphNodeTypeEnum.Genesis:
					return 6;
				case GraphNodeTypeEnum.Block:
					return 1;
				case GraphNodeTypeEnum.Input:
					return 2;
				case GraphNodeTypeEnum.Output:
					return 3;
				case GraphNodeTypeEnum.Tx:
					return 4;
				default:
					return 0;
			}
		}

		int GetSize(GraphNodeTypeEnum graphNodeType)
		{
			switch (graphNodeType)
			{
				case GraphNodeTypeEnum.UTXOSet:
				case GraphNodeTypeEnum.MemPool:
				case GraphNodeTypeEnum.Genesis:
					return 60;
				case GraphNodeTypeEnum.Block:
					return 40;
				case GraphNodeTypeEnum.Input:
					return 15;
				case GraphNodeTypeEnum.Output:
					return 15;
				case GraphNodeTypeEnum.Tx:
					return 30;
				default:
					return 5;
			}
		}					
  	}
}
