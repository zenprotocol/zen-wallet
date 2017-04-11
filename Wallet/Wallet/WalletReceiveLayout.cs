using System;
using Gtk;
using QRCoder;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class WalletReceiveLayout : WidgetBase
	{
		public WalletReceiveLayout()
		{
			this.Build();

			entryAddress.ModifyFg(Gtk.StateType.Normal, Constants.Colors.Text2.Gdk);
			entryAddress.ModifyFont(Constants.Fonts.ActionBarBig);

			var key = App.Instance.Wallet.GetUnusedKey().Address.ToString();

			Gtk.Clipboard clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
			buttonCopy.Clicked += delegate
			{
				clipboard.Text = key;
			};

			entryAddress.Text = key;

			entryAddress.SelectRegion(0, -1);

			entryAddress.FocusGrabbed += (sender, e) =>
			{
				new System.Threading.Thread(() =>
				{
					Gtk.Application.Invoke(delegate
					{
						System.Threading.Thread.Sleep(150);
						entryAddress.SelectRegion(0, -1);
						System.Threading.Thread.Sleep(100);
						entryAddress.SelectRegion(0, -1);
					});
				}).Start();
			};

			buttonBack.Clicked += delegate {
				FindParent<Gtk.Notebook>().Page = 0;
			};

			var qrGenerator = new QRCodeGenerator();
			var qrCodeData = qrGenerator.CreateQrCode(key, QRCodeGenerator.ECCLevel.Q);
			var qrCode = new BitmapByteQRCode(qrCodeData);
			var qrCodeImage = qrCode.GetGraphic(20);

			var lastAllocation = image1.Allocation;

			image1.ExposeEvent += (sender, args) => {
				var image = sender as Image;

				if (lastAllocation != image.Allocation)
				{
					var dim = Math.Min(image.Allocation.Width, image.Allocation.Height);
					image.Pixbuf = new Gdk.Pixbuf(qrCodeImage).ScaleSimple(dim, dim, Gdk.InterpType.Hyper);

					lastAllocation = image.Allocation;
				}
				Console.WriteLine();
			};
		}
	}
}
