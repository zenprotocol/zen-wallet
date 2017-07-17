using System;
using Gtk;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class WalletLayout : WidgetBase, IControlInit
	{
		const int RECEIVE_PAGE = 1;
		const int SEND_PAGE = 2;

		public WalletLayout()
		{
			this.Build();

			eventbox1.ModifyBg(StateType.Normal, Constants.Colors.DialogBackground.Gdk);

			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.SubText.Gdk);
				label.ModifyFont(Constants.Fonts.ActionBarBig);
			}, label1);

			entryAddress.ModifyFg(StateType.Normal, Constants.Colors.Text2.Gdk);
			entryAddress.ModifyFont(Constants.Fonts.ActionBarSmall);

			var key = App.Instance.Wallet.GetUnusedKey().Address.ToString();

			Clipboard clipboard = Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
			buttonCopy.Clicked += delegate
			{
				clipboard.Text = key;
			};

			entryAddress.Text = key;

			entryAddress.SelectRegion(0, -1);

			entryAddress.FocusGrabbed += (sender, e) => {
				new System.Threading.Thread(() =>
				{
					Application.Invoke(delegate
					{
						System.Threading.Thread.Sleep(150);
						entryAddress.SelectRegion(0, -1);
						System.Threading.Thread.Sleep(100);
						entryAddress.SelectRegion(0, -1);
					});
				}).Start();
			};

			//entryAddress.FocusInEvent += (o, args) => { 
			//};

			buttonSend.Clicked += delegate
			{
				notebook1.Page = SEND_PAGE;
			};

			buttonQR.Clicked += delegate
			{
				notebook1.Page = RECEIVE_PAGE;
			};

			buttonKeys.Clicked += delegate {
				//new KeysDialog().ShowDialog();
				Clipboard _clipboard = Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
				_clipboard.Text = System.Convert.ToBase64String(App.Instance.Wallet.GetUnusedKey().Public);
			};

			notebook1.ShowTabs = false;

			Init();
		}

		public void Init()
		{
			notebook1.Page = 0;
		}
	}
}
