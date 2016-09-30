using System;
using Wallet.Domain;
using Gdk;
using Gtk;

namespace Wallet
{
	public class ExpandingCellRenderer : CellRenderer
	{
		static Gdk.Pixbuf[] icons = {
			Gdk.PixbufLoader.LoadFromResource(Constants.Images.UpArrow).Pixbuf,
			Gdk.PixbufLoader.LoadFromResource(Constants.Images.DownArrow).Pixbuf
		};

		public bool Expanded { get; set; }

		public TransactionItem TransactionItem { get; set; }

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
			gc.RgbFgColor = Constants.Colors.Text;
			//gc.RgbBgColor = new Gdk.Color (0, 0, 0);

			RendererHelper rendererHelper = new RendererHelper(gc, window, widget, exposeArea);

			rendererHelper.Label (TransactionItem.Amount, 100, 10);
			rendererHelper.Label (TransactionItem.Currency, 200, 10);
			rendererHelper.Image(icons[TransactionItem.Direction == DirectionEnum.Recieved ? 0 : 1], 0, 0);

			if (Expanded) {
				rendererHelper.Label ("xx!", 100, 100);
				rendererHelper.Label ("xx!!", 120, 120);
				rendererHelper.Label ("xx!!!", 130, 130);
			}
		}
	}
}

