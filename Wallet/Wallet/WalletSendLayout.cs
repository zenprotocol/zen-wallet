using System;
using System.Collections.Generic;
using Gdk;
using Gtk;
using Wallet.core;

namespace Wallet
{
	public class SendInfo
	{
		public bool Signed { get; set; }
		public BlockChain.BlockChain.TxResultEnum? Result { get; set; }

		public byte[] Asset
		{
			get; set;
		}

		public ulong Amount
		{
			get; set;
		}
	
		public byte[] Destination
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

		public SendInfo()
		{
			Signed = false;
			Result = null;
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

			entryDestination.Changed += (sender, e) =>
			{
				try
				{
					SendInfo.Destination = core.Data.Key.FromBase64String(((Entry)sender).Text);
					labelDestinationError.Text = "";
				}
				catch
				{
					SendInfo.Destination = null;
					labelDestinationError.Text = "Invalid address";
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
					try
					{
						SendInfo.Amount = ulong.Parse(text);
						CheckAssetAmount();
					}
					catch
					{
						SendInfo.Amount = 0;
						labelAmountError.Text = "Invalid amount";
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
			}, eventboxDestination, eventboxAsset, eventboxAmount);

			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.SubText.Gdk);
				label.ModifyFont(Constants.Fonts.ActionBarIntermediate);
			}, labelDestination, labelAsset, labelAmount, labelBalanceValue);

			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.SubText.Gdk);
				label.ModifyFont(Constants.Fonts.ActionBarSmall);
			}, labelSelectedAsset, labelSelectedAsset1, labelSelectOtherAsset, labelBalance);

			Apply((Entry entry) =>
			{
				entry.ModifyFg(StateType.Normal, Constants.Colors.Text2.Gdk);
				entry.ModifyFont(Constants.Fonts.ActionBarSmall);
			}, entryDestination);	

			buttonBack.Clicked += Back;

			var i = 0;
			int selectedIdx = 0;
			var keys = new Dictionary<string, byte[]>();
			foreach (var _asset in App.Instance.Wallet.AssetsMetadata)
			{
				keys[_asset.Value.Caption] = _asset.Key;

				if (WalletController.Instance.AssetType.Caption == _asset.Value.Caption)
				{
					SendInfo.Asset = _asset.Key;
					selectedIdx = i;
				}
				else
				{
					i++;
				}

				comboboxAsset.AppendText(_asset.Value.Caption);
			}

			TreeIter iter;
			comboboxAsset.Model.IterNthChild(out iter, selectedIdx);
			comboboxAsset.SetActiveIter(iter);

			comboboxAsset.Changed += (sender, e) =>
			{
				var comboBox = sender as Gtk.ComboBox;

				comboBox.GetActiveIter(out iter);
				var value = new GLib.Value();
				comboBox.Model.GetValue(iter, 0, ref value);

				SendInfo.Asset = keys[value.Val as string]; 
				var assetType = App.Instance.Wallet.AssetsMetadata[SendInfo.Asset];

				labelSelectedAsset.Text = labelSelectedAsset1.Text = assetType.Caption;
				imageAsset.Pixbuf = ImagesCache.Instance.GetIcon(assetType.Image);

				UpdateBalance();
			};

			UpdateBalance();

			buttonSignAndReview.Clicked += delegate {
				SendInfo.Result = null;

				SendInfo.Signed = App.Instance.Wallet.Sign(
					SendInfo.Destination,
					SendInfo.Asset,
					SendInfo.Amount,
					out _Tx
				);

				if (SendInfo.Signed)
				{
					FindParent<Notebook>().Page = 3;
					FindParent<WalletLayout>().FindChild<WalletSendConfirmationLayout>().Init();
				}
				else
				{
					labelAmountError.Text = "Error: not enough " + App.Instance.Wallet.AssetsMetadata[SendInfo.Asset];
				}
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
			_AssetBalance = _AssetDeltas == null || !_AssetDeltas.ContainsKey(SendInfo.Asset) ? 0 : (ulong) _AssetDeltas[SendInfo.Asset];		
			labelBalanceValue.Text = $"{_AssetBalance} {App.Instance.Wallet.AssetsMetadata[SendInfo.Asset]}";
			CheckAssetAmount();
		}

		void CheckAssetAmount()
		{
			SendInfo.HasEnough = SendInfo.Amount <= _AssetBalance;

			if (!SendInfo.HasEnough)
			{
				labelAmountError.Text = "Not enough " + App.Instance.Wallet.AssetsMetadata[SendInfo.Asset];
			}
			else
			{
				labelAmountError.Text = "";
			}
		}
	}
}
