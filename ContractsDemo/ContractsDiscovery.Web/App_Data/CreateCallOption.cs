using System;
using Wallet.core.Data;

namespace ContractsDiscovery.Web.App_Data
{
    public class Field<T>
    {
        public T Value { get; set; }
        public bool Invalid { get; set; }

        public Field()
        {
            Value = default(T);
            Invalid = false;
        }

        public virtual void SetValue(T value)
        {
            Value = value;
        }
    }

    public class ContractAddressField : Field<string>
    {
        public Address Address { get; set; }

        public override void SetValue(string value)
        {
            base.SetValue(value);

            try
			{
				Address = new Address(Convert.FromBase64String(value), Wallet.core.Data.AddressType.Contract);
			}
			catch
			{
                Invalid = true;
            }
        }
    }

	public class DecimalField : Field<string>
	{
		public Decimal Decimal { get; set; }

		public override void SetValue(string value)
		{
			Value = value;

			try
			{
                Decimal = decimal.Parse(value);
			}
			catch
			{
				Invalid = true;
			}
		}
	}

	public class CreateCallOption
    {
		public ContractAddressField Numeraire { get; set; }
		public ContractAddressField ControlAsset { get; set; }
		public ContractAddressField Oracle { get; set; }
		public ContractAddressField ControlAssetReturn { get; set; }
		public Field<string> Underlying { get; set; }
		public DecimalField Price { get; set; }
		public DecimalField MinimumCollateralRatio { get; set; }
		public ContractAddressField OwnerPubKey { get; set; }

        public CreateCallOption()
        {
            Numeraire = new ContractAddressField();
            ControlAsset = new ContractAddressField();
            Oracle = new ContractAddressField();
            ControlAssetReturn = new ContractAddressField();
            Underlying = new Field<string>();
            Price = new DecimalField();
            MinimumCollateralRatio = new DecimalField();
            OwnerPubKey = new ContractAddressField();
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
                    MinimumCollateralRatio.Invalid ||
                    OwnerPubKey.Invalid;
            }
        }
	}
}
