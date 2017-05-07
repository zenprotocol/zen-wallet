using System;
using Gtk;

namespace Wallet
{
	public interface IMainAreaView {
		//int Page { set; }
		Type Control { set; }
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class MainArea : WidgetBase, IMainAreaView
	{
		MainAreaController MainAreaController = MainAreaController.Instance;

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

