using System;
using Wallet.Domain;
using Gdk;
using Gtk;
using Cairo;

namespace Wallet
{
	public abstract class LogRendererBase : CellRenderer
	{
		//public ILogEntryRow LogEntryRow { get; set; }

		public override void GetSize(Widget widget, ref Gdk.Rectangle cellArea, out int xOffset, out int yOffset, out int width, out int height)
		{
			xOffset = 0;
			yOffset = 0;
			width = 0;

			height = 50;
		}
	}

	public class LogEntryRenderer : LogRendererBase
	{
		public LogEntryItem LogEntryItem { get; set; }

		private static Gdk.Pixbuf[] icons = {
			Gdk.PixbufLoader.LoadFromResource(Constants.Images.Received).Pixbuf,
			Gdk.PixbufLoader.LoadFromResource(Constants.Images.Sent).Pixbuf
		};

		protected override void Render(Drawable window, Widget widget, Gdk.Rectangle backgroundArea, Gdk.Rectangle cellArea, Gdk.Rectangle exposeArea, CellRendererState flags)
		{
			Gdk.GC gc = widget.Style.TextGC(StateType.Normal);

			var rendererHelper = new RendererHelper(gc, window, widget, exposeArea);

			var MARGIN = 40;
			var STEP = exposeArea.Width / 4;
			var x = MARGIN;

			rendererHelper.Label(
				LogEntryItem.Date,
				x,
				20,
				Constants.Fonts.LogText,
				STEP,
				Constants.Colors.Text
			);

			x += STEP;

			rendererHelper.Image(icons[LogEntryItem.Direction == DirectionEnum.Recieved ? 0 : 1], x + 2, 22);

			rendererHelper.Label(
				LogEntryItem.Amount,
				x + 30,
				20,
				Constants.Fonts.LogText,
				STEP,
				LogEntryItem.Direction == DirectionEnum.Recieved ? Constants.Colors.LogReceived : Constants.Colors.LogSent
			);

			x += STEP;

			rendererHelper.Label(
				LogEntryItem.Id,
				x,
				20,
				Constants.Fonts.LogText,
				157,
				Constants.Colors.Text
			);

			x += STEP;

			rendererHelper.Label(
				LogEntryItem.Balance,
				x,
				20,
				Constants.Fonts.LogText,
				STEP,
				Constants.Colors.TextBlue
			);
		}
	}

	public class LogHeaderRenderer : LogRendererBase
	{
		protected override void Render(Drawable window, Widget widget, Gdk.Rectangle backgroundArea, Gdk.Rectangle cellArea, Gdk.Rectangle exposeArea, CellRendererState flags)
		{
			Gdk.GC gc = widget.Style.TextGC(StateType.Normal);

			var rendererHelper = new RendererHelper(gc, window, widget, exposeArea);

			var MARGIN = 40;
			var STEP = exposeArea.Width / 4;
			var x = MARGIN;

			rendererHelper.Label(
				"DATE",
				x,
				20,
				Constants.Fonts.LogText,
				STEP,
				Constants.Colors.LogHeader
			);

			x += STEP;

			rendererHelper.Label(
				"SENT / RECEIVED",
				x,
				20,
				Constants.Fonts.LogText,
				STEP,
				Constants.Colors.LogHeader
			);

			x += STEP;

			rendererHelper.Label(
				"STATUS",
				x,
				20,
				Constants.Fonts.LogText,
				STEP,
				Constants.Colors.LogHeader
			);

			x += STEP;

			rendererHelper.Label(
				"BALANCE",
				x,
				20,
				Constants.Fonts.LogText,
				STEP,
				Constants.Colors.LogHeader
			);
		}
	}
}

