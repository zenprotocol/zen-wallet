using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Consensus;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using System.Linq;

namespace BlockChain
{
	public class ContractArgs
	{
		public Types.ContractContext context { get; set; }
		public List<byte[]> witnesses { get; set; }
		public List<Types.Output> outputs { get; set; }
		public Types.ExtendedContract option { get; set; }
	}

	public class ContractHelper
	{
		const string _OutputPath = "contracts";
		const string DEPENCENCY_OPTION = " -r ";

		static readonly string[] _Dependencies = new string[] {
				"/home/user/repo/zen-wallet/Consensus/bin/Debug/Consensus.dll",
				"/usr/lib/cli/nunit.framework-2.6.3/nunit.framework.dll",
				"/home/user/repo/zen-wallet/Consensus/bin/Debug/MsgPack.dll",
				"/home/user/repo/zen-wallet/Consensus/bin/Debug/BouncyCastle.Crypto.dll"
			}; //TODO

		static string _CompilerPath = "/usr/lib/mono/4.5/"; //TODO

		public static bool Execute(byte[] contractHash, out Types.Transaction transaction, ContractArgs contractArgs)
		{
			try
			{
				var assembly = Assembly.LoadFrom(GetFileName(contractHash));
				var module = assembly.GetModules()[0];
				var type = module.GetTypes()[0];
				var method = type.GetMethod("run");

				var args = new object[] {
					contractArgs.context,
				//	ListModule.OfSeq(contractArgs.inputs),
					ListModule.OfSeq(contractArgs.witnesses),
					ListModule.OfSeq(contractArgs.outputs),
					contractArgs.option
				};

				var result =
					(Tuple<IEnumerable<Types.Outpoint>, FSharpList<byte[]>, FSharpList<Types.Output>, Types.ExtendedContract>) 
					method.Invoke(null, args);

				transaction =
					new Types.Transaction(
					Tests.tx.version,
					ListModule.OfSeq(result.Item1),
					result.Item2,
					result.Item3,
					new FSharpOption<Types.ExtendedContract>(result.Item4)
				);

				return true;
			}
			catch (Exception e)
			{
				BlockChainTrace.Error("Error executing contract", e);
			}
			transaction = null;
			return false;
		}

		public static bool IsTxValid(TransactionValidation.PointedTransaction ptx, byte[] contractHash, List<Tuple<Types.Outpoint, Types.Output>> utxos)
		{
			var args = new ContractArgs()
			{
				context = new Types.ContractContext(contractHash, new FSharpMap<Types.Outpoint, Types.Output>(utxos)),
				witnesses = new List<byte[]>(),
				outputs = ptx.outputs.ToList(),
				option = Types.ExtendedContract.NewContract(new Types.Contract(new byte[] { }, new byte[] { }, new byte[] { }))
			};

			Types.Transaction tx;
			return Execute(contractHash, out tx, args) && TransactionValidation.unpoint(ptx).Equals(tx);
		}

		public static bool Compile(byte[] fsSourceCodeBytes, out byte[] contractHash)
		{
			return Compile(Encoding.ASCII.GetString(fsSourceCodeBytes), out contractHash);
		}

		public static bool Compile(String fsSourceCodeText, out byte[] contractHash)
		{
			var tempSourceFile = Path.ChangeExtension(Path.GetTempFileName(), ".fs");
			var process = new Process();

			contractHash = GetHash(fsSourceCodeText);

			File.WriteAllText(tempSourceFile, fsSourceCodeText);

			if (!Directory.Exists(_OutputPath))
			{
				Directory.CreateDirectory(_OutputPath);
			}

			if (IsRunningOnMono())
			{
				process.StartInfo.FileName = "mono";
				process.StartInfo.Arguments = $"{ Path.Combine(_CompilerPath, "fsc.exe") } -o { GetFileName(contractHash) } -a {tempSourceFile}{DEPENCENCY_OPTION + string.Join(DEPENCENCY_OPTION, _Dependencies)}";

			//	Console.WriteLine();
			//	Console.WriteLine(process.StartInfo.Arguments);
			}
			else
			{
				//TODO
			}

			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;

			process.OutputDataReceived += (sender, args1) =>
			{
				BlockChainTrace.Information(args1.Data);
			};

			process.Start();
			process.BeginOutputReadLine();
			process.WaitForExit();
			File.Delete(tempSourceFile);

			return process.ExitCode == 0;
		}

		private static byte[] GetHash(string value)
		{
			return Merkle.hashHasher.Invoke(Encoding.ASCII.GetBytes(value)); //TODO: hashHasher?
		}

		private static string GetFileName(byte[] hash)
		{
			return Path.Combine(_OutputPath, BitConverter.ToString(hash).Replace("-", "") + ".dll");
		}

		private static bool IsRunningOnMono()
		{
			return Type.GetType("Mono.Runtime") != null;
		}
	}
}
