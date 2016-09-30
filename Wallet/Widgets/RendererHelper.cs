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

		public void Label(object text, int x, int y) {
			Pango.Layout layout = new Pango.Layout(widget.PangoContext);
			layout.SetText(text.ToString());
			drawable.DrawLayout(gc, exposeArea.X  + x, exposeArea.Y +  y, layout);
		}

		public void Image(Gdk.Pixbuf image, int x, int y) {
			drawable.DrawPixbuf(gc, image, 0, 0, exposeArea.X + x, exposeArea.Y + y, 40, 40, RgbDither.None, 0,0);
		}
	}
}

