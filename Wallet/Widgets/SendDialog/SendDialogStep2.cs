using System;
using System.Threading.Tasks;
using Consensus;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class SendDialogStep2 : WidgetBase
	{
		public Types.Transaction Tx { get; set; }//TODO

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
				var result = App.Instance.Wallet.Transmit(Tx);

				if (result == BlockChain.BlockChain.TxResultEnum.Accepted)
				{
					FindParent<SendDialog>().Close();
				}
				else
				{
					new MessageBox("Rejected, reason: " + result).ShowDialog();
				}
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