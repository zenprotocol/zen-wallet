using System;
using System.Collections.Generic;
using Gdk;
using Gtk;
using System.Linq;
using Wallet.core;
using Wallet.core.Data;
using Consensus;
using Wallet.Constants;

namespace Wallet
{
	public class SendInfo
	{
		public bool Signed { get; set; }
		public bool AutoTxCreated { get; set; }
        public bool NeedAutoTx = false;
        public byte[] WitnessData { get; set; }

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

		public string Data
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
				return Amount > 0 && Destination != null && Asset != null && HasEnough;
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
		ulong _AssetBalance = 0;

		public WalletSendLayout()
		{
			this.Build();
			SendInfo = new SendInfo();

			_Tx = null;
			buttonSignAndReview.Sensitive = false;

			buttonPaste.Clicked += delegate {
				var clipboard = Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
				var target = Gdk.Atom.Intern("text/plain", true);
				var selection = clipboard.WaitForContents(target);

				if (selection != null)
				{
					entryDestination.Text = System.Text.Encoding.UTF8.GetString(selection.Data, 0, selection.Data.Length);
				}
			};

			buttonPasteData.Clicked += delegate
			{
				var clipboard = Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
				var target = Gdk.Atom.Intern("text/plain", true);
				var selection = clipboard.WaitForContents(target);

				if (selection != null)
				{
					entryData.Text = System.Text.Encoding.UTF8.GetString(selection.Data, 0, selection.Data.Length);
				}
			};

			vboxData.Visible = false; // just hide the f@cking thing already
			vboxData.Hide(); // just hide the f@cking thing already

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
                            vboxData.Visible = true;
                            break;
                        case AddressType.PK:
                            vboxData.Visible = false;
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

            entryData.Changed += (sender, e) =>
            {
                var text = ((Entry)sender).Text;

                SendInfo.Data = text;
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
			}, labelDestinationError, labelAmountError);

			Apply((EventBox eventbox) =>
			{
				eventbox.ModifyBg(StateType.Normal, Constants.Colors.Textbox.Gdk);
            }, eventboxDestination, eventboxData, eventboxAsset, eventboxAmount);

			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.SubText.Gdk);
				label.ModifyFont(Constants.Fonts.ActionBarIntermediate);
			}, labelDestination, labelData, labelAsset, labelAmount, labelBalanceValue);

			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.SubText.Gdk);
				label.ModifyFont(Constants.Fonts.ActionBarSmall);
			}, labelSelectedAsset, labelSelectedAsset1, labelSelectOtherAsset, labelBalance);

			Apply((Entry entry) =>
			{
				entry.ModifyFg(StateType.Normal, Constants.Colors.Text2.Gdk);
				entry.ModifyFont(Constants.Fonts.ActionBarSmall);
            }, entryDestination, entryData);	

			buttonBack.Clicked += Back;

			var comboboxStore = new ListStore(typeof(byte[]), typeof(string));

			var i = 0;
			int selectedIdx = 0;

			comboboxAsset.Model = comboboxStore;
			var textRenderer = new CellRendererText();
			comboboxAsset.PackStart(textRenderer, false);
			comboboxAsset.AddAttribute(textRenderer, "text", 1);

			foreach (var _asset in App.Instance.Wallet.AssetsMetadata.Keys)
			{
				if (_asset.SequenceEqual(WalletController.Instance.Asset))
				{
					selectedIdx = i;
				}
				else
				{
					i++;
				}

				var _iter = comboboxStore.AppendValues(_asset, Convert.ToBase64String(_asset));
				App.Instance.Wallet.AssetsMetadata.Get(_asset).ContinueWith(t =>
				{
					Gtk.Application.Invoke(delegate
					{
						comboboxStore.SetValue(_iter, 1, t.Result);
					});
				});
			}
			TreeIter iter;
			comboboxAsset.Model.IterNthChild(out iter, selectedIdx);
			comboboxAsset.SetActiveIter(iter);

			comboboxAsset.Changed += async (sender, e) =>
			{
				var comboBox = sender as Gtk.ComboBox;

				comboBox.GetActiveIter(out iter);
				var value = new GLib.Value();
				comboBox.Model.GetValue(iter, 0, ref value);
                byte[] _asset = value.Val as byte[];
				SendInfo.Asset = _asset; 

                var assetName = await App.Instance.Wallet.AssetsMetadata.Get(SendInfo.Asset);
				labelSelectedAsset.Text = labelSelectedAsset1.Text = assetName;
			//	imageAsset.Pixbuf = ImagesCache.Instance.GetIcon(assetType.Image);

				UpdateBalance();
			};

			UpdateBalance();

			buttonSignAndReview.Clicked += delegate {
                SendInfo.Reset();

                switch (SendInfo.Destination.AddressType)
                {
                    case AddressType.Contract:
						var parts = SendInfo.Data.Split(null);

                        SendInfo.Destination.Data = Convert.FromBase64String(parts[0]);

                        switch (parts.Length) {
                            case 1:
								break;
                            case 2:
                                SendInfo.WitnessData = Convert.FromBase64String(parts[1]);
                                SendInfo.NeedAutoTx = true;
                                break;
                        }
                        break;
                    case AddressType.PK:
                        break;
                }

                SendInfo.Signed = App.Instance.Wallet.Sign(
                    SendInfo.Destination,
                    SendInfo.Asset,
                    SendInfo.Amount,
                    out _Tx
                );

				if (!SendInfo.Signed)
				{
                    labelAmountError.Text = "Error: not enough " + App.Instance.Wallet.AssetsMetadata.Get(SendInfo.Asset).Result;
					return;
				}

                FindParent<Notebook>().Page = 3;
                FindParent<WalletLayout>().FindChild<WalletSendConfirmationLayout>().Init();
			};

			PortfolioController.Instance.AddVIew(this);
		}

		void Back(object sender, EventArgs e)
		{
			FindParent<Notebook>().Page = 0;
		}

		public void Clear()
		{

		}

		public void SetDeltas(AssetDeltas assetDeltas)
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

			_AssetBalance = _AssetDeltas == null || !_AssetDeltas.ContainsKey(SendInfo.Asset) ? 0 : (ulong) _AssetDeltas[SendInfo.Asset];

			string value;

			if (SendInfo.Asset.SequenceEqual(Tests.zhash))
			{
				value = new Zen(_AssetBalance).ToString();
			}
			else
			{
				value = String.Format(Formats.Money, _AssetBalance);
			}

            labelBalanceValue.Text = $"{value} {App.Instance.Wallet.AssetsMetadata.Get(SendInfo.Asset).Result}";
			CheckAssetAmount();
		}

		void CheckAssetAmount()
		{
			SendInfo.HasEnough = SendInfo.Amount <= _AssetBalance;

			if (!SendInfo.HasEnough)
			{
                labelAmountError.Text = "Not enough " + App.Instance.Wallet.AssetsMetadata.Get(SendInfo.Asset).Result;
			}
			else
			{
				labelAmountError.Text = "";
			}
		}
	}
}
