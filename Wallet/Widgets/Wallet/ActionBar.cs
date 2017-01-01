using System;
using Gtk;

namespace Wallet
{
	public interface ActionBarView {
		Decimal Total { set; }
		Decimal Rate { set; }
		AssetType Asset { set; }
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class ActionBar : WidgetBase, ActionBarView
	{
		private WalletController WalletController = WalletController.GetInstance ();

		public ActionBar ()
		{
			this.Build ();
			WalletController.ActionBarView = this;

			Apply ((Label label) => {
				label.ModifyFg(Gtk.StateType.Normal, Constants.Colors.Text2.Gdk);
				label.ModifyFont (Constants.Fonts.ActionBarSmall);
			}, labelCurrency, labelCurrencyConverted);

			Apply ((Label label) => {
				label.ModifyFg(Gtk.StateType.Normal, Constants.Colors.Text2.Gdk);
				label.ModifyFont (Constants.Fonts.ActionBarBig);
			}, labelAmount, labelAmountConverted);

			ButtonPressEvent (eventboxSend, () => {
//				new SendDialog(WalletController.GetInstance().Asset).ShowDialog(Program.temp); //TODO: fuck this.
			});

			ButtonPressEvent(eventboxReceive, () =>
			{
//				new ReceiveDialog().ShowDialog(Program.temp); //TODO: fuck this.
			});


			HeightRequest = 130;
		}

		public Decimal Rate {
			set {
				labelAmount.Text = value.ToString ();
			}
		}

		public Decimal Total {
			set {
				labelAmountConverted.Text = value.ToString();
				labelCurrencyConverted.Text = "USD";
			}
		}

		public AssetType Asset {
			set {
				labelCurrency.Text = value.Caption;
				image2.Pixbuf = Utils.ToPixbuf(Constants.Images.AssetLogo(value.Image));
			}
		}
	}
}

