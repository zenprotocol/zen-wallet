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

			SelectedKey = App.Instance.Wallet.KeyStore.GetKey (false);

			foreach (var key_ in App.Instance.Wallet.KeyStore.List())
			{
				Console.WriteLine(key_.Public);
			}

			buttonClose.Clicked += delegate { 
				CloseDialog(); 
			};

			buttonKeys.Clicked += delegate { 
				new KeysDialog(key => {
					SelectedKey = key;
				}).ShowDialog(MainAreaController.GetInstance().MainView as Gtk.Window);
			};
		}

		private Key _selectedKey;
		private Key SelectedKey {
			get { 
				return _selectedKey;
			}
			set { 
				_selectedKey = value;
				dialogfieldAddress.Value = value == null ? null : BitConverter.ToString(value.Public).Substring(0, 15);
			}
		}
	}
}
