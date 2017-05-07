using System;
using Gtk;

namespace Wallet
{
	public interface MainView {
	}

	public partial class MainWindow : Gtk.Window, MainView
	{
		public MainWindow () :
			base (Gtk.WindowType.Toplevel)
		{
			this.Build ();
			MainAreaController.Instance.MainView = this;

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

			ModifyBg (Gtk.StateType.Normal, Constants.Colors.Base.Gdk);

			Show ();
		}

		protected void OnDeleteEvent (object sender, DeleteEventArgs a)
		{
			a.RetVal = true;
			App.Instance.Quit();
		}
	}
}

