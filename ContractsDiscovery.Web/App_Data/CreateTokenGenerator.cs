using System;
using Wallet.core.Data;

namespace ContractsDiscovery.Web.App_Data
{
	public class CreateTokenGenerator
	{
		public PKAddressField Destination { get; set; }

		public CreateTokenGenerator()
		{
			Destination = new PKAddressField();
		}

		public bool Invalid
		{
			get
			{
				return
					Destination.Invalid;
			}
		}
	}
}
