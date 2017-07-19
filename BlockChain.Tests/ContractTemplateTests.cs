using System;
using NUnit.Framework;
using System.IO;
using System.Reflection;
using Microsoft.FSharp.Core;
using Newtonsoft.Json;

namespace BlockChain
{
	public class ContractTemplateTests
	{
		string GetTemplate(string name)
		{
			var ns = "BlockChain.Tests";
			var res = $"ContractTemplates.{name}.txt";

			using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Format("{0}.{1}", ns, res))))
			{
				return reader.ReadToEnd();
			}
		}

		[Test]
		public void CanCompileSecureTokenCodeFromTemplate()
		{
			var tpl = GetTemplate("SecureToken");

			var address = new Wallet.core.Data.Address("1rGUTQWEMgCt1fZoQ9gRzoyX8+AfSDRJHtflmCLenwaw=");

			var code = tpl.Replace("__ADDRESS__", Convert.ToBase64String(address.Bytes));

			var metadata = new { Type = "secure-token-generator" };
			code = code + "\n// " + JsonConvert.SerializeObject(metadata);

			var compiled = ContractExamples.Execution.compile(code);

			Assert.That(compiled, Is.Not.Null, "should compile code");
			Assert.That(FSharpOption<byte[]>.get_IsNone(compiled), Is.False, "should compile code");

		    Console.WriteLine(code);

			var metadataParsed = ContractExamples.Execution.metadata(code);
		}

		[Test]
		public void CanCompileCallOptionCodeFromTemplate()
		{
			var tpl = GetTemplate("CallOption");

			var address = new Wallet.core.Data.Address("1rGUTQWEMgCt1fZoQ9gRzoyX8+AfSDRJHtflmCLenwaw=");

			var code = tpl
			   .Replace("__numeraire__", Convert.ToBase64String(address.Bytes))
			   .Replace("__controlAsset__", Convert.ToBase64String(address.Bytes))
			   .Replace("__controlAssetReturn__", Convert.ToBase64String(address.Bytes))
			   .Replace("__oracle__", Convert.ToBase64String(address.Bytes))
			   .Replace("__underlying__", "GOOG")
			   .Replace("__price__", "10")
			   .Replace("__strike__", "900")
			   .Replace("__minimumCollateralRatio__", "1")
			   .Replace("__ownerPubKey__", Convert.ToBase64String(address.Bytes));

			var metadata = new { Type = "call-option" };
		//	code = code + "\n// " + JsonConvert.SerializeObject(metadata);

			var compiled = ContractExamples.Execution.compile(code);

			Assert.That(compiled, Is.Not.Null, "should compile code");
			Assert.That(FSharpOption<byte[]>.get_IsNone(compiled), Is.False, "should compile code");
		}
	}
}
