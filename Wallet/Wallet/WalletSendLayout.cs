﻿using System;
using System.Collections.Generic;
using Gdk;
using Gtk;
using System.Linq;
using Wallet.core;
using Wallet.core.Data;
using Consensus;
using Wallet.Constants;
using Newtonsoft.Json.Converters;
using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Wallet
{
    public class WitnessData
    {
		public byte[] Initial { get; set; }
		public byte[] Final { get; set; }
	}

	public class SendInfo
	{
		public bool Signed { get; set; }
		public bool AutoTxCreated { get; set; }
        public bool NeedAutoTx = false;
        public WitnessData WitnessData { get; set; }

		public BlockChain.BlockChain.TxResultEnum? TxResult { get; set; }
		public BlockChain.BlockChain.TxResultEnum? AutoTxResult { get; set; }

		public byte[] Asset
		{
			get; set;
		}

		public byte[] SecureToken
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
            WitnessData = null;
		}
	}

	[System.ComponentModel.ToolboxItem(true)]
	public partial class WalletSendLayout : WidgetBase, IPortfolioVIew
	{
		public static SendInfo SendInfo
		{
			get; private set;
		}

		static Consensus.Types.Transaction _Tx;
		public static Consensus.Types.Transaction Tx
		{
			get { return _Tx; }
		}

		AssetDeltas _AssetDeltas = null;
		long _AssetBalance = 0;

		public WalletSendLayout()
		{
			this.Build();
			SendInfo = new SendInfo();

			_Tx = null;
			buttonSignAndReview.Sensitive = false;

			buttonPaste.Clicked += delegate
			{
				try
				{
					var clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", true));
					entryDestination.Text = clipboard.WaitForText();
				}
				catch { }
			};

			buttonPasteData.Clicked += delegate
			{
				try
				{
					var clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", true));
					txtData.Buffer.Text = clipboard.WaitForText();
				}
				catch { }
			};

			vboxMainInner.Remove(eventboxData);
			vboxMainInner.Remove(eventboxSecureToken);

			entryDestination.Changed += (sender, e) =>
			{
				try
				{
					var address = new Address(((Entry)sender).Text);
					SendInfo.Destination = address;
					labelDestinationError.Text = "";

					switch (address.AddressType)
					{
						case AddressType.Contract:
                            //vboxMainInner.Add(eventboxSecureToken);
							//vboxMainInner.ReorderChild(eventboxSecureToken, 1);

							vboxMainInner.Add(eventboxData);
							vboxMainInner.ReorderChild(eventboxData, 1);

							break;
						case AddressType.PK:
							vboxMainInner.Remove(eventboxData);
							vboxMainInner.Remove(eventboxSecureToken);
							break;
					}
				}
				catch
				{
					SendInfo.Destination = null;
					labelDestinationError.Text = "Invalid address";
				}

				buttonSignAndReview.Sensitive = SendInfo.Valid;
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

				buttonSignAndReview.Sensitive = SendInfo.Valid;
            };

			entryAmount.ModifyFg(StateType.Normal, Constants.Colors.Text2.Gdk);
			entryAmount.ModifyFont(Constants.Fonts.ActionBarSmall);
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

				buttonSignAndReview.Sensitive = SendInfo.Valid;
			};

            Apply((Label label) =>
            {
                label.ModifyFg(StateType.Normal, Constants.Colors.Error.Gdk);
                label.ModifyFont(Constants.Fonts.ActionBarSmall);
            }, labelDestinationError, labelDataError, labelAmountError);

			Apply((EventBox eventbox) =>
			{
				eventbox.ModifyBg(StateType.Normal, Constants.Colors.Textbox.Gdk);
            }, eventboxDestination, eventboxData, eventboxSecureToken, eventboxAsset, eventboxAmount);

			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.SubText.Gdk);
				label.ModifyFont(Constants.Fonts.ActionBarIntermediate);
			}, labelDestination, labelData, labelAsset, labelSecureToken, labelAmount, labelBalanceValue);

			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.SubText.Gdk);
				label.ModifyFont(Constants.Fonts.ActionBarSmall);
            }, labelSelectedAsset, labelSelectedAsset1, labelSelectOtherAsset, labelSelectSecureToken, labelBalance);

			Apply((Entry entry) =>
			{
				entry.ModifyFg(StateType.Normal, Constants.Colors.Text2.Gdk);
				entry.ModifyFont(Constants.Fonts.ActionBarSmall);
            }, entryDestination);

            txtData.ModifyFg(StateType.Normal, Constants.Colors.Text2.Gdk);
            txtData.ModifyFont(Constants.Fonts.ActionBarSmall);

			buttonBack.Clicked += Back;

			var comboboxStore = new ListStore(typeof(byte[]), typeof(string));

			comboboxAsset.Model = comboboxStore;
			var textRenderer = new CellRendererText();
			comboboxAsset.PackStart(textRenderer, false);
			comboboxAsset.AddAttribute(textRenderer, "text", 1);

			var secureTokenComboboxStore = new ListStore(typeof(byte[]), typeof(string));

			comboboxSecureToken.Model = secureTokenComboboxStore;
			var textRendererSecukreToken = new CellRendererText();
			comboboxSecureToken.PackStart(textRendererSecukreToken, false);
			comboboxSecureToken.AddAttribute(textRendererSecukreToken, "text", 1);

            secureTokenComboboxStore.AppendValues(new byte[] {}, "None");

			foreach (var _asset in App.Instance.Wallet.AssetsMetadata.GetAssetMatadataList())
            {
                var iter = comboboxStore.AppendValues(_asset.Asset, _asset.Display);
				
                if (_asset.Asset.SequenceEqual(WalletController.Instance.Asset))
                {
                    comboboxAsset.SetActiveIter(iter);
                }

                secureTokenComboboxStore.AppendValues(_asset.Asset, _asset.Display);
            }

            App.Instance.Wallet.AssetsMetadata.AssetMatadataChanged += t =>
            {
				Gtk.Application.Invoke(delegate
				{
					try
					{
		                TreeIter iter;
		                comboboxStore.GetIterFirst(out iter);
						bool found = false;

						do
		                {
		                    var key = new GLib.Value();
		                    comboboxStore.GetValue(iter, 0, ref key);
		                    byte[] _asset = key.Val as byte[];

		                    if (_asset != null && _asset.SequenceEqual(t.Asset))
		                    {
		                        comboboxStore.SetValue(iter, 1, t.Display);
		                        found = true;
		                        break;
		                    }
						} while (comboboxStore.IterNext(ref iter));

						if (!found)
						{
							comboboxStore.AppendValues(t.Asset, t.Display);
						}

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

		                if (labelSelectedAsset.Text == Convert.ToBase64String(t.Asset))
		                {
		                    labelSelectedAsset.Text = t.Display;
		            	}

						if (labelSelectedAsset1.Text == Convert.ToBase64String(t.Asset))
						{
							labelSelectedAsset1.Text = t.Display;
						}
					}
					catch
					{
						Console.WriteLine("Exception in portfolio AssetMatadataChanged handler");
					}
				});
            };

			comboboxAsset.Changed += (sender, e) =>
			{
				var comboBox = sender as Gtk.ComboBox;
                TreeIter iter;
				comboBox.GetActiveIter(out iter);
				var value = new GLib.Value();
				comboBox.Model.GetValue(iter, 0, ref value);
                byte[] asset = value.Val as byte[];
				SendInfo.Asset = asset;

                var assetMatadataList = App.Instance.Wallet.AssetsMetadata.GetAssetMatadataList().Where(t => t.Asset.SequenceEqual(asset));

                if (assetMatadataList.Count() != 0)
                {
					labelSelectedAsset.Text = assetMatadataList.First().Display;
					labelSelectedAsset1.Text = assetMatadataList.First().Display;
				}

				UpdateBalance();
			};

            comboboxSecureToken.Changed += (sender, e) =>
			{
			    var comboBox = sender as Gtk.ComboBox;
			    TreeIter iter;
			    comboBox.GetActiveIter(out iter);
			    var value = new GLib.Value();
			    comboBox.Model.GetValue(iter, 0, ref value);
			    byte[] asset = value.Val as byte[];
                SendInfo.SecureToken = asset;
			};

			UpdateBalance();

			buttonSignAndReview.Clicked += delegate {
                SendInfo.Reset();

                if (SendInfo.Destination.AddressType == AddressType.Contract && SendInfo.Data != null)
                {
                    try
                    {
                        var data = SendInfo.Data;

                        byte[] firstData = null;

                        if (data["first"] is JObject)
                        {
                            byte[] signature;

                            var pubkey = Convert.FromBase64String(data["first"]["pubkey"].ToString());
                            var toSign = Convert.FromBase64String(data["first"]["toSign"].ToString());

                            if (!App.Instance.Wallet.SignData(pubkey, toSign, out signature))
                            {
                                labelDataError.Text = "Could not sign data";
                                return;
                            }

                            byte[] _data = Convert.FromBase64String(data["first"]["data"].ToString());

                            firstData = _data.Concat(signature).ToArray();
                        }
                        else
                        {
                            firstData = Convert.FromBase64String(data["first"].ToString());
                        }

                        SendInfo.Destination.Data = firstData;

                        if (data["second"] is JObject)
                        {
                            SendInfo.WitnessData = new WitnessData()
                            {
                                Initial = Convert.FromBase64String(data["second"]["initial"].ToString()),
                                Final = Convert.FromBase64String(data["second"]["final"].ToString())
                            };

                            SendInfo.NeedAutoTx = true;
                        }
                    } catch {
                        labelDataError.Text = "Could not parse data";
                        return;
                    }
                }

                SendInfo.Signed = App.Instance.Wallet.Sign(
                    SendInfo.Destination,
                    SendInfo.Asset,
                    SendInfo.Amount,
                    out _Tx
                );

				if (!SendInfo.Signed)
				{
                    labelAmountError.Text = "Error: not enough " + App.Instance.Wallet.AssetsMetadata.GetMetadata(SendInfo.Asset).Result;
					return;
				}

                FindParent<Notebook>().Page = 3;
                FindParent<WalletLayout>().FindChild<WalletSendConfirmationLayout>().Init();
			};

            //Assets' images not implemented, remove ui elements
            hboxAsset.Remove(hboxAsset.Children[0]);

			PortfolioController.Instance.AddVIew(this);
		}

		void Back(object sender, EventArgs e)
		{
			FindParent<Notebook>().Page = 0;
		}

		public void Clear()
		{

		}

        public void SetPortfolioDeltas(AssetDeltas assetDeltas)
		{
			_AssetDeltas = assetDeltas;
			UpdateBalance();
		}

		void UpdateBalance()
		{
            if (SendInfo.Asset == null)
            {
                SendInfo.Asset = Consensus.Tests.zhash;
            }

            _AssetBalance = _AssetDeltas != null && _AssetDeltas.ContainsKey(SendInfo.Asset) ? _AssetDeltas[SendInfo.Asset] : 0;

			string value = SendInfo.Asset.SequenceEqual(Tests.zhash) ? new Zen(_AssetBalance).ToString() : String.Format(Formats.Money, _AssetBalance);

            labelBalanceValue.Text = $"{value} {App.Instance.Wallet.AssetsMetadata.GetMetadata(SendInfo.Asset).Result}";
			CheckAssetAmount();
		}

		void CheckAssetAmount()
		{
			SendInfo.HasEnough = SendInfo.Amount <= (ulong)_AssetBalance && _AssetBalance > 0;

			if (!SendInfo.HasEnough)
			{
                labelAmountError.Text = "Not enough " + App.Instance.Wallet.AssetsMetadata.GetMetadata(SendInfo.Asset).Result;
			}
			else
			{
				labelAmountError.Text = "";
			}
		}
	}
}
