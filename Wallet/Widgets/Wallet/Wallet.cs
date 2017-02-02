using System;
using Gtk;

namespace Wallet
{
	public interface IWalletView {
		bool ActionBar { set; }
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class Wallet : WidgetBase, IWalletView
	{
		public Wallet ()
		{
			this.Build ();

			WalletController.Instance.WalletView = this;

//			ExposeEvent += (object o, ExposeEventArgs args) => {
//				ActionBar actionBar = FindChild<ActionBar>();
//				Transactions transactions = FindChild<Transactions>();
//				actionBar.Hide();
//			};
		}

		public bool ActionBar { 
			set {
				//What a superb implementation for setting visiblity of ActionBar!

				ActionBar actionBar = FindChild<ActionBar>();
				Transactions transactions = FindChild<Transactions>();

				foreach (Widget widget in vbox1.AllChildren) {
					vbox1.Remove (widget);
				}

				if (value) {
					vbox1.PackStart(actionBar, false, false, 0);
				}

				vbox1.PackStart(transactions, true, true, 0);
			}
		}
	}
}

