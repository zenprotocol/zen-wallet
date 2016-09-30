using System;
using Gtk;

namespace Wallet
{
	public interface MainAreaView {
		int Page { set; }
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class MainArea : WidgetBase, MainAreaView
	{
		MainAreaController MainAreaController = MainAreaController.GetInstance ();

		public MainArea ()
		{
			this.Build ();
			MainAreaController.MainAreaView = this;
		}

		public int Page { 
			set {
				notebook1.Page = value;

				WidgetBase widgetBase = FindChild<Notebook>().Children [value] as WidgetBase;

				if (widgetBase != null) {
					FocusableWidget focusableWidget = widgetBase.FindChild<FocusableWidget>();
						
					if (focusableWidget != null) {
						focusableWidget.Focus();
					}
				}
			}
		}
	}
}

