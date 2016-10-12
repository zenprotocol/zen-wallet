using System;
using Gtk;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class SendConfirmationDialog : DialogBase
	{
		public SendConfirmationDialog (ISendDialogView sendDialogView)
		{
			this.Build ();

			eventbox1.ButtonReleaseEvent += (object o, ButtonReleaseEventArgs args) => 
			{
				CloseDialog();
				sendDialogView.Confirm();
			};
		}
	}
}

