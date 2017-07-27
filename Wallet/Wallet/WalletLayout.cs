using System;
using Gtk;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class WalletLayout : WidgetBase, IControlInit
	{
        int _CurrentPage;
		const int RECEIVE_PAGE = 1;
		const int SEND_PAGE = 2;
		//const int SEND_CONFIRM_PAGE = 2;

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

			buttonSend.Clicked += delegate
			{
                SetPage(SEND_PAGE);
			};

			buttonQR.Clicked += delegate
			{
                SetPage(RECEIVE_PAGE);
			};

			buttonKeys.Clicked += delegate {
				Clipboard _clipboard = Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
				_clipboard.Text = System.Convert.ToBase64String(App.Instance.Wallet.GetUnusedKey().Public);
			};

			notebook1.ShowTabs = false;

			Init();
		}

		public void Init()
		{
            _CurrentPage = notebook1.Page;
            SetPage(0);
		}

        public void SetPage(int page)
        {
            _CurrentPage = page;
            InitCurrentPage();
		}

        void InitCurrentPage(bool init = true)
        {
            notebook1.Page = _CurrentPage;
            var ctl = notebook1.GetNthPage(_CurrentPage);

			if (init && ctl is IControlInit)
			{
				((IControlInit)ctl).Init();
			}
		}

        public void PrevPage(bool init = true)
        {
            _CurrentPage--;
            InitCurrentPage(init);
        }

		public void NextPage(bool init = true)
		{
			_CurrentPage++;
			InitCurrentPage(init);
		}
	}
}