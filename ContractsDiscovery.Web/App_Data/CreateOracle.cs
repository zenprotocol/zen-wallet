using System;
using Wallet.core.Data;

namespace ContractsDiscovery.Web.App_Data
{
	public class CreateOracle
	{
		public ContractAddressField OwnerPubKey { get; set; }

		public CreateOracle()
		{
			OwnerPubKey = new ContractAddressField();
		}

		public bool Invalid
		{
			get
			{
				return
					OwnerPubKey.Invalid;
			}
		}
	}
}
