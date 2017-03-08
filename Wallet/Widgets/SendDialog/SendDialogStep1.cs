using System;
using Consensus;
using Wallet.core.Data;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class SendDialogStep1 : WidgetBase
	{
		public SendDialogStep1()
		{
			this.Build();

			dialogfieldAmount.Caption = "AMOUNT";
			dialogfieldTo.Caption = "TO";

			eventboxSend.ButtonReleaseEvent += (object o, Gtk.ButtonReleaseEventArgs args) =>
			{
				ulong amount;
				byte[] address;

				try
				{
					amount = ulong.Parse(dialogfieldAmount.Value);
				}
				catch (Exception e)
				{
					new MessageBox("Invalid amount").ShowDialog();
					return;
				}

				try
				{
					address = Key.FromBase64String(dialogfieldTo.Value);
				}
				catch (Exception e)
				{
					new MessageBox("Invalid address").ShowDialog();
					return;
				}

				Types.Transaction tx;

				if (App.Instance.Wallet.Sign(
					address,
					Consensus.Tests.zhash,
					amount,
					out tx
				))
				{
					FindParent<SendDialog>().Next(tx);
				}
				else
				{
					new MessageBox("Could not satisfy amount for asset").ShowDialog();
				}
			};

			buttonRaw.Clicked += delegate
			{
				new SendRaw().ShowDialog();
			};
		}
	}
}

