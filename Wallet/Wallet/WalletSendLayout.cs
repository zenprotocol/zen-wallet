﻿using System;
using System.Collections.Generic;
using Gdk;
using Gtk;
using System.Linq;
using Wallet.core;
using Wallet.core.Data;
using Consensus;
using Wallet.Constants;
using Newtonsoft.Json.Linq;
using Wallet;
using System.Threading.Tasks;
using BlockChain.Data;

namespace Wallet
{
    //public class WitnessData
    //{
    //    public byte[] Initial { get; set; }
    //    public byte[] Final { get; set; }
    //}

    public class SendInfo
    {
        public bool Signed { get; set; }
        public bool AutoTxCreated { get; set; }
        public bool NeedAutoTx = false;
		//public WitnessData WitnessData { get; set; }
	    public string Json { get; set; }

		public BlockChain.BlockChain.TxResultEnum? TxResult { get; set; }
        public BlockChain.BlockChain.TxResultEnum? AutoTxResult { get; set; }

        public byte[] Asset
        {
            get; set;
        }

        public ulong Amount
        {
            get; set;
        }
    
        public Address Destination
        {
            get; set;
        }

        public JObject Data
        {
            get; set;
        }

        public bool DataValid
        {
            get; set;
        }

        public bool HasEnough
        {
            get; set;
        }

        public bool Valid
        {
            get
            {
                return Amount > 0 && Destination != null && Asset != null && HasEnough && (Data == null || DataValid);
            }
        }

        public void Reset()
        {
            Signed = false;
            NeedAutoTx = false;
            TxResult = null;
            AutoTxResult = null;
            Json = null;
        }
    }

    [System.ComponentModel.ToolboxItem(true)]
    public partial class WalletSendLayout : WidgetBase, IPortfolioVIew, IAssetsView, IControlInit
    {
        readonly AssetsController _AssetsController;
        readonly DeltasController _DeltasController;

        public static SendInfo SendInfo
        {
            get; private set;
        }

        static Consensus.Types.Transaction _Tx;
        public static Consensus.Types.Transaction Tx
        {
            get { return _Tx; }
        }

        byte[] _CurrentAsset;
        UpdatingStore<byte[]> _AssetsStore = new UpdatingStore<byte[]>(0, typeof(byte[]), typeof(string));

        public ICollection<AssetMetadata> Assets 
        { 
            set 
            {
                foreach (var _asset in value)
                {
                    var iter = _AssetsStore.AppendValues(_asset.Asset, _asset.Display);

                    if (_CurrentAsset != null && _asset.Asset.SequenceEqual(_CurrentAsset))
                    {
                        comboboxAsset.SetActiveIter(iter);
                    }

                }
            } 
        }

        public AssetMetadata AssetUpdated
        {
            set
            {
                _AssetsStore.Upsert(t => t.SequenceEqual(value.Asset), value.Asset, value.Display);

                if (_CurrentAsset != null && _CurrentAsset.SequenceEqual(value.Asset))
                {

                }
            }
        }

        AssetDeltas _AssetDeltas = null;
        long _AssetBalance = 0;

