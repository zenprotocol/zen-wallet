using System;
using Wallet.core;
using Wallet.core.Data;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ReceiveDialog : DialogBase
	{
		public ReceiveDialog()
		{
			this.Build();

			dialogfieldAddress.Caption = "Public Key:";

			SelectedKey = App.Instance.Wallet.GetUnusedKey ();

			//foreach (var key_ in App.Instance.Wallet.ListKeys())
			//{
			//	Console.WriteLine(key_.Address);
			//}

			buttonClose.Clicked += delegate { 
				CloseDialog(); 
			};

			buttonKeys.Clicked += delegate { 
				new KeysDialog(key => {
					SelectedKey = key;
				}).ShowDialog();
			};
		}

		private Key _selectedKey;
		private Key SelectedKey {
			get { 
				return _selectedKey;
			}
			set { 
				_selectedKey = value;
				dialogfieldAddress.Value = value == null ? null : value.AddressAsString;
			}
		}
	}
}
