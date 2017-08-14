using System;
using System.Threading.Tasks;
using Gtk;
using QRCoder;
using Wallet.core.Data;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class WalletReceiveLayout : WidgetBase, IControlInit
	{
        string Address;
		byte[] _QrCodeImage;
		Gdk.Rectangle _ImageAllocation;

		public WalletReceiveLayout()
		{
			this.Build();

            labelAddress.ModifyFg(Gtk.StateType.Normal, Constants.Colors.TextBlue.Gdk);
            labelAddress.ModifyFont(Constants.Fonts.ActionBarSmall);

			Gtk.Clipboard clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
            eventboxBack.ButtonPressEvent += delegate
			{
				FindParent<WalletLayout>().SetPage(0);
			};

			_ImageAllocation = image1.Allocation;
			image1.ExposeEvent += Image1_ExposeEvent;
		}

		public void Init()
		{
            Address = FindParent<WalletLayout>().Address;
            labelAddress.Text = Address;

			var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(Address, QRCodeGenerator.ECCLevel.Q);
			var qrCode = new BitmapByteQRCode(qrCodeData);

			_QrCodeImage = qrCode.GetGraphic(20);
		}

		void Image1_ExposeEvent(object o, ExposeEventArgs args)
		{
			var image = (Image)o;

			if (_ImageAllocation != image.Allocation)
			{
				SetImage((Image)o);
			}
		}

		async Task SetImage(Image image)
		{
			var dim = Math.Min(image.Allocation.Width, image.Allocation.Height);
            Gdk.Pixbuf pixbuf = null;

			await Task.Run(() =>
			{
				pixbuf = new Gdk.Pixbuf(_QrCodeImage).ScaleSimple(dim, dim, Gdk.InterpType.Hyper);
			});

			Application.Invoke(delegate { image.Pixbuf = pixbuf; });
			_ImageAllocation = image.Allocation;
		}
	}
}
