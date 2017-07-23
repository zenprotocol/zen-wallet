using System;
using Gtk;
using Network;

namespace Wallet
{
	public partial class MainWindow : Gtk.Window
	{
		public MainWindowController MainWindowController = new MainWindowController();

		public MainWindow () : base (Gtk.WindowType.Toplevel)
		{
			this.Build ();

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

			MainWindowController.MainMenuView = MainMenu1;
			MainWindowController.MainAreaView = mainarea1;
			MainWindowController.StatusView = status1;

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

