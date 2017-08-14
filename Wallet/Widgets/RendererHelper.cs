using System;
using Gdk;
using Gtk;

namespace Wallet
{
	public class RendererHelper
	{
		private Drawable drawable; 
		private Gdk.GC gc;
		private Gdk.Rectangle exposeArea;
		private Widget widget;

		public RendererHelper (Gdk.GC gc, Drawable drawable, Widget widget, Gdk.Rectangle exposeArea)
		{
			this.gc = gc;
			this.drawable = drawable;
			this.exposeArea = exposeArea;
			this.widget = widget;
		}

        public void Label(System.Object value, int x, int y, Pango.FontDescription font, int width, Constants.Color color) {
			Pango.Layout layout = new Pango.Layout(widget.PangoContext);
			LayoutHelper layoutHelper = LayoutHelper.Factor (value);

            layout.SetMarkup("<span color=" + (char)34 + "#" + color.ToString() + (char)34 + ">" + layoutHelper.Text + "</span>");

			if (width != 0) {
				layout.Ellipsize = Pango.EllipsizeMode.End;
				layout.Width = Pango.Units.FromPixels (width);
			}

		//	layout.Alignment = layoutHelper.Alignment;
			layout.FontDescription = font;

			drawable.DrawLayout(gc, exposeArea.X  + x, exposeArea.Y + y, layout);
		}

//		public void Divider(int x, int height) {
//			drawable.DrawLine (gc, exposeArea.X + x, exposeArea.Y - 1, exposeArea.X + x, exposeArea.Y  + height);
//		}

		public void Image(Gdk.Pixbuf image, int x, int y) {
			drawable.DrawPixbuf(gc, image, 0, 0, exposeArea.X + x, exposeArea.Y + y, image.Width, image.Height, RgbDither.None, 0, 0);
		}
	}
}

