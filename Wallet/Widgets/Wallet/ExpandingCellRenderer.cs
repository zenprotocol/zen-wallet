using System;
using Wallet.Domain;
using Gdk;
using Gtk;
using Cairo;

namespace Wallet
{
	public class ExpandingCellRenderer : CellRenderer
	{
		private static Gdk.Pixbuf[] icons = {
			Gdk.PixbufLoader.LoadFromResource(Constants.Images.UpArrow).Pixbuf,
			Gdk.PixbufLoader.LoadFromResource(Constants.Images.DownArrow).Pixbuf
		};

		public bool Expanded { get; set; }

		public TransactionItem TransactionItem { private get; set; }

		private String GetTimeDescription() {
			TimeSpan timeSpan = DateTime.Now - TransactionItem.Date;

			int monthsDiff = (DateTime.Now.Month - TransactionItem.Date.Month) + 12 * (DateTime.Now.Year - TransactionItem.Date.Year);

			if (monthsDiff >= 1) {
				return Constants.Strings.MonthsAgo (monthsDiff); 
			}
					
			if (timeSpan.TotalDays >= 1) {
				return Constants.Strings.DaysAgo((int)timeSpan.TotalDays);
			}

			return TransactionItem.Date.ToString ();
		}

		private String GetDescrption() {
			return String.Format("{0} {1} {2} ({3} {4})", 
				TransactionItem.Direction == DirectionEnum.Recieved ? Constants.Strings.Received : Constants.Strings.Sent,
				TransactionItem.Amount,
				TransactionItem.Currency,
				"0.34",
				"USD"
			);
		}


		public override void GetSize (Widget widget, ref Gdk.Rectangle cellArea, out int xOffset, out int yOffset, out int width, out int height)
		{
			xOffset = 0;
			yOffset = 0;
			width = 0;

			height = Expanded ? 200 : 50;
		}

		protected override void Render (Drawable window, Widget widget, Gdk.Rectangle backgroundArea, Gdk.Rectangle cellArea, Gdk.Rectangle exposeArea, CellRendererState flags)
		{
			Gdk.GC gc = widget.Style.TextGC(StateType.Normal);


			// hey, what's this? another way to set color.
			/*Gdk.PangoRenderer renderer = Gdk.PangoRenderer.GetDefault(gc.Screen);
			renderer.Drawable = drawingArea.GdkWindow;
			renderer.Gc = drawingArea.Style.BlackGC;
	        renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(200, 30, 30));
	        layout.Alignment = Pango.Alignment.Center;
	        renderer.DrawLayout(layout, 0, 0);
	        
	        renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
	        renderer.Drawable = null;
	        renderer.Gc = null;
			*/

			//gc.RgbFgColor = Constants.Colors.Text.Gdk;
			//gc.RgbBgColor = new Gdk.Color (0, 0, 0);

			RendererHelper rendererHelper = new RendererHelper(gc, window, widget, exposeArea);

			rendererHelper.Image(icons[TransactionItem.Direction == DirectionEnum.Recieved ? 0 : 1], 10, 10);

			TextRenderer textRenderer = new TextRenderer (widget);

			Cairo.Context context = Gdk.CairoHelper.Create(window);

			int TEXT_PADDING = 12;
			int TEXT_PADDING_LEFT = 70;

			textRenderer.RenderLayoutText (context, GetTimeDescription(), 0, TEXT_PADDING + exposeArea.Y, exposeArea.Width, 20, Constants.Colors.Text, Pango.Alignment.Right, Pango.EllipsizeMode.End);
			textRenderer.RenderLayoutText (context, GetDescrption(), TEXT_PADDING_LEFT, TEXT_PADDING + exposeArea.Y, exposeArea.Width, 20, Constants.Colors.Text, Pango.Alignment.Left, Pango.EllipsizeMode.End);

			int EXPANTION_SPACE = 50;
			int ROW_SPACE = 30;
			int ROW_SPACE2 = 50;
			int TEXT_PADDING_RIGHT = 20;
			int HEADER_SIZE = 16;
			int TEXT_SIZE = 14;

			if (Expanded) {
				textRenderer.RenderLayoutText (context, "DATE", TEXT_PADDING_LEFT, EXPANTION_SPACE +  TEXT_PADDING + exposeArea.Y, exposeArea.Width, HEADER_SIZE, Constants.Colors.SubText, Pango.Alignment.Left, Pango.EllipsizeMode.End);
				textRenderer.RenderLayoutText (context, "TO", exposeArea.Width / 2, EXPANTION_SPACE + TEXT_PADDING + exposeArea.Y, exposeArea.Width, HEADER_SIZE, Constants.Colors.SubText, Pango.Alignment.Left, Pango.EllipsizeMode.End);

				//http://www.csharp-examples.net/string-format-datetime/
				textRenderer.RenderLayoutText (context, TransactionItem.Date.ToLongDateString(), TEXT_PADDING_LEFT, EXPANTION_SPACE + ROW_SPACE +  TEXT_PADDING + exposeArea.Y, exposeArea.Width, TEXT_SIZE, Constants.Colors.Text, Pango.Alignment.Left, Pango.EllipsizeMode.End);
				textRenderer.RenderLayoutText (context, TransactionItem.To, exposeArea.Width / 2, EXPANTION_SPACE + ROW_SPACE + TEXT_PADDING + exposeArea.Y, exposeArea.Width, TEXT_SIZE, Constants.Colors.Text, Pango.Alignment.Left, Pango.EllipsizeMode.End);

				textRenderer.RenderLayoutText (context, "TRANSACTION ID", TEXT_PADDING_LEFT, EXPANTION_SPACE + ROW_SPACE + ROW_SPACE2 + TEXT_PADDING + exposeArea.Y, exposeArea.Width, HEADER_SIZE, Constants.Colors.SubText, Pango.Alignment.Left, Pango.EllipsizeMode.End);
				textRenderer.RenderLayoutText (context, "FEE", exposeArea.Width / 2, EXPANTION_SPACE + ROW_SPACE + ROW_SPACE2 + TEXT_PADDING + exposeArea.Y, exposeArea.Width, HEADER_SIZE, Constants.Colors.SubText, Pango.Alignment.Left, Pango.EllipsizeMode.End);

				textRenderer.RenderLayoutText (context, TransactionItem.Id, TEXT_PADDING_LEFT, EXPANTION_SPACE + ROW_SPACE * 2 + ROW_SPACE2 + TEXT_PADDING + exposeArea.Y, exposeArea.Width /2 - TEXT_PADDING_LEFT - TEXT_PADDING_RIGHT, TEXT_SIZE, Constants.Colors.Text, Pango.Alignment.Left, Pango.EllipsizeMode.End);
				textRenderer.RenderLayoutText (context, TransactionItem.Fee.ToString() + " " + TransactionItem.Currency, exposeArea.Width / 2, EXPANTION_SPACE + ROW_SPACE * 2 + ROW_SPACE2 + TEXT_PADDING + exposeArea.Y, exposeArea.Width, TEXT_SIZE, Constants.Colors.Text, Pango.Alignment.Left, Pango.EllipsizeMode.End);
			}

			context.Dispose ();
		}
	}
}

