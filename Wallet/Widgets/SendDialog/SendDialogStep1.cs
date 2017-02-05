using System;
using Wallet.core.Data;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class SendDialogStep1 : WidgetBase
	{
		public SendDialogStep1 ()
		{
			this.Build ();

			dialogfieldAmount.Caption = "AMOUNT";
			dialogfieldTo.Caption = "TO";

			eventboxSend.ButtonReleaseEvent += (object o, Gtk.ButtonReleaseEventArgs args) => {
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

				var tx = App.Instance.Wallet.Sign(
						address, 
						Consensus.Tests.zhash, 
						amount);

				if (tx != null)
				{
					FindParent<SendDialog>().Next(tx);
				}
				else
				{
					new MessageBox("Could not satisfy amount for asset").ShowDialog();
				}
			};
		}
	}
}

