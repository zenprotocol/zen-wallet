using System;
using Gtk;
using Wallet.core;

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
		private WalletController WalletController = WalletController.Instance ;

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
				new SendDialog(WalletController.Instance.AssetType).ShowDialog();
			});

			ButtonPressEvent(eventboxReceive, () =>
			{
				new ReceiveDialog().ShowDialog();
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
				labelAmount.Text = value.ToString();
			}
		}

		public AssetType Asset {
			set {
				if (value == null)
					return;
				
				labelCurrency.Text = value.Caption;
				if (value.Image != null) {
					try
					{
						image2.Pixbuf = new Gdk.Pixbuf(value.Image);
						image2.Pixbuf = image2.Pixbuf.ScaleSimple(64, 64, Gdk.InterpType.Hyper);
					}
					catch
					{
					}
				}
				else
					image2.Pixbuf = null;
			}
		}
	}
}

