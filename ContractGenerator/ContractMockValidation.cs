using System;
using System.Threading.Tasks;

namespace BlockChain
{
	public class ContractGenerationData
	{
		public byte[] Hints { get; set; }
		public ulong KalapasPerBlock { get; set; }
		public ulong ActivationCost { get; set; }
	}

	public static class ContractMockValidation
	{
		public static async Task<ContractGenerationData> GenerateHints(byte[] fsCode)
		{
			new Task(() =>
			{
				var contractGenerationData = new ContractGenerationData()
				{
					Hints = new byte() { 0x00, 0x01, 0x02 },
					KalapasPerBlock = 1000,
					ActivationCost = 100
				}

				yield return contractGenerationData;

			}).Start();
		}
	}
}
