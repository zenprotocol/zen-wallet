using System;
using Wallet.Domain;
using Gdk;
using Gtk;
using Cairo;

namespace Wallet
{
	public class LogCellRenderer : CellRenderer
	{
		public ILogEntryRow LogEntryRow { private get; set; }

		public override void GetSize (Widget widget, ref Gdk.Rectangle cellArea, out int xOffset, out int yOffset, out int width, out int height)
		{
			xOffset = 0;
			yOffset = 0;
			width = 0;

			height = 50;
		}

//		protected override void Render (Drawable window, Widget widget, Gdk.Rectangle backgroundArea, Gdk.Rectangle cellArea, Gdk.Rectangle exposeArea, CellRendererState flags)
//		{
//			Cairo.Context context = Gdk.CairoHelper.Create(window);
//
//			RenderValues (context, widget, exposeArea.Y, exposeArea.Width, cellArea);
//
//			context.Dispose ();
//		}

		protected override void Render (Drawable window, Widget widget, Gdk.Rectangle backgroundArea, Gdk.Rectangle cellArea, Gdk.Rectangle exposeArea, CellRendererState flags)
		{
			Gdk.GC gc = widget.Style.TextGC(StateType.Normal);

			//TODO: look for setting color in a different way. this relates to a display bug in Mac
			gc.RgbFgColor = LogEntryRow is LogHeaderRow ? Constants.Colors.Text.Gdk : Constants.Colors.SubText.Gdk;

			RendererHelper rendererHelper = new RendererHelper(gc, window, widget, exposeArea);

			int TEXT_PADDING = 15;

			int length = LogEntryRow.Values.Length;

			int offset = LogEntryRow.Offset;

			for (int i = 0; i < length; i++) {

				int PADDING = 10;

				rendererHelper.Label(
					LogEntryRow.Values[i],
					PADDING  + (i + offset) * exposeArea.Width / (length + offset), 
					TEXT_PADDING, 
					LogEntryRow is LogHeaderRow ? Constants.Fonts.LogHeader : Constants.Fonts.LogText,
					exposeArea.Width / (length + offset) - PADDING
				);
			}
		}

//		private void RenderValues(Cairo.Context context, Widget widget, int y, int width, Gdk.Rectangle cellArea) {
//			int TEXT_PADDING = 12;
//			int length = LogEntryRow.Values.Length;
//
//			int offset = LogEntryRow.Offset;
//
//			for (int i = 0; i < length; i++) {
//				RenderLayoutText (
//					context, 
//					widget,
//					i,
//					(i + offset) * width / (length + offset),
//					TEXT_PADDING + y, 
//					width / (length + offset), 
//					20, 
//					Constants.Colors.Text, 
//					i + offset != 0,
//					cellArea
//				);
//			}
//		}

//		Pango.Layout layout;
//		int DIVIDER_WIDTH = 20;
//
//		public Gdk.Rectangle RenderLayoutText (Context cr, Widget widget, int cellIndex, int x, int y, int width, int textHeight,
//			Constants.Color color, bool drawDivider, Gdk.Rectangle cellArea)
//		{
//			Pango.EllipsizeMode ellipse = Pango.EllipsizeMode.End;
//			Pango.Alignment align = Pango.Alignment.Center;
//			String text = String.Empty;
//
//			System.Object value = LogEntryRow.Values [cellIndex];
//
//			if (value.GetType () == typeof(String)) {
//				align = LogEntryRow is LogHeaderRow ? Pango.Alignment.Center :  Pango.Alignment.Left;
//				text = value.ToString();
//			} if (value.GetType () == typeof(Decimal) || value.GetType () == typeof(Double) || value.GetType () == typeof(int)) {
//				align = Pango.Alignment.Right;
//				text = value + ".00";
//			} if (value.GetType () == typeof(DateTime)) {
//				align = Pango.Alignment.Left;
//				text = String.Format ("{0:G}", value);
////			} else {
////				Console.WriteLine (value);
////				Console.WriteLine (value.GetType());
//			}
//
//			if (layout != null) {
//				layout.Context.Dispose ();
//				layout.FontDescription.Dispose ();
//				layout.Dispose ();
//			}
//
//			layout = new Pango.Layout (widget.CreatePangoContext ());
//			layout.FontDescription = new Pango.FontDescription ();
//			layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (textHeight);
//
//			layout.Width = Pango.Units.FromPixels (width - (drawDivider ? DIVIDER_WIDTH : 0));
//			layout.Ellipsize = ellipse;
//			layout.Alignment = align;
//
//			if (ellipse == Pango.EllipsizeMode.None)
//				layout.Wrap = Pango.WrapMode.WordChar;
//
//			cr.SetSourceRGBA (0, 0, 1, 1 * 0.7);
//			layout.SetMarkup (text);
//
//			cr.Rectangle (x, y, width, 50);
//			cr.Clip ();
//			cr.MoveTo (x + (drawDivider ? DIVIDER_WIDTH : 0), y);
//			Pango.CairoHelper.ShowLayout (cr, layout);
//			Pango.Rectangle strong, weak;
//			layout.GetCursorPos (layout.Lines [layout.LineCount-1].StartIndex + 
//				layout.Lines [layout.LineCount-1].Length, 
//				out strong, out weak);
//			cr.ResetClip ();
//
//			int TEXT_PADDING = 12;
//
//			if (drawDivider) {
//				cr.SetSourceRGBA (0, 0, 1, 1 * 0.7);
//				cr.Rectangle (x, y - TEXT_PADDING - 1, DIVIDER_WIDTH, 50 + 1 + 1 + 1 + 1);
//				cr.Fill ();  
//
//				Console.WriteLine (cellArea.Y + " " + cellArea.Height);
//			}
//
//			return new Gdk.Rectangle (Pango.Units.ToPixels (weak.X) + x,
//				Pango.Units.ToPixels (weak.Y) + y,
//				Pango.Units.ToPixels (weak.Width),
//				Pango.Units.ToPixels (weak.Height));
//		}
	}
}

