using System;
using Gtk;
using Wallet.core;
using System.Linq;

namespace Wallet.Widgets.Portfolio
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class PortfolioLayout : WidgetBase, IPortfolioVIew
    {
        public PortfolioLayout()
        {
            this.Build();

			new DeltasController(this);

			labelHeader.ModifyFg(StateType.Normal, Constants.Colors.TextHeader.Gdk);
			labelHeader.ModifyFont(Constants.Fonts.ActionBarBig);
        }

        public decimal Total
        {
            set
            {
                portfoliototals.Total = value;
            }
        }

		public AssetDeltas PortfolioDeltas
		{
			set
			{
				foreach (var item in value)
				{
					if (item.Key.SequenceEqual(Consensus.Tests.zhash))
					{
                        portfoliototals.Total = item.Value;
					}
				}
			}
		}
    }
}
