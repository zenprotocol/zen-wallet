using System;
using System.Collections.Generic;
using BlockChain.Data;

namespace Zen
{
	public enum GraphNodeTypeEnum
	{
		Block,
		Tx,
		Output,
		Input
	}

	public class GraphNode
	{
		//public string Title { get; set; }
		//public int Size { get; set; }
		public GraphNodeTypeEnum GraphNodeTypes { get; set; }

		public GraphNode(GraphNodeTypeEnum graphNodeTypes)
		{
			GraphNodeTypes = graphNodeTypes;
		}
	}

	public class GraphLink
	{
		public GraphNode GraphNode1 { get; set; }
		public GraphNode GraphNode2 { get; set; }
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
	}

	public class EmptyClass
	{
		BlockChain.BlockChain _BlockChain;
		Graph _Graph;
		HashDictionary<GraphNode> Keys;

		public void x()
		{
			var tip = _BlockChain.Tip;

			if (tip != null)
			{
				var bkHash = tip.Key;
				var bk = tip.Value;

				while (bkHash != null)
				{
					Add(bkHash, new GraphNode(GraphNodeTypeEnum.Block);

					bkHash = tip.Value.header.parent;
					bk = _BlockChain.GetBlock(bkHash);
				}
			}

			foreach (var i in _BlockChain.memPool.TxPool)
			{
				Add(i.Key, new GraphNode(GraphNodeTypeEnum.Tx));
			}
		}

		void Add(byte[] key, GraphNode graphNode)
		{
			Keys.Add(key, graphNode);
			_Graph.GraphNodes.Add(graphNode);
		}
	}
}
