using System;
using Gtk;

namespace Wallet
{
	public interface TestTabsBarView {
		int Default { set; }
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class TestTabsBarWidget : Gtk.Bin, TestTabsBarView
	{
		MainAreaController MainAreaController = MainAreaController.GetInstance ();

		public TestTabsBarWidget ()
		{
			this.Build ();
			MainAreaController.TestTabsBarView = this;

			HeightRequest = 100;
		}

		public int Default { 
			set {
				((WidgetMyButton) ((Container)Children[0]).Children[value]).Select();
			}
		}
	}
}

