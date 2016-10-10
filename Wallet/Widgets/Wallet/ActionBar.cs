using System;
using Gtk;

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

			label1.ModifyFg(Gtk.StateType.Normal, Constants.Colors.Text2.Gdk);
			label3.ModifyFg(Gtk.StateType.Normal, Constants.Colors.Text2.Gdk);
			label4.ModifyFg(Gtk.StateType.Normal, Constants.Colors.Text2.Gdk);
			label5.ModifyFg(Gtk.StateType.Normal, Constants.Colors.Text2.Gdk);

			label1.ModifyFont (Constants.Fonts.ActionBarBig);
			label3.ModifyFont (Constants.Fonts.ActionBarSmall);
			label4.ModifyFont (Constants.Fonts.ActionBarBig);
			label5.ModifyFont (Constants.Fonts.ActionBarSmall);
//
			imagebutton1.ButtonPressEvent += (object o, Gtk.ButtonPressEventArgs args) => {
				Dialog dialog = new Dialog();
		//		("Sample", Program.MainWindow, Gtk.DialogFlags.DestroyWithParent);
							dialog.Modal = true;
							dialog.Decorated = false;
				//			dialog.AddButton ("Close", ResponseType.Close);
				dialog.Add(new ActionBar());
						//	dialog.Response += new ResponseHandler (on_dialog_response);
							dialog.Run ();
							dialog.Destroy ();
			};
//			imageSend.ButtonReleaseEvent +=	(object o, Gtk.ButtonReleaseEventArgs args) => {
//				SendDialog dialog = new SendDialog();
////					("Sample", Program.MainWindow, Gtk.DialogFlags.Modal);
//				dialog.Modal = true;
////				dialog.AddButton ("Close", ResponseType.Close);
//			//	dialog.Response += new ResponseHandler (on_dialog_response);
//				dialog.Run ();
//				dialog.Destroy ();
//			};

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
					image2.Pixbuf = Gdk.Pixbuf.LoadFromResource(Constants.Images.CurrencyLogo(value));
				} catch (Exception e) {
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

