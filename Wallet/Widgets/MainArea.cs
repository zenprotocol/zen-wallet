using System;
using Gtk;

namespace Wallet
{
	public interface IMainAreaView {
		Type Control { set; }
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class MainArea : WidgetBase, IMainAreaView
	{
		public MainArea ()
		{
			this.Build ();
			FindChild<Notebook>().ShowTabs = false; //Tabs used during development in designer
		}

		public Type Control { 
			set { 
				for (int i = 0; i < FindChild<Notebook>().Children.Length; i++) {
					var ctl = FindChild<Notebook>().GetNthPage(i);

					if (ctl is IControlInit)
					{
						((IControlInit)ctl).Init();
					}

					FindChild<Notebook>().GetNthPage (i).Visible = ctl.GetType () == value;
				}
			}
		}
	}
}

