using System;

namespace Wallet
{
	public interface ISendDialogStep1 {
		
	}
	[System.ComponentModel.ToolboxItem (true)]
	public partial class SendDialogStep1 : WidgetBase
	{
		public decimal Amount { get { return  Utils.ToDecimal(dialogfieldAmount.Value); } }
		public string To { get { return dialogfieldTo.Value; } }

		public SendDialogStep1 ()
		{
			this.Build ();

			dialogfieldAmount.Caption = "AMOUNT";
			dialogfieldTo.Caption = "TO";

			eventboxSend.ButtonReleaseEvent += (object o, Gtk.ButtonReleaseEventArgs args) => {
				FindParent<SendDialog>().Next();
			};
		}
	}
}

