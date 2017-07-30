﻿using System;
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
            var destinationAddress = Wallet.core.Data.Key.Create().Address.Bytes;
            var destination = Convert.ToBase64String(destinationAddress);
            var metadata = new { contractType = "securetoken", destination = destination };
            var jsonHeader = "//" + JsonConvert.SerializeObject(metadata);
            var code = tpl.Replace("__ADDRESS__", destination);
            code += "\n" + jsonHeader;
            var compiled = ContractExamples.Execution.compile(code);

			Assert.That(compiled, Is.Not.Null, "should compile code");
			Assert.That(FSharpOption<byte[]>.get_IsNone(compiled), Is.False, "should compile code");

			var metadataParsed = ContractExamples.Execution.metadata(code);

            Assert.That(FSharpOption<ContractExamples.Execution.ContractMetadata>.get_IsNone(metadataParsed), Is.False, "should parse metadata");
            Assert.That(metadataParsed.Value, Is.TypeOf(typeof(ContractExamples.Execution.ContractMetadata.SecureToken)));
            Assert.That((metadataParsed.Value as ContractExamples.Execution.ContractMetadata.SecureToken).Item.destination, Is.EquivalentTo(destinationAddress));

            object deserialized = null;

            try
            {
                deserialized = ContractExamples.Execution.deserialize(compiled.Value);
            }
            catch 
            {
                
            }
            Assert.That(deserialized, Is.Not.Null);
        }

		[Test]
		public void CanCompileCallOptionCodeFromTemplate()
		{
			var tpl = GetTemplate("CallOption");

            var controlAssetReturn = Wallet.core.Data.Key.Create().Address.Bytes;
            var controlAsset = Wallet.core.Data.Key.Create().Address.Bytes;
            var oracleAddress = Wallet.core.Data.Key.Create().Address.Bytes;
            var underlying = "GOOG";
            var price = 10M;
            var strike = 900M;
            var minimumCollateralRatio = 1;
            var ownerPubKey = Wallet.core.Data.Key.Create().Public;

            var callOptionsParams = new ContractExamples.QuotedContracts.CallOptionParameters(
                Consensus.Tests.zhash,
                controlAsset,
                controlAssetReturn,
                oracleAddress,
                underlying,
                price,
                strike,
                minimumCollateralRatio,
                ownerPubKey
            );

            var code = tpl
                .Replace("__numeraire__", Convert.ToBase64String(callOptionsParams.numeraire))
                .Replace("__controlAsset__", Convert.ToBase64String(callOptionsParams.controlAsset))
                .Replace("__controlAssetReturn__", Convert.ToBase64String(callOptionsParams.controlAssetReturn))
                .Replace("__oracle__", Convert.ToBase64String(callOptionsParams.oracle))
                .Replace("__underlying__", callOptionsParams.underlying)
                .Replace("__price__", "" + callOptionsParams.price)
                .Replace("__strike__", "" + callOptionsParams.strike)
                .Replace("__minimumCollateralRatio__", "" + callOptionsParams.minimumCollateralRatio)
                .Replace("__ownerPubKey__", Convert.ToBase64String(callOptionsParams.ownerPubKey));

			var metadata = new { 
				contractType = "calloption", 
                numeraire = Convert.ToBase64String(callOptionsParams.numeraire),
				controlAsset = Convert.ToBase64String(callOptionsParams.controlAsset),
                controlAssetReturn = Convert.ToBase64String(callOptionsParams.controlAssetReturn),
                oracle = Convert.ToBase64String(callOptionsParams.oracle),
				underlying = callOptionsParams.underlying,
				price = "" + callOptionsParams.price,
				strike = "" + callOptionsParams.strike,
                minimumCollateralRatio = "" + callOptionsParams.minimumCollateralRatio,
                ownerPubKey = Convert.ToBase64String(callOptionsParams.ownerPubKey)
            };

			var jsonHeader = "//" + JsonConvert.SerializeObject(metadata);
            code += "\n" + jsonHeader;

			var compiled = ContractExamples.Execution.compile(code);

			Assert.That(compiled, Is.Not.Null, "should compile code");
			Assert.That(FSharpOption<byte[]>.get_IsNone(compiled), Is.False, "should compile code");

			var metadataParsed = ContractExamples.Execution.metadata(code);

			Assert.That(FSharpOption<ContractExamples.Execution.ContractMetadata>.get_IsNone(metadataParsed), Is.False, "should parse metadata");
            Assert.That(metadataParsed.Value, Is.TypeOf(typeof(ContractExamples.Execution.ContractMetadata.CallOption)));

            Assert.That((metadataParsed.Value as ContractExamples.Execution.ContractMetadata.CallOption).Item.numeraire, Is.EquivalentTo(Consensus.Tests.zhash));
			Assert.That((metadataParsed.Value as ContractExamples.Execution.ContractMetadata.CallOption).Item.controlAsset, Is.EquivalentTo(controlAsset));
            Assert.That((metadataParsed.Value as ContractExamples.Execution.ContractMetadata.CallOption).Item.controlAssetReturn, Is.EquivalentTo(controlAssetReturn));
			Assert.That((metadataParsed.Value as ContractExamples.Execution.ContractMetadata.CallOption).Item.oracle, Is.EquivalentTo(oracleAddress));
			Assert.That((metadataParsed.Value as ContractExamples.Execution.ContractMetadata.CallOption).Item.underlying, Is.EquivalentTo(underlying));
			Assert.That((metadataParsed.Value as ContractExamples.Execution.ContractMetadata.CallOption).Item.price, Is.EqualTo(price));
			Assert.That((metadataParsed.Value as ContractExamples.Execution.ContractMetadata.CallOption).Item.strike, Is.EqualTo(strike));
			Assert.That((metadataParsed.Value as ContractExamples.Execution.ContractMetadata.CallOption).Item.minimumCollateralRatio, Is.EqualTo(minimumCollateralRatio));
			Assert.That((metadataParsed.Value as ContractExamples.Execution.ContractMetadata.CallOption).Item.ownerPubKey, Is.EqualTo(ownerPubKey));

			object deserialized = null;

			try
			{
				deserialized = ContractExamples.Execution.deserialize(compiled.Value);
			}
			catch
			{

			}
			Assert.That(deserialized, Is.Not.Null);
		}
	}
}
