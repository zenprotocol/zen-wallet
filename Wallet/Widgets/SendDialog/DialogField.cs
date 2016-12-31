using System;
using Wallet.Constants;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class DialogField : Gtk.Bin
	{
		public DialogField ()
		{
			this.Build ();

			label.ModifyFont (Fonts.DialogContentBold);

	//		entry.ModifyFont (Fonts.DialogContent);
	//		entry.ModifyBase(Gtk.StateType.Normal, Colors.Textbox.Gdk);
		}

		public bool IsEditable {
			set {
				entry.IsEditable = value;
				entry.CanFocus = value;
			}
		}

		public String Value { 
			get { 
				return entry.Text;
			} 
			set { 
				entry.Text = value;
			} 
		}

		public String Caption { 
			set { 
				label.Text = value;
			} 
		}
	}
}

