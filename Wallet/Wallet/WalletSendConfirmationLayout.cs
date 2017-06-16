using System;
using System.Collections.Generic;
using Gdk;
using Gtk;
using System.Linq;
using Consensus;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class WalletSendConfirmationLayout : WidgetBase
	{
		public WalletSendConfirmationLayout()
		{
			this.Build();

			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.Success.Gdk);
				label.ModifyFont(Constants.Fonts.ActionBarSmall);
			}, labelStatus);

			Apply((EventBox eventbox) =>
			{
				eventbox.ModifyBg(StateType.Normal, Constants.Colors.Textbox.Gdk);
			}, eventboxStatus, eventboxDestination, eventboxAsset, eventboxAmount);

			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.SubText.Gdk);
				label.ModifyFont(Constants.Fonts.ActionBarIntermediate);
			}, labelDestination, labelAsset, labelAmount, labelAmountValue);

			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.SubText.Gdk);
				label.ModifyFont(Constants.Fonts.ActionBarSmall);
			}, labelSelectedAsset, labelSelectedAsset1);

			Apply((Entry entry) =>
			{
				entry.ModifyFg(StateType.Normal, Constants.Colors.Text2.Gdk);
				entry.ModifyFont(Constants.Fonts.ActionBarSmall);
			}, entryDestination);

			buttonBack.Clicked += Back;

			buttonTransmit.Clicked += delegate {
				WalletSendLayout.SendInfo.TxResult = App.Instance.Node.Transmit(WalletSendLayout.Tx);

                if (WalletSendLayout.SendInfo.TxResult == BlockChain.BlockChain.TxResultEnum.Accepted && WalletSendLayout.SendInfo.NeedAutoTx)
				{
					var outputIdx = WalletSendLayout.Tx.outputs.ToList().FindIndex(t => t.@lock is Consensus.Types.OutputLock.ContractLock);
					var outpoint = new Types.Outpoint(Merkle.transactionHasher.Invoke(WalletSendLayout.Tx), (uint)outputIdx);
					var outpointBytes = new byte[] { (byte)outpoint.index }.Concat(outpoint.txHash).ToArray();

                    byte[] witnessData = WalletSendLayout.SendInfo.WitnessData.Initial.Concat(outpointBytes).ToArray();
					witnessData = witnessData.Concat(WalletSendLayout.SendInfo.WitnessData.Final).ToArray();

					Types.Transaction autoTx;
					WalletSendLayout.SendInfo.AutoTxCreated = App.Instance.Wallet.SendContract(WalletSendLayout.SendInfo.Destination.Bytes, witnessData, out autoTx);

                    if (WalletSendLayout.SendInfo.AutoTxCreated)
                    {
                        WalletSendLayout.SendInfo.AutoTxResult = App.Instance.Node.Transmit(autoTx);
                    }
				}
              	UpdateStatus();
			};
		}

		public async void Init()
		{
            var assetName = await App.Instance.Wallet.AssetsMetadata.GetMetadata(WalletSendLayout.SendInfo.Asset);

		//	imageAsset.Pixbuf = ImagesCache.Instance.GetIcon(assetType.Image);
			labelSelectedAsset.Text = labelSelectedAsset1.Text = assetName;

			labelAmountValue.Text = WalletSendLayout.SendInfo.Amount.ToString();
			entryDestination.Text = WalletSendLayout.SendInfo.Destination.ToString();

			UpdateStatus();
		}

		void Back(object sender, EventArgs e)
		{
			FindParent<Notebook>().Page -= 1;
		}

		void UpdateStatus()
		{
            var error = false;
            var messasge = string.Empty;
            var sendInfo = WalletSendLayout.SendInfo;
                
			if (sendInfo.Signed)
			{
				messasge += "Transaction signed successfully. ";
			}
			else
			{
				messasge += "Transaction signing error. ";
                error = true;
            }

            if (sendInfo.NeedAutoTx)
            {
                if (sendInfo.AutoTxCreated)
                {
                    messasge += "AutoTX created successfully. ";
                }
                else
                {
                    messasge += "Error creating AutoTX. ";
                    error = true;
                }
            }

            if (sendInfo.TxResult == BlockChain.BlockChain.TxResultEnum.Accepted)
            {
                messasge += "Transaction broadcasted successfully.";
            }
            else
            {
                messasge += "Transaction broadcasted failed, reason: " + WalletSendLayout.SendInfo.TxResult;
                error = true;
            }

            if (sendInfo.NeedAutoTx)
            {
                if (sendInfo.AutoTxResult == BlockChain.BlockChain.TxResultEnum.Accepted)
                {
                    messasge += "AutoTX broadcasted successfully.";
                }
                else
                {
                    messasge += "AutoTX broadcasted failed, reason: " + WalletSendLayout.SendInfo.AutoTxResult;
                    error = true;
                }
            }

            labelStatus.Text = messasge;

			if (error)
				labelStatus.ModifyFg(StateType.Normal, Constants.Colors.Error.Gdk);
			else
                labelStatus.ModifyFg(StateType.Normal, Constants.Colors.Success.Gdk);
		}
	}
}