        public WalletSendLayout()
        {
            this.Build();

            SendInfo = new SendInfo();

            _AssetsController = new AssetsController(this);
            _DeltasController = new DeltasController(this);

            vboxDataPaste.ModifyFg(StateType.Normal, Constants.Colors.DialogBackground.Gdk);
            eventboxSeperator.ModifyBg(StateType.Normal, Constants.Colors.Seperator.Gdk);
            eventboxSeperator1.ModifyBg(StateType.Normal, Constants.Colors.Seperator.Gdk);

            entryAmount.ModifyFg(StateType.Normal, Constants.Colors.Text2.Gdk);
            entryAmount.ModifyFont(Constants.Fonts.ActionBarSmall);

            labelBalance.ModifyFg(StateType.Normal, Constants.Colors.TextBlue.Gdk);
            labelBalance.ModifyFont(Constants.Fonts.LogBig);

            labelHeader.ModifyFg(StateType.Normal, Constants.Colors.TextHeader.Gdk);
            labelHeader.ModifyFont(Constants.Fonts.ActionBarBig);

            //error labels
            Apply((Label label) =>
            {
                label.ModifyFg(StateType.Normal, Constants.Colors.Error.Gdk);
                label.ModifyFont(Constants.Fonts.ActionBarSmall);
            }, labelDestinationError, labelDataError, labelAmountError);

            //labels
            Apply((Label label) =>
            {
                label.ModifyFg(StateType.Normal, Constants.Colors.LabelText.Gdk);
                label.ModifyFont(Constants.Fonts.ActionBarIntermediate);
            }, labelDestination, labelData, labelAmount, labelSelectAsset, labelBalanceHeader);

            //entries
            Apply((Entry entry) =>
            {
                entry.ModifyBg(StateType.Normal, Constants.Colors.Seperator.Gdk);
				entry.ModifyText(StateType.Normal, Constants.Colors.Text.Gdk);
                entry.ModifyFont(Constants.Fonts.ActionBarSmall);
                entry.ModifyBase(StateType.Normal, Constants.Colors.ButtonUnselected.Gdk);
            }, entryDestination, entryAmount);

            txtData.ModifyFg(StateType.Normal, Constants.Colors.Text2.Gdk);
            txtData.ModifyFont(Constants.Fonts.ActionBarSmall);
			txtData.ModifyBase(StateType.Normal, Constants.Colors.ButtonUnselected.Gdk);
			txtData.ModifyText(StateType.Normal, Constants.Colors.Text.Gdk);


			_Tx = null;

            eventboxPasteAddress.ButtonPressEvent += delegate
            {
                try
                {
                    var clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", true));
                    entryDestination.Text = clipboard.WaitForText();
                }
                catch { }
            };

            eventboxPasterData.ButtonPressEvent += delegate
            {
                try
                {
                    var clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", true));
                    txtData.Buffer.Text = clipboard.WaitForText();
                }
                catch { }
            };

            vboxMainInner.Remove(vboxData);

            entryDestination.Changed += (sender, e) =>
            {
                try
                {
                    var value = ((Entry)sender).Text;

                    var address = string.IsNullOrEmpty(value) ? null : new Address(value);
                    SendInfo.Destination = address;
                    labelDestinationError.Text = "";

                    vboxMainInner.Remove(vboxData);

                    if (address != null && address.AddressType == AddressType.Contract)
                    {
                        vboxMainInner.Add(vboxData);
                        vboxMainInner.ReorderChild(vboxData, 2);
                    }
                }
                catch
                {
                    SendInfo.Destination = null;
                    labelDestinationError.Text = "Invalid address";
                }

                UpdateButtons();
            };

            txtData.Buffer.Changed += (sender, e) =>
            {
                var text = ((TextBuffer)sender).Text;

                if (string.IsNullOrWhiteSpace(text) || text.Trim().Length == 0)
                {
                    SendInfo.Data = null;
                    SendInfo.DataValid = true;
                    labelDataError.Text = "";
                }
                else
                {
                    try
                    {
                        SendInfo.Data = JObject.Parse(text); //TODO: deeper validation
                        SendInfo.DataValid = true;
                        labelDataError.Text = "";
                    }
                    catch
                    {
                        SendInfo.Data = null;
                        SendInfo.DataValid = false;
                        labelDataError.Text = "Invalid data";
                    }
                }

                UpdateButtons();
            };

            entryAmount.Changed += (sender, e) =>
            {
                var text = ((Entry)sender).Text;

                if (text.Trim().Length == 0)
                {
                    SendInfo.Amount = 0;
                    labelAmountError.Text = "";
                }
                else
                {
                    Zen zen;
                    if (!Zen.IsValidText(text, out zen))
                    {
                        SendInfo.Amount = 0;
                        labelAmountError.Text = "Invalid amount";
                    }
                    else
                    {
                        SendInfo.Amount = zen.Kalapas;
                        CheckAssetAmount();
                    }
                }

                UpdateButtons();
            };

            eventboxBack.ButtonPressEvent += Back;

            comboboxAsset.Model = _AssetsStore;
            var textRenderer = new CellRendererText();
            comboboxAsset.PackStart(textRenderer, false);
            comboboxAsset.AddAttribute(textRenderer, "text", 1);

			comboboxAsset.Changed += (sender, e) =>
            {
                var comboBox = sender as Gtk.ComboBox;
                TreeIter iter;

                if (comboBox.GetActiveIter(out iter))
                {
                    var value = new GLib.Value();
                    comboBox.Model.GetValue(iter, 0, ref value);
                    byte[] asset = value.Val as byte[];
                    SendInfo.Asset = asset;
                }
                else
                {
                    SendInfo.Asset = null;
                }

               // var assetMatadataList = App.Instance.AssetsMetadata.GetAssetMatadataList().Where(t => t.Asset.SequenceEqual(SendInfo.Asset));

    //            if (assetMatadataList.Count() != 0)
    //            {
                //    labelSelectedAsset.Text = assetMatadataList.First().Display;
                //    labelSelectedAsset1.Text = assetMatadataList.First().Display;
                //}

                UpdateBalance();
            };

            TreeIter iterDefault;
            if (_AssetsStore.Find(t => t.SequenceEqual(Consensus.Tests.zhash), out iterDefault))
                comboboxAsset.SetActiveIter(iterDefault);

            UpdateBalance();

            eventboxSend.ButtonPressEvent += async delegate {
                SendInfo.Reset();
				UpdateStatus("");
				HideButtons();

                if (SendInfo.Destination.AddressType == AddressType.Contract && SendInfo.Data != null)
                {
                    try
                    {
                        var data = SendInfo.Data;

                        byte[] firstData = null;

                        if (data["first"] is JObject)
                        {
                            byte[] signature;

                            var pubkey = Convert.FromBase64String(data["first"]["pubkey"].Value<string>());
                            var toSign = Convert.FromBase64String(data["first"]["toSign"].Value<string>());

                            //TODO: run on non-ui thread?
                            if (!App.Instance.Wallet.SignData(pubkey, toSign, out signature))
                            {
                                labelDataError.Text = "Could not sign data";
                                UpdateButtons();
                                return;
                            }

                            byte[] _data = Convert.FromBase64String(data["first"]["data"].Value<string>());

                            firstData = _data.Concat(signature).ToArray();
                        }
                        else
                        {
                            firstData = Convert.FromBase64String(data["first"].ToString());
                        }

                        SendInfo.Destination.Data = firstData;

                        if (data["second"] is JObject)
                        {
                            SendInfo.Json = data.ToString();
                            SendInfo.NeedAutoTx = true;
                        }
                    } catch {
                        labelDataError.Text = "Could not parse data";
                        UpdateButtons();
                        return;
                    }
                }

                _Tx = await Task.Run(() => App.Instance.Wallet.Sign(
                    SendInfo.Destination,
                    SendInfo.Asset,
                    SendInfo.Amount
                ));

                SendInfo.Signed = _Tx != null;

                if (!SendInfo.Signed)
                {
                    Gtk.Application.Invoke(delegate
	                {
                        UpdateStatus("Could not sign transaction");
	                });
                    return;
                }

				await Task.Run(() =>
				{
                    try
                    {
                        WalletSendLayout.SendInfo.TxResult = App.Instance.Node.Transmit(WalletSendLayout.Tx).Result;

                        if (WalletSendLayout.SendInfo.TxResult != BlockChain.BlockChain.TxResultEnum.Accepted)
                        {
                            Gtk.Application.Invoke(delegate
                            {
                                UpdateStatus($"Could not broadcast transaction ({WalletSendLayout.SendInfo.TxResult})");
                            });
                        }
                        else if (!WalletSendLayout.SendInfo.NeedAutoTx)
                        {
                            Gtk.Application.Invoke(delegate
                            {
                                UpdateStatus("Transaction transmitted", true);
                            });
                        }
                        else
                        {
                            var outputIdx = WalletSendLayout.Tx.outputs.ToList().FindIndex(t => t.@lock is Consensus.Types.OutputLock.ContractLock);
                            var outpoint = new Types.Outpoint(Merkle.transactionHasher.Invoke(WalletSendLayout.Tx), (uint)outputIdx);

                            byte[] witnessData = ContractUtilities.DataGenerator.makeMessage(
                                WalletSendLayout.SendInfo.Json,
                                outpoint);

                            var autoTxResult = new ExecuteContractAction()
                            {
                                ContractHash = WalletSendLayout.SendInfo.Destination.Bytes,
                                Message = witnessData
                            }.Publish().Result;

                            WalletSendLayout.SendInfo.AutoTxCreated = autoTxResult.Item1;

                            if (WalletSendLayout.SendInfo.AutoTxCreated)
                            {
                                WalletSendLayout.SendInfo.AutoTxResult = App.Instance.Node.Transmit(autoTxResult.Item2).Result;

                                if (WalletSendLayout.SendInfo.AutoTxResult == BlockChain.BlockChain.TxResultEnum.Accepted)
                                {
                                    Gtk.Application.Invoke(delegate
                                    {
                                        UpdateStatus("Transmitted", true);
                                    });
                                }
                                else
                                {
                                    Gtk.Application.Invoke(delegate
                                    {
                                        UpdateStatus($"Could not broadcast auto transaction ({WalletSendLayout.SendInfo.AutoTxResult})");
                                    });
                                }
                            }
                            else
                            {
                                Gtk.Application.Invoke(delegate
                                {
                                    UpdateStatus($"Could not execute contract");
                                });
                            }
                        }
					}
					catch
					{
						Gtk.Application.Invoke(delegate
						{
							UpdateStatus("Error sending message to contract");
						});
					}
				});
			};
        }

