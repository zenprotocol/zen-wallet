using System;
using Gtk;
using Cairo;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class SendDialog : DialogBase
	{
		public SendDialog (CurrencyEnum currency)
		{
			this.Build ();

			imageSend.Pixbuf = Gdk.Pixbuf.LoadFromResource(Constants.Images.SendDialog);

			try {
				image.Pixbuf = Gdk.Pixbuf.LoadFromResource(Constants.Images.CurrencyLogo(currency));
			} catch {
				Console.WriteLine("missing" + Constants.Images.CurrencyLogo(currency));
			}

			eventboxSend.ButtonReleaseEvent += (object o, ButtonReleaseEventArgs args) => 
			{
				CloseDialog();
			};
		}
	}
}