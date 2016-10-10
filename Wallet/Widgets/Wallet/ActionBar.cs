using System;
using Gtk;

namespace Wallet
{
	public interface ActionBarView {
		Decimal Total { set; }
		Decimal Rate { set; }
		CurrencyEnum Currency { set; }
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class ActionBar : Gtk.Bin, ActionBarView
	{
		private WalletController WalletController = WalletController.GetInstance ();

		public ActionBar ()
		{
			this.Build ();
			WalletController.ActionBarView = this;

			foreach (Label label in new Label[] { label1, label3,  label4, label5 }) {
				label.ModifyFg(Gtk.StateType.Normal, Constants.Colors.Text2.Gdk);
				label.ModifyFont (Constants.Fonts.ActionBarBig);
			}

			imagebutton1.ButtonPressEvent += (object o, Gtk.ButtonPressEventArgs args) => {
				new SendDialog(CurrencyEnum.Zen).ShowDialog(Program.MainWindow);
			};

			HeightRequest = 130;
		}

		public Decimal Rate {
			set {
				label1.Text = value.ToString ();
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

		public CurrencyEnum Currency {
			set {
				switch (value) {
				case CurrencyEnum.Bitcoin:
					_currency = "BTC";
					break;
				case CurrencyEnum.Ether:
					_currency = "ETH";
					break;
				case CurrencyEnum.Zen:
					_currency = "ZEN";
					break;
				case CurrencyEnum.Lite:
					_currency = "LTC";
					break;
				}
					
				try {
					image2.Pixbuf = Gdk.Pixbuf.LoadFromResource(Constants.Images.CurrencyLogo(value));
				} catch {
					Console.WriteLine("missing" + Constants.Images.CurrencyLogo(value));
				}
			}
		}

		protected void Send (object sender, EventArgs e)
		{
			WalletController.Send (400);
		}
	}
}

