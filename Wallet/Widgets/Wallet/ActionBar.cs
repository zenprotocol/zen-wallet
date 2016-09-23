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

		
			Gdk.Color fontcolor = new Gdk.Color(0x0F7, 0x0F7, 0x0F7);

			label1.ModifyFg(Gtk.StateType.Normal, fontcolor);
			label3.ModifyFg(Gtk.StateType.Normal, fontcolor);
			label4.ModifyFg(Gtk.StateType.Normal, fontcolor);
			label5.ModifyFg(Gtk.StateType.Normal, fontcolor);

			label1.ModifyFont (Pango.FontDescription.FromString ("Aharoni CLM Bold 25"));
			label3.ModifyFont (Pango.FontDescription.FromString ("Aharoni CLM Bold 15"));
			label4.ModifyFont (Pango.FontDescription.FromString ("Aharoni CLM Bold 25"));
			label5.ModifyFont (Pango.FontDescription.FromString ("Aharoni CLM Bold 15"));


			HeightRequest = 130;
		}

		public Decimal Rate {
			set {
				label1.Text = value.ToString();
				label3.Text = _currency;
			}
		}

		public Decimal Total {
			set {
				label4.Text = value.ToString();
				label5.Text = "USD";
			}
		}

		private String _currency;

		public String Currency {
			set {

				switch (value) {
				case "Bitcoin":
					_currency = "BTC";
					break;
				case "Ether":
					_currency = "ETH";
					break;
				case "Zen":
					_currency = "ZEN";
					break;
				case "Lite":
					_currency = "LTC";
					break;
				}

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

