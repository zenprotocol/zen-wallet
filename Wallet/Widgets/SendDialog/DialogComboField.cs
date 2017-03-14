using System;
using Wallet.Constants;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class DialogComboField : Gtk.Bin
	{
		public DialogComboField()
		{
			this.Build();

			label.ModifyFont(Fonts.DialogContentBold);
		}

		public Gtk.ComboBox ComboBox
		{
			get
			{
				return combobox;
			}
		}

		public String Caption
		{
			set
			{
				label.Text = value;
			}
		}
	}
}
