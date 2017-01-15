using System;
using Gtk;
using Cairo;
using Wallet.Constants;
using Wallet.core;

namespace Wallet
{
	public interface ISendDialogView {
		decimal Amount { get; }
		string To { get; }
		void Confirm();
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class SendDialog : DialogBase//, ISendDialogView
	{
//		public decimal Amount { get { return Utils.ToDecimal(entryAmount.Text); } }
//		public string To { get { return entryTo.Text; } }

		private const int WAITING_PAGE = 0;
		private const int PAGE_1 = 1;
		private const int PAGE_2 = 2;

		private void SetContent(Widget widget) {
			foreach (Widget child in hboxContent.AllChildren) {
				hboxContent.Remove (child);
			}

			hboxContent.PackStart (widget, true, true, 0);

//			if (widget != senddialogwaiting) {
				Resize ();
	//		} else {
			//}
		}

		public void Next() {
			try
			{
				if (!App.Instance.Wallet.Spend(senddialogstep1.To, AssetsHelper.AssetCodes["zen"], (ulong)senddialogstep1.Amount))
				{
					new MessageBox("error during send").ShowDialog();
				}
				CloseDialog();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		public void Back() {
			SetContent(senddialogstep1);
		}

		public SendDialog (AssetType asset)
		{
			this.Build();

			SetContent (senddialogstep1);

			CloseControl = eventboxCancel;

			labelHeader.ModifyFont (Fonts.DialogHeader);
//
//			Apply((Label label) => {
//				label.ModifyFont (Fonts.DialogContent);
//			}, labelTo, labelAmount, labelBalance, labelFee);
//
//			Apply((Entry entry) => {
//				entry.ModifyFont (Fonts.DialogContentBold);
//				entry.ModifyBase(Gtk.StateType.Normal, Colors.Textbox.Gdk);
//			}, entryTo, entryAmount);
		
			imageCurrency.Pixbuf = Utils.ToPixbuf(Images.AssetLogo(asset.Image));

		}
			
		public void Confirm() {
		//	WalletController.GetInstance ().SendStub.RequestSend("assetId", 10, "desc", 10);
			//..
			CloseDialog();
		}
	}
}