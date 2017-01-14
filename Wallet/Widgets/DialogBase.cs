using System;
using Gtk;

namespace Wallet
{
	public class DialogBase : WidgetBase
	{
		private Dialog dialog;

		public static Window parent;

		public void Resize() {
			if (dialog != null) {
				dialog.Resize(1, 1); // A hah
			}
		}
	
		public void ShowDialog(/*Window parent*/) {
			dialog = new Dialog (null, parent, DialogFlags.Modal | DialogFlags.DestroyWithParent);
			dialog.Decorated = false;
			dialog.Modal = true;
			Gdk.GC gc = parent.Style.TextGC(StateType.Normal);
			gc.RgbFgColor = Constants.Colors.Text.Gdk;

			dialog.ModifyBg (Gtk.StateType.Normal, Constants.Colors.DialogBackground.Gdk);
			dialog.VBox.PackStart (this, false, false, 0);

//			dialog.ExposeEvent += (o, a) => {
//
//				{
//				//	DrawingArea area = (DrawingArea) o;
//					Cairo.Context cr =  Gdk.CairoHelper.Create(dialog.GdkWindow);
//
//					cr.LineWidth = 9;
//					cr.SetSourceRGB(0.7, 0.2, 0.0);
//
//					int width, height;
//					width = Allocation.Width;
//					height = Allocation.Height;
//
//					cr.Rectangle(0, 0, width - 20, height - 10);
//
//					cr.SetSourceRGB(0.3, 0.4, 0.6);
//					cr.Fill();
//
//				
//					cr.LineWidth = 0.5;
//
//				//	int width, height;
//					width = Allocation.Width;
//					height = Allocation.Height;
//
//					cr.Translate(width/2, height/2);
//					cr.Arc(0, 0, 120, 0, 2*Math.PI);
//					cr.Stroke();
//
//					cr.Save();
//
//					for (int i = 0; i < 36; i++) {
//						cr.Rotate( i*Math.PI/36);
//						cr.Scale(0.3, 1);
//						cr.Arc(0, 0, 120, 0, 2*Math.PI);
//						cr.Restore();
//						cr.Stroke();
//						cr.Save();
//					}
//
//					((IDisposable) cr.Target).Dispose();                                      
//					((IDisposable) cr).Dispose();
//				}
//
//
//				using (Cairo.Context cr = Gdk.CairoHelper.Create (dialog.GdkWindow)) {
//					double radius = 5;
//					int x = a.Event.Area.X;
//					int y = a.Event.Area.Y;
//					int width = a.Event.Area.Width;
//					int height = a.Event.Area.Height;
//
//					cr.MoveTo (x + radius, y);
//					cr.Arc (x + width - radius, y + radius, radius, Math.PI * 1.5, Math.PI * 2);
//					cr.Arc (x + width - radius, y + height - radius, radius, 0, Math.PI * .5);
//					cr.Arc (x + radius, y + height - radius, radius, Math.PI * .5, Math.PI);
//					cr.Arc (x + radius, y + radius, radius, Math.PI, Math.PI * 1.5);
//					cr.Color = new Cairo.Color (.5, .3, .7, 0.5);
//					cr.Fill ();
//				}
//			};

			ShowAll ();

			dialog.Run ();
		}

		protected void CloseDialog() {
			dialog.Destroy ();
		}

		protected Widget CloseControl {
			set {
				value.ButtonReleaseEvent += (object o, ButtonReleaseEventArgs args) => {
					CloseDialog();
				};
			}
		}
	}
}

