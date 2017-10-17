﻿using System;
using System.Collections.Generic;
using Gdk;
using Gtk;
using System.Linq;
using Consensus;
using System.Threading.Tasks;
using BlockChain.Data;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
    public partial class WalletSendConfirmationLayout : WidgetBase, IControlInit
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

			buttonTransmit.Clicked += async delegate {
                buttonTransmit.Sensitive = false;

                await Task.Run(() => {
                    WalletSendLayout.SendInfo.TxResult = App.Instance.Node.Transmit(WalletSendLayout.Tx).Result;

	                if (WalletSendLayout.SendInfo.TxResult == BlockChain.BlockChain.TxResultEnum.Accepted && WalletSendLayout.SendInfo.NeedAutoTx)
					{
						var outputIdx = WalletSendLayout.Tx.outputs.ToList().FindIndex(t => t.@lock is Consensus.Types.OutputLock.ContractLock);
						var outpoint = new Types.Outpoint(Merkle.transactionHasher.Invoke(WalletSendLayout.Tx), (uint)outputIdx);

						byte[] witnessData = ContractUtilities.DataGenerator.makeMessage(
							WalletSendLayout.SendInfo.Json,
							outpoint);

						var autoTxResult = new ExecuteContractAction() { 
	                        ContractHash = WalletSendLayout.SendInfo.Destination.Bytes, 
	                        Message = witnessData 
	                    }.Publish().Result;

	                    WalletSendLayout.SendInfo.AutoTxCreated = autoTxResult.Item1;

	                    if (WalletSendLayout.SendInfo.AutoTxCreated)
	                    {
                            WalletSendLayout.SendInfo.AutoTxResult = App.Instance.Node.Transmit(autoTxResult.Item2).Result;
	                    }
					}
                });

                Gtk.Application.Invoke(delegate {
                    buttonTransmit.Sensitive = true;
                    UpdateStatus();
                });
			};
		}

		public void Init()
		{
            var assetName = App.Instance.AssetsMetadata.TryGetValue(WalletSendLayout.SendInfo.Asset);

		//	imageAsset.Pixbuf = ImagesCache.Instance.GetIcon(assetType.Image);
			labelSelectedAsset.Text = labelSelectedAsset1.Text = assetName;

			labelAmountValue.Text = WalletSendLayout.SendInfo.Amount.ToString();
			entryDestination.Text = WalletSendLayout.SendInfo.Destination.ToString();

			UpdateStatusInner(false);
		}

		void Back(object sender, EventArgs e)
		{
            FindParent<WalletLayout>().PrevPage(false);
		}

		void UpdateStatus()
		{
            var error = false;
            var message = string.Empty;
            var sendInfo = WalletSendLayout.SendInfo;
                
			if (sendInfo.Signed)
			{
				message += "Transaction signed successfully. ";
			}
			else
			{
				message += "Transaction signing error. ";
                error = true;
            }

            if (sendInfo.NeedAutoTx)
            {
                if (sendInfo.AutoTxCreated)
                {
                    message += "AutoTX created successfully. ";
                }
                else
                {
                    message += "Error creating AutoTX. ";
                    error = true;
                }
            }

            if (sendInfo.TxResult == BlockChain.BlockChain.TxResultEnum.Accepted)
            {
                message += "Transaction broadcasted successfully.";
            }
            else
            {
                message += "Transaction broadcasted failed, reason: " + WalletSendLayout.SendInfo.TxResult;
                error = true;
            }

            if (sendInfo.NeedAutoTx)
            {
                if (sendInfo.AutoTxResult == BlockChain.BlockChain.TxResultEnum.Accepted)
                {
                    message += "AutoTX broadcasted successfully.";
                }
                else
                {
                    message += "AutoTX broadcasted failed, reason: " + WalletSendLayout.SendInfo.AutoTxResult;
                    error = true;
                }
            }

            UpdateStatusInner(error, message);
		}

        void UpdateStatusInner(bool error, string message = null) 
        {
			labelStatus.Text = message;

			if (error)
				labelStatus.ModifyFg(StateType.Normal, Constants.Colors.Error.Gdk);
			else
				labelStatus.ModifyFg(StateType.Normal, Constants.Colors.Success.Gdk);
		}
	}
}
