using System;

namespace Wallet
{
	public interface IListener
	{
		void UpdateUI(DataModel dataModel);
	}
}

