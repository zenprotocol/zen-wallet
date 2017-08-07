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

			entryAddress.ModifyFg(Gtk.StateType.Normal, Constants.Colors.Text2.Gdk);
			entryAddress.ModifyFont(Constants.Fonts.ActionBarBig);

			Gtk.Clipboard clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
			buttonCopy.Clicked += delegate
			{
                clipboard.Text = Address;
			};

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

			buttonBack.Clicked += delegate
			{
				FindParent<WalletLayout>().SetPage(0);
			};

			_ImageAllocation = image1.Allocation;
			image1.ExposeEvent += Image1_ExposeEvent;
		}

		public void Init()
		{
            Address = FindParent<WalletLayout>().Address;
            entryAddress.Text = Address;

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

		async void SetImage(Image image)
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
