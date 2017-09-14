using System;
using Gtk;
using Wallet.Constants;

namespace Wallet.Widgets.Portfolio
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class PortfolioTotals : WidgetBase
    {
        public PortfolioTotals()
        {
			this.Build();

			Apply(t =>
			{
				t.ModifyFg(StateType.Normal, Constants.Colors.LogHeader.Gdk);
				t.ModifyFont(Constants.Fonts.ActionBarIntermediate);
			}, label9);

			Apply(t =>
			{
				t.ModifyFg(StateType.Normal, Constants.Colors.TextBlue.Gdk);
				t.ModifyFont(Constants.Fonts.LogBig);
			}, labelBalance);

			Apply(t =>
			{
				t.ModifyBg(StateType.Normal, Constants.Colors.LogBox.Gdk);
			}, eventbox3);

			Apply(t =>
			{
                t.ModifyBg(StateType.Normal, Constants.Colors.DialogBackground.Gdk);
            }, eventbox1, eventbox2);
		}

		public decimal Total
		{
			set
			{
				labelBalance.Text = String.Format(Formats.Money, value);
			}
		}
    }
}
