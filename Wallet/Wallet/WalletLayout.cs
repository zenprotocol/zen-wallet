using System;
using System.Threading.Tasks;
using Gtk;
using Wallet.core.Data;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class WalletLayout : WidgetBase, IControlInit
	{
        private core.Data.Key _Key { get; set; }
		int _CurrentPage;
		const int RECEIVE_PAGE = 1;
		const int SEND_PAGE = 2;
		//const int SEND_CONFIRM_PAGE = 2;

        public string Address { get { return _Key.Address.ToString(); }}

		public WalletLayout()
		{
			this.Build();

            hbox4.Remove(labelPublicKeyCopied);

            eventbox1.ModifyBg(StateType.Normal, Constants.Colors.DialogBackground.Gdk);
            eventboxSeperator.ModifyBg(StateType.Normal, Constants.Colors.Seperator.Gdk);

			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.TextHeader.Gdk);
				label.ModifyFont(Constants.Fonts.ActionBarBig);
			}, labelYourAddress);

			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.Text.Gdk);
			//	label.ModifyFont(Constants.Fonts.ActionBarBig);
            }, labelPublicKeyCopied);

			Apply((Entry entry) =>
            {
				entry.ModifyBg(StateType.Normal, Constants.Colors.Seperator.Gdk);
				entry.ModifyText(StateType.Normal, Constants.Colors.Text.Gdk);
				entry.ModifyFont(Constants.Fonts.ActionBarSmall);
				entry.ModifyBase(StateType.Normal, Constants.Colors.ButtonUnselected.Gdk);
			}, entryAddress);

			Clipboard clipboard = Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));

            hboxCopy.Remove(imageCopied);
            ButtonPressEvent(eventboxCopy, delegate
            {
                clipboard.Text = Address;

				new System.Threading.Thread(() =>
				{
					hboxCopy.Remove(imageCopy);
					hboxCopy.Add(imageCopied);
					System.Threading.Thread.Sleep(2000);
					Application.Invoke(delegate
					{
                        hboxCopy.Remove(imageCopied);
                        hboxCopy.Add(imageCopy);
					});
				}).Start();
            });

			entryAddress.SelectRegion(0, -1);

			entryAddress.FocusGrabbed += (sender, e) =>
			{
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

            eventboxSend.ButtonPressEvent += delegate
			{
				SetPage(SEND_PAGE);
			};

            eventboxQRCode.ButtonPressEvent += delegate
			{
				SetPage(RECEIVE_PAGE);
			};

            eventboxCopyPublicKey.ButtonPressEvent += delegate
			{
				Clipboard _clipboard = Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
                _clipboard.Text = Convert.ToBase64String(_Key.Public);

                hbox4.Add(labelPublicKeyCopied);

                Task.Run(()=>Task.Delay(2000).ContinueWith(delegate
                {
                    Application.Invoke(delegate {
                        hbox4.Remove(labelPublicKeyCopied);
                    });
                }));
			};

			notebook1.ShowTabs = false;

			Init();
		}

		public async void Init()
		{
            _Key = await Task.Run(() => App.Instance.Wallet.GetUnusedKey());
			
            Application.Invoke(delegate {
				entryAddress.Text = Address;
				_CurrentPage = notebook1.Page;
				SetPage(0); 
            });
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