        void Back(object sender, EventArgs e)
        {
            FindParent<WalletLayout>().SetPage(0);
        }

        public AssetDeltas PortfolioDeltas
        {
            set
            {
                _AssetDeltas = value;
                UpdateBalance();
            }
        }

        void UpdateBalance()
        {
            if (SendInfo.Asset == null)
            {
                SendInfo.Asset = Consensus.Tests.zhash;
            }

            _AssetBalance = _AssetDeltas != null && _AssetDeltas.ContainsKey(SendInfo.Asset) ? _AssetDeltas[SendInfo.Asset] : 0;

            string value = SendInfo.Asset.SequenceEqual(Tests.zhash) ? new Zen(_AssetBalance).ToString() : String.Format(Formats.Money, _AssetBalance);

            labelBalance.Text = string.Format(Constants.Formats.Money, value);
            CheckAssetAmount();
        }

        void CheckAssetAmount()
        {
            SendInfo.HasEnough = SendInfo.Amount <= (ulong)_AssetBalance && _AssetBalance > 0;

            if (!SendInfo.HasEnough)
            {
                labelAmountError.Text = "Not enough " + App.Instance.AssetsMetadata.TryGetValue(SendInfo.Asset);
            }
            else
            {
                labelAmountError.Text = "";
            }

            UpdateButtons();
        }

