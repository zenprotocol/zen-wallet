using System;
using System.Collections.Generic;
using BlockChain;
using Consensus;
using Gtk;

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
        Func<ulong, byte[], ContractActivationResult> _ActivateFunc;

		public ContractActivation()
		{
			Build();

			hboxStatus.Visible = false; // just hide the f@cking thing already
			hboxStatus.Hide(); // just hide the f@cking thing already

			eventboxClose.ButtonReleaseEvent += delegate
			{
				CloseDialog();
			};

			buttonApply.Clicked += delegate {
                var result = _ActivateFunc(_TotalKalapas, _Code);

                switch (result.Result)
                {
					case ContractActivationResult.ResultEnum.Error:
						hboxStatus.Visible = true;
						labelStatus.Text = "Error transmiting tx: " + result.TxResult;
						break;
					case ContractActivationResult.ResultEnum.NotEngoughZen:
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

        public void ShowDialog(byte[] hash, byte[] code, Func<ulong, byte[], ContractActivationResult> activateFunc)
		{
			//UInt32 nextBlocks;
			_IsActive = App.Instance.Wallet.IsContractActive(hash/*, out nextBlocks*/);
            _KalapasPerBlock = ActiveContractSet.KalapasPerBlock(System.Text.Encoding.ASCII.GetString(code));
			_Code = code;
            _ActivateFunc = activateFunc;

			if (_IsActive)
			{
				labelHeader.Text = buttonApply.Label = "Extend a Contract";
//				txtContent.Buffer.Text = $"Contract active for the next {nextBlocks} blocks.\nCost to extend is {_KalapasPerBlock} Kalapas/block";
				txtContent.Buffer.Text = $"Contract active.\nCost to extend is {_KalapasPerBlock} Kalapas/block";
			}
			else
			{
				labelHeader.Text = buttonApply.Label = "Contract Activation";
				txtContent.Buffer.Text = $"Contract is inactive.\nCost to activate is {_KalapasPerBlock} Kalapas/block";
			}

			UpdateZenAmount(spinBlocks);
			ShowDialog();
		}
	}
}
