using System;
using Wallet.core.Data;

namespace ContractsDiscovery.Web.App_Data
{
	public class CreateCallOption
    {
		public ContractAddressField Numeraire { get; set; }
		public ContractAddressField ControlAsset { get; set; }
		public ContractAddressField Oracle { get; set; }
		public string OracleErrorMessage { get; set; }
		public PKAddressField ControlAssetReturn { get; set; }
		public Field<string> Underlying { get; set; }
		public DecimalField Price { get; set; }
		public DecimalField Strike { get; set; }
		public DecimalField MinimumCollateralRatio { get; set; }
		public PublicKeyField OwnerPubKey { get; set; }

        public CreateCallOption()
        {
            Numeraire = new ContractAddressField();
            ControlAsset = new ContractAddressField();
            Oracle = new ContractAddressField();
            ControlAssetReturn = new PKAddressField();
            Underlying = new Field<string>();
            Price = new DecimalField();
            Strike = new DecimalField();
            MinimumCollateralRatio = new DecimalField();
            OwnerPubKey = new PublicKeyField();

			Numeraire.SetValue("cAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
			MinimumCollateralRatio.SetValue("1");
        }

        public bool Invalid 
        { 
            get
            {
                return
                    Numeraire.Invalid ||
                    ControlAsset.Invalid ||
                    Oracle.Invalid ||
                    ControlAssetReturn.Invalid ||
                    Underlying.Invalid ||
                    Price.Invalid ||
                    Strike.Invalid ||
                    MinimumCollateralRatio.Invalid ||
                    OwnerPubKey.Invalid;
            }
        }
	}
}
