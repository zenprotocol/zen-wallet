using System;
using Gtk;
using Cairo;
using Wallet.Constants;

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

			labelHeader.ModifyFont (Fonts.DialogHeader);

			Apply((Label label) => {
				label.ModifyFont (Fonts.DialogContent);
			}, labelTo, labelAmount, labelBalance, labelFee);

			Apply((Entry entry) => {
				entry.ModifyFont (Fonts.DialogContentBold);
				entry.ModifyBase(Gtk.StateType.Normal, Colors.ButtonSelected.Gdk);
			}, entryTo, entryAmount);
		
			imageCurrency.Pixbuf = Utils.ToPixbuf(Images.AssetLogo(asset.Image));

			ButtonReleaseEvent (eventboxSend, () => {
				WalletController.GetInstance().SendStub.RequestSend("1",1,"1",1).ContinueWith(Action => {
					notebookCurrent.Page++;
				});
			});
				
			ButtonReleaseEvent (eventboxBack, () => {
				notebookCurrent.Page--;
			});

			ButtonReleaseEvent (eventboxCancel, () => {
				CloseDialog();
			});
		}
			
		public void Confirm() {
		//	WalletController.GetInstance ().SendStub.RequestSend("assetId", 10, "desc", 10);
			//..
			CloseDialog();
		}
	}
}