using System;
using Wallet.Domain;
using Gdk;
using Gtk;
using System.Collections.Generic;

namespace Wallet
{
	public class RowRenderer : CellRenderer
	{
		static Dictionary<string, Gdk.Pixbuf> _IconsCache = new Dictionary<string, Pixbuf>();
		static Gdk.Pixbuf GetIcon(string image)
		{
			if (!_IconsCache.ContainsKey(image))
			{
				_IconsCache[image] = new Pixbuf(image).ScaleSimple(32, 32, InterpType.Hyper);
			}

			return _IconsCache[image];
		}

		public byte[] Asset { private get; set; }
		public long Value { private get; set; }


		public override void GetSize (Widget widget, ref Gdk.Rectangle cellArea, out int xOffset, out int yOffset, out int width, out int height)
		{
			xOffset = 0;
			yOffset = 0;
			width = 0;

			height = 50;
		}

		protected override void Render (Drawable window, Widget widget, Gdk.Rectangle backgroundArea, Gdk.Rectangle cellArea, Gdk.Rectangle exposeArea, CellRendererState flags)
		{
			var gc = widget.Style.TextGC(StateType.Normal);

			var asset = App.Instance.Wallet.AssetsMetadata[Asset];

			var rendererHelper = new RendererHelper(gc, window, widget, exposeArea);

			if (asset.Image != null)
				rendererHelper.Image(GetIcon(asset.Image), 50, 10);

			var textRenderer = new TextRenderer (widget);

			var context = Gdk.CairoHelper.Create(window);

			int TEXT_PADDING = 14;
			int TEXT_PADDING_LEFT = 60;

			textRenderer.RenderLayoutText (context, asset.Caption, TEXT_PADDING_LEFT + 60, TEXT_PADDING + exposeArea.Y, exposeArea.Width, 20, Constants.Colors.Text, Pango.Alignment.Left, Pango.EllipsizeMode.End);
			textRenderer.RenderLayoutText(context, string.Format(Constants.Formats.Money, Value), 0, TEXT_PADDING + exposeArea.Y, exposeArea.Width, 20, Constants.Colors.Text, Pango.Alignment.Right, Pango.EllipsizeMode.End, -TEXT_PADDING_LEFT);

			context.Dispose ();
		}
	}
}