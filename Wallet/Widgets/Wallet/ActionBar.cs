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

			label1.ModifyFg(Gtk.StateType.Normal, Constants.Colors.Text2);
			label3.ModifyFg(Gtk.StateType.Normal, Constants.Colors.Text2);
			label4.ModifyFg(Gtk.StateType.Normal, Constants.Colors.Text2);
			label5.ModifyFg(Gtk.StateType.Normal, Constants.Colors.Text2);

			label1.ModifyFont (Constants.Fonts.ActionBarBig);
			label3.ModifyFont (Constants.Fonts.ActionBarSmall);
			label4.ModifyFont (Constants.Fonts.ActionBarBig);
			label5.ModifyFont (Constants.Fonts.ActionBarSmall);

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
					
				try {
					image2.Pixbuf = Gdk.Pixbuf.LoadFromResource(Constants.Images.CurrencyLogo(_currency));
				} catch (Exception e) {
					Console.WriteLine("missing" + Constants.Images.CurrencyLogo(_currency));
				}
			}
		}

		protected void Send (object sender, EventArgs e)
		{
			WalletController.Send (400);
		}
	}
}

