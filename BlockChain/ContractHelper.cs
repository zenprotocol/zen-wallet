using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Consensus;
using Microsoft.FSharp.Collections;

namespace BlockChain
{
	public class ContractHelper
	{
		private const string _OutputPath = "contracts";
		private const string DEPENCENCY_FLAG = " -r ";

		private static readonly string[] _Dependencies = new string[] {
				"/home/user/repo/zen-wallet/Consensus/bin/Debug/Consensus.dll",
				"/usr/lib/cli/nunit.framework-2.6.3/nunit.framework.dll",
				"/home/user/repo/zen-wallet/Consensus/bin/Debug/MsgPack.dll",
				"/home/user/repo/zen-wallet/Consensus/bin/Debug/BouncyCastle.Crypto.dll"
			}; //TODO

		private static string _CompilerPath = "/usr/lib/cli/fsharp/"; //TODO

		public static bool Execute(byte[] contractHash, out Types.Transaction transaction, List<Types.Output> outputsList)
		{
			try
			{
				var assembly = Assembly.LoadFrom(GetFileName(contractHash));
				var module = assembly.GetModules()[0];
				var type = module.GetTypes()[0];
				var method = type.GetMethod("run");

				var args = new object[] { //TODO
				//	new Types.ContractContext(1999),
					ListModule.OfSeq<Types.Output>(outputsList)
				};

				var result = (Tuple<FSharpList<Types.Outpoint>, FSharpList<Types.Output>, byte[]>) method.Invoke(null, args);

				transaction = new Types.Transaction(
					Consensus.Tests.tx.version,
					result.Item1,
					Consensus.Tests.tx.witnesses,
					result.Item2,
					Consensus.Tests.tx.contract
				);

				return true;
			}
			catch // (Exception e)
			{
			}
			transaction = null;
			return false;
		}

		public static byte[] Compile(byte[] fsSourceCodeBytes)
		{
			return Compile(Encoding.ASCII.GetString(fsSourceCodeBytes));
		}

		public static byte[] Compile(String fsSourceCodeText)
		{
			var tempSourceFile = Path.ChangeExtension(Path.GetTempFileName(), ".fs");
			var hash = GetHash(fsSourceCodeText);
			var process = new Process();

			File.WriteAllText(tempSourceFile, fsSourceCodeText);

			if (!Directory.Exists(_OutputPath))
			{
				Directory.CreateDirectory(_OutputPath);
			}

			if (IsRunningOnMono())
			{
				process.StartInfo.FileName = "mono";
				process.StartInfo.Arguments = $@"
					{ Path.Combine(_CompilerPath, "fsc.exe") }
					-o { GetFileName(hash) }
					-a {tempSourceFile}{DEPENCENCY_FLAG + string.Join(DEPENCENCY_FLAG, _Dependencies)}";
			}
			else
			{
				//TODO
			}

			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			//process.OutputDataReceived += (sender, args1) =>
			//{
			//	Console.WriteLine(args1.Data);
			//};
			process.Start();
			process.BeginOutputReadLine();
			process.WaitForExit();
			File.Delete(tempSourceFile);

			return process.ExitCode == 0 ? hash : null;
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
