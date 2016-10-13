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

			ButtonReleaseEvent(eventbox1, () => {
				CloseDialog();
			//	sendDialogView.Close();
			});
		}
	}
}

