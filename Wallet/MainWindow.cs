using System;
using Gtk;

namespace Wallet
{
	public interface MainView {
		Boolean SideMenuVisible { set; }
	}

	public partial class WindowSSS : Gtk.Window, MainView
	{
		public WindowSSS () :
			base (/*Gtk.WindowType.Toplevel*/"ZEN Wallet")
		{
			this.Build ();
			MainAreaController.GetInstance().MainView = this;

//			Gdk.Pixbuf one= Gdk.Pixbuf.LoadFromResource ("Wallet.portfolio.png");
//			one = 
//				one.AddAlpha(true, 255, 255, 255);
//
//			one = one.ScaleSimple (700, 500, Gdk.InterpType.Bilinear);
//			Gdk.Pixmap pixmap, pix_mask;
//			one.RenderPixmapAndMask (out pixmap, out pix_mask, 255);
//			var style = this.Style;
//			style.SetBgPixmap (Gtk.StateType.Normal, pixmap);
//			this.Style = style;

			ModifyBg (Gtk.StateType.Normal, new Gdk.Color (0x024, 0x030, 0x03e));

			Show ();
		}

		protected void OnDeleteEvent (object sender, DeleteEventArgs a)
		{
			MainClass.CloseApp ();
			a.RetVal = true;
		}

		public Boolean SideMenuVisible { 
			set {
				Container c = (Container)Children [0];
				c = (Container)c.Children [2];

				((TestTabsBarVertWidget)c.Children [0]).Visible = value;
			}
		}
	}
}

