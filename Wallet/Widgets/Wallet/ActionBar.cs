using System;

namespace Wallet
{
	public interface ActionBarView {
		Decimal Total { set; }
		Decimal Rate { set; }
		String Currency { set; }
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class ActionBar : Gtk.Bin, ActionBarView
	{
		private WalletController WalletController = WalletController.GetInstance ();

		public ActionBar ()
		{
			this.Build ();
			WalletController.ActionBarView = this;

			Pango.FontDescription fontdesc = Pango.FontDescription.FromString("Purisa 10");
			label1.ModifyFont(fontdesc);

//			label1.ModifyFont (Gtk.StateType.Normal, new Gdk.Color (0x0F7, 0x0F7, 0x0F7));
			label2.ModifyText (Gtk.StateType.Normal, new Gdk.Color (0x0F7, 0x0F7, 0x0F7));

			HeightRequest = 130;
		}

		public Decimal Rate {
			set {
				label1.Text = value.ToString();
			}
		}

		public Decimal Total {
			set {
				label2.Text = value.ToString();
			}
		}

		public String Currency {
			set {
				String asset = "Wallet.Assets.misc." + value + ".png";
				try {
					image2.Pixbuf = Gdk.Pixbuf.LoadFromResource(asset);
				} catch (Exception e) {
					Console.WriteLine("missing" + asset);
				}
			}
		}

		protected void Send (object sender, EventArgs e)
		{
			WalletController.Send (400);
		}
	}
}

