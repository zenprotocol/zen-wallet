using System;
using Wallet.Domain;
using Gdk;
using Gtk;
using Cairo;

namespace Wallet
{
	public abstract class PortfolioRendererBase : CellRenderer
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

	public class PortfolioEntryRenderer : LogRendererBase
	{
		public String Asset { private get; set; }
		public long Value { private get; set; }

		protected override void Render(Drawable window, Widget widget, Gdk.Rectangle backgroundArea, Gdk.Rectangle cellArea, Gdk.Rectangle exposeArea, CellRendererState flags)
		{
			Gdk.GC gc = widget.Style.TextGC(StateType.Normal);
			var rendererHelper = new RendererHelper(gc, window, widget, exposeArea);

			var MARGIN = 40;
			var STEP = exposeArea.Width / 2;
			var x = MARGIN;

			rendererHelper.Label(
				Asset,
				x,
				20,
				Constants.Fonts.LogText,
				STEP,
				Constants.Colors.Text
			);

			x += STEP;

			rendererHelper.Label(
				Value,
				x,
				20,
				Constants.Fonts.LogText,
				STEP,
				Constants.Colors.TextBlue
			);
		}
	}

    public class PortfolioHeaderRenderer : LogRendererBase
	{
		protected override void Render(Drawable window, Widget widget, Gdk.Rectangle backgroundArea, Gdk.Rectangle cellArea, Gdk.Rectangle exposeArea, CellRendererState flags)
		{
			Gdk.GC gc = widget.Style.TextGC(StateType.Normal);

			var rendererHelper = new RendererHelper(gc, window, widget, exposeArea);

			var MARGIN = 40;
			var STEP = exposeArea.Width / 2;
			var x = MARGIN;

			rendererHelper.Label(
				"ASSET",
				x,
				20,
				Constants.Fonts.LogText,
				STEP,
				Constants.Colors.LogHeader
			);

			x += STEP;

			rendererHelper.Label(
				"VALUE",
				x,
				20,
				Constants.Fonts.LogText,
				STEP,
				Constants.Colors.LogHeader
			);
		}
	}
}