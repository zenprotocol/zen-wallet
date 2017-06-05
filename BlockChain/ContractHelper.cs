using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Consensus;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using System.Linq;
using System.Diagnostics;
using System.Web;

#if CSHARP_CONTRACTS
using Microsoft.CSharp;
using System.CodeDom.Compiler;
#endif

namespace BlockChain
{
    public class ContractArgs
    {
        public byte[] ContractHash { get; set; }
        public Func<Types.Outpoint, FSharpOption<Types.Output>> tryFindUTXOFunc { get; set; }
        public byte[] Message { get; set; }
    }

    public class ContractHelper
    {
        const string _OutputPath = "contracts";
        const string DEPENCENCY_OPTION = " -r ";

        static readonly string[] _Dependencies = new string[] {
#if CSHARP_CONTRACTS
            "System.dll",
            "System.Core.dll",
            "FSharp.Core.dll",
#else
#endif
			"Consensus.dll",
		//	"/usr/lib/cli/nunit.framework-2.6.3/nunit.framework.dll",
			"MsgPack.dll",
            "BouncyCastle.Crypto.dll",
            "ContractsUtils.dll"
        }; //TODO


        public static bool Execute(out Types.Transaction transaction, ContractArgs contractArgs, bool isWitness)
        {
			try
			{
				var fileName = HttpServerUtility.UrlTokenEncode(contractArgs.ContractHash);
				var contractCode = File.ReadAllText(Path.Combine(_OutputPath, Path.ChangeExtension(fileName, ".fs")));
				var func = ContractExamples.Execution.compileContract(contractCode);

                var result = func.Invoke(new Tuple<byte[], byte[], FSharpFunc<Types.Outpoint, FSharpOption<Types.Output>>>(
                    contractArgs.Message,
				    contractArgs.ContractHash,
                    FSharpFunc<Types.Outpoint, FSharpOption<Types.Output>>.FromConverter(t => contractArgs.tryFindUTXOFunc(t))));

				var txSkeleton = result as Tuple<FSharpList<Types.Outpoint>, FSharpList<Types.Output>, byte[]>;

				transaction = txSkeleton == null || txSkeleton.Item2.Count() == 0 ? null :
					new Types.Transaction(
						Tests.tx.version,
						txSkeleton.Item1,
						ListModule.OfSeq<byte[]>(isWitness ? new byte[][] { contractArgs.Message } : new byte[][] { }),
						txSkeleton.Item2,
						FSharpOption<Types.ExtendedContract>.None //TODO: get from txSkeleton.Item3
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

        public static bool Execute_old(out Types.Transaction transaction, ContractArgs contractArgs, bool isWitness)
        {
            try
            {
                var assembly = Assembly.LoadFrom(GetFileName(contractArgs.ContractHash));
                var module = assembly.GetModules()[0];
                var type = module.GetTypes()[0];

				//**************************************************
				// used for CSharp based contract debugging
				var matchedTypes = Assembly.GetEntryAssembly()
				    .GetModules()[0]
				    .GetTypes()
				    .Where(t => t.FullName == type.FullName);

				if (matchedTypes.Any())
				{
				    type = matchedTypes.First();
				}

				//**************************************************
				// used for FSharp based contract debugging
				//var matchedTypes = Assembly.LoadFile("TestFSharpContracts.dll")
				//    .GetModules()[0]
				//    .GetTypes()
				//    .Where(t => t.FullName == type.FullName);

				//if (matchedTypes.Any())
				//{
				//    type = matchedTypes.First();
				//}

				var method = type.GetMethod("main");
                var args = new object[] {
#if CSHARP_CONTRACTS
                    new List<byte>(contractArgs.Message),
                    contractArgs.ContractHash,
                    contractArgs.tryFindUTXOFunc,
#else
                    contractArgs.Message,
                    contractArgs.ContractHash,
                    FSharpFunc<Types.Outpoint, FSharpOption<Types.Output>>.FromConverter(t => contractArgs.tryFindUTXOFunc(t))
#endif
				};

                var result = method.Invoke(null, args);

#if CSHARP_CONTRACTS
                var txSkeleton = result as Tuple<IEnumerable<Types.Outpoint>, IEnumerable<Types.Output>, byte[]>;
#else
                var txSkeleton = result as Tuple<FSharpList<Types.Outpoint>, FSharpList<Types.Output>, byte[]>;
#endif

                transaction = txSkeleton == null || txSkeleton.Item2.Count() == 0 ? null :
                    new Types.Transaction(
                        Tests.tx.version,
#if CSHARP_CONTRACTS
                        ListModule.OfSeq(txSkeleton.Item1),
#else
						txSkeleton.Item1,
#endif
						ListModule.OfSeq<byte[]>(isWitness ? new byte[][] { contractArgs.Message } : new byte[][] { }),
#if CSHARP_CONTRACTS
						ListModule.OfSeq(txSkeleton.Item2),
#else
                        txSkeleton.Item2,
#endif
						FSharpOption<Types.ExtendedContract>.None //TODO: get from txSkeleton.Item3
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

                return false;
			}

			return true;
		}
#else
        public static bool Compile(String fsharpCode, out byte[] contractHash)
        {
			contractHash = GetHash(fsharpCode);

			if (!Directory.Exists(_OutputPath))
			{
				Directory.CreateDirectory(_OutputPath);
			}

			var fileName = HttpServerUtility.UrlTokenEncode(contractHash);

			var contractSourceFile = Path.Combine(_OutputPath, Path.ChangeExtension(fileName, ".fs"));

			File.WriteAllText(contractSourceFile, fsharpCode);

            return true;
        }

        public static bool Compile_old(String fsharpCode, out byte[] contractHash)
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
				process.StartInfo.FileName = "fsharpc";
				process.StartInfo.Arguments = $"-o { GetFileName(contractHash) } -a {tempSourceFile}{DEPENCENCY_OPTION + string.Join(DEPENCENCY_OPTION, _Dependencies)}";
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
