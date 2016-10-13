using System;
using Gtk;
using Cairo;

namespace Wallet
{
	public interface ISendDialogView {
		decimal Amount { get; }
		string To { get; }
		void Confirm();
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class SendDialog : DialogBase, ISendDialogView
	{
		public decimal Amount { get { return Utils.ToDecimal(entryAmount.Text); } }
		public string To { get { return entryTo.Text; } }

		public SendDialog (AssetType asset)
		{
			this.Build();

			labelHeader.ModifyFont (Constants.Fonts.DialogHeader);

			Apply((Label label) => {
				label.ModifyFont (Constants.Fonts.DialogContent);
			}, labelTo, labelAmount, labelBalance, labelFee);

			Apply((Entry entry) => {
				entry.ModifyFont (Constants.Fonts.DialogContentBold);
			}, entryTo, entryAmount);
		
			image.Pixbuf = Utils.ToPixbuf(Constants.Images.AssetLogo(asset.Image));

			eventboxSend.ButtonReleaseEvent += (object o, ButtonReleaseEventArgs args) => 
			{
				CloseDialog();
				//new SendConfirmationDialog(this).ShowDialog(Program.MainWindow);
			};
		}
			
		public void Confirm() {
		//	WalletController.GetInstance ().SendStub.RequestSend("assetId", 10, "desc", 10);
			//..
			CloseDialog();
		}
	}
}