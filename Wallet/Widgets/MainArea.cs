using System;
using Gtk;

namespace Wallet
{
	public interface MainAreaView {
		//int Page { set; }
		Type Control { set; }
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class MainArea : WidgetBase, MainAreaView
	{
		MainAreaController MainAreaController = MainAreaController.GetInstance ();

		public MainArea ()
		{
			this.Build ();
			MainAreaController.MainAreaView = this;
			FindChild<Notebook>().ShowTabs = false; //Tabs used during development in designer
		}

//		public int Page {
//			set {
//				FindChild<Notebook> ().Page = value;	
//			}
//		}

		public Type Control { 
			set { 
				for (int i = 0; i < FindChild<Notebook>().Children.Length; i++) {
					FindChild<Notebook>().GetNthPage (i).Visible = FindChild<Notebook> ().GetNthPage (i).GetType () == value;
				}
			}
		}
	}
}

