using System;
using System.Threading.Tasks;
using Infrastructure;

namespace ContractGenerator
{
	public interface IContractGenerator
	{
		Task<ContractGenerationData> Generate(byte[] fsCode);
	}

	public class ContractGenerationData
	{
		public byte[] Hints { get; set; }
		public ulong KalapasPerBlock { get; set; }
		public ulong ActivationCost { get; set; }
	}

	public class ContractMockValidationMock : Singleton<ContractMockValidationMock>, IContractGenerator
	{
		public async Task<ContractGenerationData> Generate(byte[] fsCode)
		{
			await Task.Delay(1500);

			var contractGenerationData = new ContractGenerationData()
			{
				Hints = new byte[] { 0x00, 0x01, 0x02 },
				KalapasPerBlock = 1000,
				ActivationCost = 100
			};

			return contractGenerationData;
		}
	}
}
