using System;
using Wallet.core.Data;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class AddKeyDialog : DialogBase
	{
		public AddKeyDialog(Action done)
		{
			this.Build();

			dialogfield1.Caption = "Public Key:";

			buttonCreate.Clicked += delegate
			{
				if (App.Instance.Wallet.AddKey(dialogfield1.Value))
				{
					done();
					CloseDialog();
				}
				else
				{
					new MessageBox("already exists").ShowDialog();
				}
			};
		}
	}
}
