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

			//dialogfieldAddress.Caption = "Public Key:";

			var key = Create();

		//	dialogfieldAddress.Value = BitConverter.ToString(key.Public);

			foreach (var key_ in App.Instance.Wallet.KeyStore.List())
			{
				Console.WriteLine(key_.Public);
			}

			buttonClose.Clicked += delegate { 
				CloseDialog(); 
			};

			buttonKeys.Clicked += delegate { 
				new KeysDialog().ShowDialog(MainAreaController.GetInstance().MainView as Gtk.Window);
			};
		}

		public Key Create()
		{
			Byte[] sendToBytes = new Byte[32];
			new Random().NextBytes(sendToBytes);

			var key = new core.Data.Key();

			key.Public = sendToBytes;

		//	App.Instance.Wallet.AddKey(key);

			return key;
		}
	}
}
