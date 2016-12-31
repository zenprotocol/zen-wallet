using System;
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

			var key = Create();

			dialogfieldAddress.Value = BitConverter.ToString(key.Public);

			foreach (var key_ in core.Wallet.Instance.GetKeys())
			{
				Console.WriteLine(key_.Public);
			}

			buttonClose.Clicked += (sender, e) => { CloseDialog(); };
		}

		public Key Create()
		{
			Byte[] sendToBytes = new Byte[32];
			new Random().NextBytes(sendToBytes);

			var key = new core.Data.Key();

			key.Public = sendToBytes;

			core.Wallet.Instance.AddKey(key);

			return key;
		}
	}
}
