using System.IO;
using Wallet.core.Data;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ReceiveDialog : DialogBase
	{
		public ReceiveDialog()
		{
			this.Build();

			entry1.Text = App.Instance.Wallet.GetUnusedKey().Address.ToString();

			Gtk.Clipboard clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));

			buttonCopy.Clicked += delegate {
				clipboard.Text = entry1.Text;
			};

			buttonClose.Clicked += delegate { 
				CloseDialog(); 
			};
		}
	}
}