        public void Init()
        {
            entryDestination.Text = "";
            txtData.Buffer.Text = "";
            entryAmount.Text = "";

            labelDestinationError.Text = "";
            labelDataError.Text = "";

            vboxMainInner.Remove(vboxData);

            UpdateStatus("");

            TreeIter storeIter;
            var canIter = _AssetsStore.GetIterFirst(out storeIter);

            while (canIter)
            {
                var value = new GLib.Value();
                _AssetsStore.GetValue(storeIter, 0, ref value);
                var asset = value.Val as byte[];

                if (asset.SequenceEqual(Tests.zhash))
                    comboboxAsset.SetActiveIter(storeIter);
                
                canIter = _AssetsStore.IterNext(ref storeIter);
            }
        }

        void HideButtons()
        {
			if (hboxSignAndReview.Children.Contains(eventboxSend))
				hboxSignAndReview.Remove(eventboxSend);
			if (!hboxSignAndReview.Children.Contains(imageSignAndReviewDisabled))
				hboxSignAndReview.Add(imageSignAndReviewDisabled);
        }

        void UpdateButtons()
        {
            if (SendInfo.Valid)
            {
                if (hboxSignAndReview.Children.Contains(imageSignAndReviewDisabled))
                    hboxSignAndReview.Remove(imageSignAndReviewDisabled);
				if (!hboxSignAndReview.Children.Contains(eventboxSend))
					hboxSignAndReview.Add(eventboxSend);
            }
            else
            {
                if (hboxSignAndReview.Children.Contains(eventboxSend))
                    hboxSignAndReview.Remove(eventboxSend);
                if (!hboxSignAndReview.Children.Contains(imageSignAndReviewDisabled))
                    hboxSignAndReview.Add(imageSignAndReviewDisabled);
            }
        }

        void UpdateStatus(string status, bool isOk = false)
        {
			labelStatus.Text = status;
            labelStatus.ModifyFg(StateType.Normal, isOk ? Constants.Colors.TextBlue.Gdk : Constants.Colors.Error.Gdk);
            UpdateButtons();
		}
    }
}
