using System;
using System.Collections.Generic;
using BlockChain;
using Consensus;
using Gtk;
using System.Linq;
namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
    public partial class ContractActivation : DialogBase, IAssetsView
	{
		bool _IsActive;
		byte[] _Hash;
		byte[] _Code;
		ulong _KalapasPerBlock;
		ulong _TotalKalapas;
        Func<ulong, byte[], byte[], ContractActivationResult> _ActivateFunc;
		byte[] SecureToken = null;

        UpdatingStore<byte[]> _SecureTokenComboboxStore = new UpdatingStore<byte[]>(
			0,
			typeof(byte[]),
			typeof(string)
		);

        readonly AssetsController _AssetsController;

        public ContractActivation()
		{
			Build();

            _AssetsController = new AssetsController(this);

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

			buttonActivate.Clicked += delegate {
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

			comboboxSecureToken.Model = _SecureTokenComboboxStore;
			var textRendererSecukreToken = new CellRendererText();
			comboboxSecureToken.PackStart(textRendererSecukreToken, false);
			comboboxSecureToken.AddAttribute(textRendererSecukreToken, "text", 1);

			_SecureTokenComboboxStore.AppendValues(new byte[] { }, "None");

			comboboxSecureToken.Changed += (sender, e) =>
			{
				var comboBox = sender as Gtk.ComboBox;
				TreeIter iter;
                if (comboBox.GetActiveIter(out iter))
                {
                    var value = new GLib.Value();
                    comboBox.Model.GetValue(iter, 0, ref value);
                    byte[] asset = value.Val as byte[];
                    SecureToken = asset != null && asset.Length == 0 ? null : asset;
                }
                else
                {
                    SecureToken = null;
                }
			};
		}

		void UpdateZenAmount(SpinButton button)
		{
            _TotalKalapas = (ulong)button.Value * _KalapasPerBlock;

			if (App.Instance.Wallet.CanSpend(Tests.zhash, _TotalKalapas))
			{
				buttonActivate.Sensitive = true;
                labelKalapas.Text = new Zen(_TotalKalapas) + " Zen";
			}
			else
			{
				buttonActivate.Sensitive = false;
				labelKalapas.Text = "Not enough Zen";
			}
		}

        public void ShowDialog(byte[] hash, byte[] code, Func<ulong, byte[], byte[], ContractActivationResult> activateFunc)
		{
			//UInt32 nextBlocks;
			_IsActive = App.Instance.Wallet.IsContractActive(hash/*, out nextBlocks*/);
            _KalapasPerBlock = code == null || code.Length == 0 ? 0 : ActiveContractSet.KalapasPerBlock(System.Text.Encoding.ASCII.GetString(code));
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

        public ICollection<AssetMetadata> Assets 
        { 
            set
            {
                foreach (var item in value)
                    _SecureTokenComboboxStore.Update(t => t.SequenceEqual(item.Asset), item.Asset, item.Display);
			} 
        }

		public AssetMetadata AssetUpdated
		{
			set
			{
				_SecureTokenComboboxStore.UpdateColumn(t => t.SequenceEqual(value.Asset), 1, value.Display);
			}
		}
	}
}
