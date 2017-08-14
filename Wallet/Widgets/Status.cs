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
		string _Text1;
		string _Text2;
		string _Text3;
		
        public string Text1
		{
			set
			{
                _Text1 = value;
                Update();
            }
		}

		public string Text2
		{
			set
			{
				_Text2 = value;
				Update();
			}
		}

		public string Text3
		{
			set
			{
				_Text3 = value;
				Update();
			}
		}

        void Update()
        {
            label1.Text = _Text1;
			Append(_Text2);
			Append(_Text3);
		}

        void Append(string text)
        {
			if (!string.IsNullOrEmpty(text))
				label1.Text += "   |   " + text;
		}

		public Status()
        {
            this.Build();

            eventbox1.ModifyBg(Gtk.StateType.Normal, Constants.Colors.ButtonUnselected.Gdk);

            Text1 = "Connecting...";
            Text2 = "Inbound connectivity initializing...";
            //Text3 = "Blockchain initializing...";

			Apply(t => { 
				t.ModifyFg(Gtk.StateType.Normal, Constants.Colors.Text.Gdk);
				t.ModifyFont (Constants.Fonts.ActionBarSmall);
			}, label1);
        }
    }
}
