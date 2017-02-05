using System;
using System.Threading.Tasks;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class SendDialogStep2 : WidgetBase
	{
		public SendDialogStep2 ()
		{
			this.Build ();

			dialogfieldAmount.Caption = "AMOUNT";
			dialogfieldTo.Caption = "TO";

			dialogfieldAmount.IsEditable = false;
			dialogfieldTo.IsEditable = false;

			eventboxBack.ButtonReleaseEvent += (object o, Gtk.ButtonReleaseEventArgs args) => {
				FindParent<SendDialog>().Back();
			};

			eventboxSend.ButtonReleaseEvent += (object o, Gtk.ButtonReleaseEventArgs args) =>
			{
				FindParent<SendDialog>().Send();
			};

			expander.Activated += (object sender, EventArgs e) => {
				System.Threading.Thread.Sleep(100);
				FindParent<SendDialog>().Resize();
			};
		}

		public bool Waiting { 
			set {
				if (value) {
					senddialogwaiting.Show ();
				} else {
					senddialogwaiting.Hide ();
					FindParent<SendDialog>().Resize();
				}
			}
		}
	}
}