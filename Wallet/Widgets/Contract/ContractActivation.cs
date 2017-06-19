using System;
using System.Collections.Generic;
using BlockChain;
using Consensus;
using Gtk;
using System.Linq;
namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ContractActivation : DialogBase
	{
		bool _IsActive;
		byte[] _Hash;
		byte[] _Code;
		ulong _KalapasPerBlock;
		ulong _TotalKalapas;
        Func<ulong, byte[], byte[], ContractActivationResult> _ActivateFunc;
		byte[] SecureToken = null;

		public ContractActivation()
		{
			Build();

			hboxStatus.Visible = false; // just hide the f@cking thing already
			hboxStatus.Hide(); // just hide the f@cking thing already

			//eventboxClose.ButtonReleaseEvent += delegate
			//{
			//	CloseDialog();
			//};

			Apply((Label label) =>
			{
				label.ModifyFg(Gtk.StateType.Normal, Constants.Colors.SubText.Gdk);
				label.ModifyFont(Constants.Fonts.DialogContent);
            }, label1, label5, label3, labelKalapas, labelSelectSecureToken, labelStatus);

			buttonApply.Clicked += delegate {
                var result = _ActivateFunc(_TotalKalapas, _Code, SecureToken);

                switch (result.Result)
                {
					case ContractActivationResult.ResultEnum.Error:
						hboxStatus.Visible = true;
						labelStatus.Text = "Error transmiting tx: " + result.TxResult;
						break;
					case ContractActivationResult.ResultEnum.NotEnoughZen:
						hboxStatus.Visible = true;
						labelStatus.Text = "Not enougn Zen";
						break;
					case ContractActivationResult.ResultEnum.Success:
                        CloseDialog();
						break;
                }
			};

			spinBlocks.Changed += (sender, e) =>
			{
				UpdateZenAmount((SpinButton)sender);
			};

			var secureTokenComboboxStore = new ListStore(typeof(byte[]), typeof(string));

			comboboxSecureToken.Model = secureTokenComboboxStore;
			var textRendererSecukreToken = new CellRendererText();
			comboboxSecureToken.PackStart(textRendererSecukreToken, false);
			comboboxSecureToken.AddAttribute(textRendererSecukreToken, "text", 1);

			secureTokenComboboxStore.AppendValues(new byte[] { }, "None");

			foreach (var _asset in App.Instance.Wallet.AssetsMetadata.GetAssetMatadataList())
			{
				secureTokenComboboxStore.AppendValues(_asset.Asset, _asset.Display);
			}

            App.Instance.Wallet.AssetsMetadata.AssetMatadataChanged += t =>
			{
			    Gtk.Application.Invoke(delegate
			    {
			        try
			        {
						bool found = false;
						TreeIter iter;
						secureTokenComboboxStore.GetIterFirst(out iter);

						do
			            {
			                var key = new GLib.Value();
			                secureTokenComboboxStore.GetValue(iter, 0, ref key);
			                byte[] _asset = key.Val as byte[];

			                if (_asset != null && _asset.SequenceEqual(t.Asset))
			                {
			                    secureTokenComboboxStore.SetValue(iter, 1, t.Display);
			                    found = true;
			                    break;
			                }
			            } while (secureTokenComboboxStore.IterNext(ref iter));

			            if (!found)
			            {
			                secureTokenComboboxStore.AppendValues(t.Asset, t.Display);
			            }
			        }
			        catch
			        {
			            Console.WriteLine("Exception in portfolio AssetMatadataChanged handler");
			        }
			    });
			};

			comboboxSecureToken.Changed += (sender, e) =>
			{
				var comboBox = sender as Gtk.ComboBox;
				TreeIter iter;
				comboBox.GetActiveIter(out iter);
				var value = new GLib.Value();
				comboBox.Model.GetValue(iter, 0, ref value);
				byte[] asset = value.Val as byte[];
                SecureToken = asset.Length == 0 ? null : asset;
			};
		}

		void UpdateZenAmount(SpinButton button)
		{
            _TotalKalapas = (ulong)button.Value * _KalapasPerBlock;

			if (App.Instance.Wallet.CanSpend(Tests.zhash, _TotalKalapas))
			{
				buttonApply.Sensitive = true;
                labelKalapas.Text = new Zen(_TotalKalapas) + " Zen";
			}
			else
			{
				buttonApply.Sensitive = false;
				labelKalapas.Text = "Not enough Zen";
			}
		}

        public void ShowDialog(byte[] hash, byte[] code, Func<ulong, byte[], byte[], ContractActivationResult> activateFunc)
		{
			//UInt32 nextBlocks;
			_IsActive = App.Instance.Wallet.IsContractActive(hash/*, out nextBlocks*/);
            _KalapasPerBlock = ActiveContractSet.KalapasPerBlock(System.Text.Encoding.ASCII.GetString(code));
			_Code = code;
            _ActivateFunc = activateFunc;

			if (_IsActive)
			{
//				labelHeader.Text = buttonApply.Label = "Extend a Contract";
//				txtContent.Buffer.Text = $"Contract active for the next {nextBlocks} blocks.\nCost to extend is {_KalapasPerBlock} Kalapas/block";
				txtContent.Buffer.Text = $"Contract active.\nCost to extend is {_KalapasPerBlock} Kalapas/block";
			}
			else
			{
//				labelHeader.Text = buttonApply.Label = "Contract Activation";
				txtContent.Buffer.Text = $"Contract is inactive.\nCost to activate is {_KalapasPerBlock} Kalapas/block";
			}

			UpdateZenAmount(spinBlocks);
			ShowDialog();
		}
	}
}
