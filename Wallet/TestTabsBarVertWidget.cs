using System;
using Gtk;

namespace Wallet
{
	public interface TestTabsBarVertView {
		int Default { set; }
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class TestTabsBarVertWidget : Gtk.Bin, TestTabsBarVertView
	{
		MainAreaController MainAreaController = MainAreaController.GetInstance ();

		public TestTabsBarVertWidget ()
		{
			this.Build ();
			MainAreaController.TestTabsBarVertView = this;

			WidthRequest = 170;
		}

		public int Default { 
			set {
				((WidgetMyButton) ((Container)Children[0]).Children[value]).Select();
			}
		}
	}
}

