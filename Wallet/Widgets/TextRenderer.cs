using System;
using Cairo;

namespace Wallet
{
	public class TextRenderer
	{
		Pango.Layout layout;

		public Gtk.Widget ReferenceWidget { private get; set; }

		public TextRenderer (Gtk.Widget referenceWidget)
		{
			ReferenceWidget = referenceWidget;
		}
			
		//TODO: Deprecate Cairo usage entirely
		public Gdk.Rectangle RenderLayoutText (Context cr, String text, int x, int y, int width, int textHeight,
			Constants.Color color, Pango.Alignment align, Pango.EllipsizeMode ellipse)
		{
			if (string.IsNullOrEmpty (text)) return new Gdk.Rectangle ();

			if (layout != null) {
				layout.Context.Dispose ();
				layout.FontDescription.Dispose ();
				layout.Dispose ();
			}

			layout = new Pango.Layout (ReferenceWidget.CreatePangoContext ());
			layout.FontDescription = new Pango.FontDescription ();
			layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (textHeight);

			layout.Width = Pango.Units.FromPixels (width);
			layout.Ellipsize = ellipse;
			layout.Alignment = align;

			if (ellipse == Pango.EllipsizeMode.None)
				layout.Wrap = Pango.WrapMode.WordChar;

			text = string.Format ("<span foreground=\"#{0}\">{1}</span>", color, text);
			layout.SetMarkup (text);

			cr.Rectangle (x, y, width, 155);
			cr.Clip ();
			cr.MoveTo (x, y);
			Pango.CairoHelper.ShowLayout (cr, layout);
			Pango.Rectangle strong, weak;
			layout.GetCursorPos (layout.Lines [layout.LineCount-1].StartIndex + 
				layout.Lines [layout.LineCount-1].Length, 
				out strong, out weak);
			cr.ResetClip ();
			return new Gdk.Rectangle (Pango.Units.ToPixels (weak.X) + x,
				Pango.Units.ToPixels (weak.Y) + y,
				Pango.Units.ToPixels (weak.Width),
				Pango.Units.ToPixels (weak.Height));
		}
	}
}

