using System;
using System.Collections.Generic;
using BlockChain;
using Consensus;
using Gtk;
using Microsoft.FSharp.Collections;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ContractActivation : DialogBase
	{
		bool _IsActive;
		byte[] _Hash;
		byte[] _Code;
		ulong _KalapasPerBlock;
		ulong _ZenAmount;

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
				Types.Transaction tx;
				if (!App.Instance.Wallet.SacrificeToContract(_Hash, _Code, _ZenAmount, out tx))
				{
					hboxStatus.Visible = true;
					labelStatus.Text = "Not enougn Zen";
					return;
				}

				var txResult = App.Instance.Node.Transmit(tx);

				if (txResult != BlockChain.BlockChain.TxResultEnum.Accepted)
				{
					hboxStatus.Visible = true;
					labelStatus.Text = "Error transmiting tx: " + txResult;
					return;
				}

				CloseDialog();
			};

			spinBlocks.Changed += (sender, e) =>
			{
				UpdateZenAmount((SpinButton)sender);
			};
		}

		void UpdateZenAmount(SpinButton button)
		{
			_ZenAmount = (ulong)(button.Value * _KalapasPerBlock / Math.Pow(10, 3)); //TODO: kalapas per zen?

			if (App.Instance.Wallet.CanSpend(Tests.zhash, _ZenAmount))
			{
				buttonApply.Sensitive = true;
				labelKalapas.Text = _ZenAmount + " Zen";
			}
			else
			{
				buttonApply.Sensitive = false;
				labelKalapas.Text = "Not enough Zen";
			}
		}

		public void ShowDialog(byte[] hash, byte[] code)
		{
			UInt32 nextBlocks;
			_IsActive = App.Instance.Wallet.IsContractActive(hash, out nextBlocks);
			_KalapasPerBlock = ActiveContractSet.KalapasPerBlock(code);
			_Code = code;

			if (_IsActive)
			{
				labelHeader.Text = buttonApply.Label = "Extend a Contract";
				txtContent.Buffer.Text = $"Contract active for the next {nextBlocks} blocks.\nCost to extend is {_KalapasPerBlock} Kalapas/block";
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
