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


			Pango.FontDescription fontdesc = new Pango.FontDescription();
			fontdesc.Family = "Sans";
			fontdesc.Size = 42;
			//fontdesc.Weight = Pango.Weight.Semibold;
			Gdk.Color fontcolor = new Gdk.Color(0x0F7, 0x0F7, 0x0F7);


			label1.ModifyFg(Gtk.StateType.Normal, fontcolor);
			label2.ModifyFg(Gtk.StateType.Normal, fontcolor);


			HeightRequest = 130;
		}

		public Decimal Rate {
			set {
				label1.Text = value.ToString() + " " + _currency;
			}
		}

		public Decimal Total {
			set {
				label2.Text = value.ToString() + " USD";
			}
		}

		private String _currency;

		public String Currency {
			set {
				_currency = value;
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

