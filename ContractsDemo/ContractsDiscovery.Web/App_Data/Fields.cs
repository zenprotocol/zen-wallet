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

	public class PKAddressField : Field<string>
	{
		public Address Address { get; set; }

		public override void SetValue(string value)
		{
			base.SetValue(value);

			try
			{
				Address = new Address(value);
				Invalid = Address.AddressType != AddressType.PK;
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
}
