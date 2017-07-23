using System;
namespace Wallet.Widgets
{
	public interface IStatusView
	{
		string Text1 { set; }
		string Text2 { set; }
		string Text3 { set; }
	}
		
    [System.ComponentModel.ToolboxItem(true)]
	public partial class Status : WidgetBase, IStatusView
    {
		public string Text1
		{
			set
			{
				label1.Text = value;
			}
		}

		public string Text2
		{
			set
			{
				label2.Text = value;
			}
		}

		public string Text3
		{
			set
			{
				label3.Text = value;
			}
		}

		public Status()
        {
            this.Build();

            eventbox1.ModifyBg(Gtk.StateType.Normal, Constants.Colors.ButtonUnselected.Gdk);

            Text1 = "Connecting...";
            Text2 = "Inbound connectivity initializing...";
            Text3 = "Blockchain initializing...";

			Apply(t => { 
				t.ModifyFg(Gtk.StateType.Normal, Constants.Colors.Text2.Gdk);
				t.ModifyFont (Constants.Fonts.ActionBarSmall);
			}, label1, label2, label3);
        }
    }
}
