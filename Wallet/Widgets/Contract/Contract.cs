using System;

namespace Wallet
{
	public interface ContractView {
		String ContractCodeHash { set; }
		String ContractCodeContent { set; }
		String ContractCodeAssertion { set; }
		String Proof { set; }
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class Contract : Gtk.Bin, ContractView
	{
		ContractController ContractController = ContractController.GetInstance();
		public Contract ()
		{
			this.Build ();
			ContractController.ContractView = this;

			textview1.ModifyBase (Gtk.StateType.Normal, new Gdk.Color (0x01d, 0x025, 0x030));
			textview2.ModifyBase (Gtk.StateType.Normal, new Gdk.Color (0x01d, 0x025, 0x030));
			textview3.ModifyBase (Gtk.StateType.Normal, new Gdk.Color (0x01d, 0x025, 0x030));

			textview1.ModifyText (Gtk.StateType.Normal, new Gdk.Color (0x0F7, 0x0F7, 0x0F7));
			textview2.ModifyText (Gtk.StateType.Normal, new Gdk.Color (0x0F7, 0x0F7, 0x0F7));
			textview3.ModifyText (Gtk.StateType.Normal, new Gdk.Color (0x0F7, 0x0F7, 0x0F7));

			eventboxCreate.ButtonPressEvent += delegate {
				ContractController.Create();
			};

			eventboxVerify.ButtonPressEvent += delegate {
				ContractController.Verify();
			};

			eventboxLoad.ButtonPressEvent += delegate {
				ContractController.Load();
			};
		}

		public String ContractCodeHash { 
			set {
				entry2.Text = value;
			} 
		}

		public String ContractCodeContent { 
			set {
				textview2.Buffer.Text = value;
			} 
		}

		public String ContractCodeAssertion { 
			set {
				textview1.Buffer.Text = value;
			} 
		}

		public String Proof { 
			set {
				textview3.Buffer.Text = value;
			} 
		}

	}
}

