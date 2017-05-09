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

#if CSHARP_CONTRACTS
using Microsoft.CSharp;
using System.CodeDom.Compiler;
#endif

namespace BlockChain
{
	public class ContractArgs
	{
		public byte[] ContractHash { get; set; }
		public SortedDictionary<Types.Outpoint, Types.Output> Utxos { get; set; }
		public byte[] Message { get; set; }
	}

	public class ContractHelper
	{
		const string _OutputPath = "contracts";
		const string DEPENCENCY_OPTION = " -r ";

		static readonly string[] _Dependencies = new string[] {
#if CSHARP_CONTRACTS
            "System.dll",
            "FSharp.Core.dll",
#else
#endif
			"../../../Consensus/bin/Debug/Consensus.dll",
		//	"/usr/lib/cli/nunit.framework-2.6.3/nunit.framework.dll",
			"../../../Consensus/bin/Debug/MsgPack.dll",
			"../../../Consensus/bin/Debug/BouncyCastle.Crypto.dll"
		}; //TODO

		//static string _CompilerPath = "/usr/lib/mono/4.5/fsc.exe"; 
		static string _CompilerPath = "";

		public static bool Execute(out Types.Transaction transaction, ContractArgs contractArgs)
		{
			try
			{
				var assembly = Assembly.LoadFrom(GetFileName(contractArgs.ContractHash));
				var module = assembly.GetModules()[0];
				var type = module.GetTypes()[0];
				var method = type.GetMethod("run");
				var args = new object[] {
					contractArgs.ContractHash,
#if CSHARP_CONTRACTS
					contractArgs.Utxos,
#else
					new FSharpMap<Types.Outpoint, Types.Output>(contractArgs.Utxos.ToList().Select(t=>new Tuple<Types.Outpoint, Types.Output>(t.Key, t.Value))),
#endif
					contractArgs.Message
				};
				var result = method.Invoke(null, args);
				var txSkeleton = result as Tuple<IEnumerable<Types.Outpoint>, IEnumerable<Types.Output>, FSharpOption<Types.ExtendedContract>>;
				var _txSkeleton = result as Tuple<IEnumerable<Types.Outpoint>, IEnumerable<Types.Output>>;

				transaction = result == null ? null :
					new Types.Transaction(
						Tests.tx.version,
						ListModule.OfSeq(txSkeleton.Item1),
						ListModule.OfSeq<byte[]>(new byte[][] { }),
						ListModule.OfSeq(txSkeleton.Item2),
						txSkeleton.Item3
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

		public static bool Compile(byte[] fsharpCode, out byte[] contractHash)
		{
			return Compile(Encoding.ASCII.GetString(fsharpCode), out contractHash);
		}

		//		public async static Task<bool> Extract(byte[] fstarCode, StrongBox<byte[]> fsharpCode)
		public static bool Extract(byte[] fstarCode, out byte[] fsharpCode)
		{
			//	await Task.Delay(1000);
//			var fsharpCodeExtracted = @"
//module Test
//open Consensus.Types
//let run (context : ContractContext, message: byte[], outputs: Output list) = (context.utxo |> Map.toSeq |> Seq.map fst, outputs)
//";
//			fsharpCode = Encoding.ASCII.GetBytes(fsharpCodeExtracted);

			fsharpCode = fstarCode;
			return true;
		}

		#if CSHARP_CONTRACTS
		public static bool Compile(String csharpCode, out byte[] contractHash)
		{
			var provider = new CSharpCodeProvider();
			var parameters = new CompilerParameters();

			foreach (var dependency in _Dependencies)
			{
				parameters.ReferencedAssemblies.Add(dependency);
			}

			if (!Directory.Exists(_OutputPath))
			{
				Directory.CreateDirectory(_OutputPath);
			}
			contractHash = GetHash(csharpCode);

			parameters.GenerateInMemory = false;
			parameters.GenerateExecutable = false;
			parameters.OutputAssembly = GetFileName(contractHash); 
			var results = provider.CompileAssemblyFromSource(parameters, csharpCode);

			if (results.Errors.HasErrors)
			{
				StringBuilder sb = new StringBuilder();

				foreach (CompilerError error in results.Errors)
				{
					sb.AppendLine(String.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText));
				}

				BlockChainTrace.Error(sb.ToString(), new Exception());
				//throw new InvalidOperationException(sb.ToString());
			}

			return true;
		}
		#else
		public static bool Compile(String fsharpCode, out byte[] contractHash)
		{
			var tempSourceFile = Path.ChangeExtension(Path.GetTempFileName(), ".fs");
			var process = new Process();

			contractHash = GetHash(fsharpCode);

			File.WriteAllText(tempSourceFile, fsharpCode);

			if (!Directory.Exists(_OutputPath))
			{
				Directory.CreateDirectory(_OutputPath);
			}

			if (IsRunningOnMono())
			{
#if LINUX
				process.StartInfo.FileName = "mono";
				process.StartInfo.Arguments = $"{ Path.Combine(_CompilerPath, "fsc.exe") } -o { GetFileName(contractHash) } -a {tempSourceFile}{DEPENCENCY_OPTION + string.Join(DEPENCENCY_OPTION, _Dependencies)}";
#else
				process.StartInfo.FileName = "fsharpc";
				process.StartInfo.Arguments = $"-o { GetFileName(contractHash) } -a {tempSourceFile}{DEPENCENCY_OPTION + string.Join(DEPENCENCY_OPTION, _Dependencies)}";
#endif
			}
			else
			{
				//TODO
			}

			process.OutputDataReceived += (sender, args1) =>
			{
			    Console.WriteLine("## " + args1.Data);
			};

			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;

			process.OutputDataReceived += (sender, args1) =>
			{
				BlockChainTrace.Information(args1.Data);
			};

			try
			{
				process.Start();
				process.BeginOutputReadLine();
				process.WaitForExit();
			}
			catch (Exception e)
			{
				BlockChainTrace.Error("process", e);
			}

			File.Delete(tempSourceFile);

			return process.ExitCode == 0;
		}
		#endif

		private static byte[] GetHash(string value)
		{
			return Merkle.innerHash(Encoding.ASCII.GetBytes(value));
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
