using System;
using Gtk;
using Wallet.Constants;

namespace Wallet.Widgets.Log
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class LogTotals : WidgetBase
    {
        public LogTotals()
        {
            this.Build();

            Apply(t => {
				t.ModifyFg(StateType.Normal, Constants.Colors.LogHeader.Gdk);
				t.ModifyFont(Constants.Fonts.ActionBarIntermediate);
			}, label5, label7, label9);

			Apply(t =>
			{
				t.ModifyFg(StateType.Normal, Constants.Colors.TextBlue.Gdk);
                t.ModifyFont(Constants.Fonts.LogBig);
			}, labelBalance);

            Apply(t =>
			{
				t.ModifyFg(StateType.Normal, Constants.Colors.LabelText.Gdk);
				t.ModifyFont(Constants.Fonts.LogBig);
			}, labelSent, labelReceived);

			Apply(t =>
			{
                t.ModifyBg(StateType.Normal, Constants.Colors.LogBox.Gdk);
            }, eventbox1, eventbox2, eventbox3);
		}

		public Tuple<decimal, decimal, decimal> Totals
		{
			set
			{
                labelReceived.Text = String.Format(Formats.Money, value.Item1);
                labelSent.Text = String.Format(Formats.Money, value.Item2);
                labelBalance.Text = String.Format(Formats.Money, value.Item3);
			}
		}
    }
}
