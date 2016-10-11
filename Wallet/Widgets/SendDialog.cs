using System;
using Gtk;
using Cairo;

namespace Wallet
{
	public interface ISendDialogView {
		decimal Amount { get; }
		string To { get; }
		void Cofirm();
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class SendDialog : DialogBase, ISendDialogView
	{
		public decimal Amount { get { return Utils.ToDecimal(entryAmount.Text); } }
		public string To { get { return entryTo.Text; } }

		public SendDialog (CurrencyEnum currency)
		{
			this.Build();

			labelHeader.ModifyFont (Constants.Fonts.DialogHeader);

			foreach (Label label in new Label[] { labelTo, labelAmount, labelBalance, labelFee}) {
				label.ModifyFont (Constants.Fonts.DialogContent);
			}

			try {
				image.Pixbuf = Gdk.Pixbuf.LoadFromResource(Constants.Images.CurrencyLogo(currency));
			} catch {
				Console.WriteLine("missing" + Constants.Images.CurrencyLogo(currency));
			}

			eventboxSend.ButtonReleaseEvent += (object o, ButtonReleaseEventArgs args) => 
			{
				new SendConfirmationDialog(this).ShowDialog(Program.MainWindow);
			};
		}
			
		public void Cofirm() {
			//..
			CloseDialog();
		}
	}
}