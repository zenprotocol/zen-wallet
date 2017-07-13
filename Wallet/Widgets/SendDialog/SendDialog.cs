using System;
using Gtk;
using Cairo;
using Wallet.Constants;
using Wallet.core;
using Consensus;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class SendDialog : DialogBase
	{
		//private const int WAITING_PAGE = 0;
		//private const int PAGE_1 = 1;
		//private const int PAGE_2 = 2;

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

		internal void Next(Types.Transaction tx)
		{
			SetContent(senddialogstep2);
			senddialogstep2.Tx = tx; //TODO
		}

		public void Back()
		{
			SetContent(senddialogstep1);
		}

		public SendDialog (byte[] asset)
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
		
 
            //App.Instance.Wallet.AssetsMetadata.
			//imageCurrency.Pixbuf = Utils.ToPixbuf(Images.AssetLogo(asset.Image));
		}
			
		public void Close() {
		//	WalletController.GetInstance ().SendStub.RequestSend("assetId", 10, "desc", 10);
			//..
			CloseDialog();
		}

		public void Sign()
		{
			throw new NotImplementedException();
		}
	}
}