using System;
using System.Diagnostics;
using System.Text;
using BlockChain.Data;

namespace BlockChain
{
	public static class BlockChainTrace
	{
		static TraceSource _Trace = new TraceSource("BlockChain");

		internal static void Information(string info)
		{
			_Trace.TraceEvent(TraceEventType.Information, 0, info);
		}

		static HashDictionary<string> tags = new HashDictionary<string>();

		public static void SetTag(Consensus.Types.Transaction tx, string value)
		{
			SetTag(Consensus.Merkle.transactionHasher.Invoke(tx), value);
		}

		public static void SetTag(Consensus.Types.Block bk, string value)
		{
			SetTag(Consensus.Merkle.blockHeaderHasher.Invoke(bk.header), value);
		}

		public static void SetTag(byte[] key, string value)
		{
			tags[key] = value;
		}

		internal static void Information(string info, Consensus.TransactionValidation.PointedTransaction ptx)
		{
			Information(info, Consensus.TransactionValidation.unpoint(ptx));
		}

		internal static void Information(string info, Consensus.Types.Transaction tx)
		{
			Information(info, Consensus.Merkle.transactionHasher.Invoke(tx));
		}

		internal static void Information(string info, Consensus.Types.Block bk)
		{
			Information(info, Consensus.Merkle.blockHeaderHasher.Invoke(bk.header));
		}

		internal static void Information(string info, byte[] key)
		{
			_Trace.TraceEvent(TraceEventType.Information, 0, key != null && tags.ContainsKey(key) ? info + " (" + tags[key] + ")" : info);
		}

		internal static void Error(string msg, Exception ex)
		{
			_Trace.TraceEvent(TraceEventType.Error, 0, msg + " " + Infrastructure.Utils.ExceptionToString(ex));
		}

		internal static void Verbose(string str)
		{
			_Trace.TraceEvent(TraceEventType.Verbose, 0, str);
		}
	}
}