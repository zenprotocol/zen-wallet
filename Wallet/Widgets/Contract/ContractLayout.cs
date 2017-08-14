using System;
using System.Collections.Generic;
using Gtk;
using Wallet.core;
using System.Linq;
using Consensus;

namespace Wallet.Widgets.Contract
{
	public interface IContractView
	{
		//Boolean IsActive { set; }
		byte[] Hash { set; }
		//ulong Tokens { set; }
		String Code { get; set; }
		//String Assertion { set; }
		bool HasEnoughZen { set; }
        decimal CostPerBlock { set; }
        ulong CostTotal { get; set; }
	}

    [System.ComponentModel.ToolboxItem(true)]
    public partial class ContractLayout : WidgetBase, IContractView, IAssetsView, IPortfolioVIew, IControlInit
    {
		ContractController _ContractController;

		UpdatingStore<byte[]> _SecureTokenComboboxStore = new UpdatingStore<byte[]>(
			0,
			typeof(byte[]),
			typeof(string)
		);
		
		// public bool IsActive { get; set; }
		public byte[] Hash { set { txtHash.Text = Convert.ToBase64String(value); } }
        public string Code { get { return txtCode.Buffer.Text; } set { txtCode.Buffer.Text = value; } }
        public bool HasEnoughZen { get; set; }

        byte[] SecureToken;

        decimal _costPerBlock;
        public decimal CostPerBlock
        {
            set 
            {
                _costPerBlock = value;

				labelCostPerBlock.Text = value.ToString();
			}
            get
            {
                return _costPerBlock;
            }
		}

        ulong _costTotal;
		public ulong CostTotal
        {
            set
            {
                _costTotal = value;
                labelTotalCost.Text = value.ToString();
            }
            get
            {
                return _costTotal;
            }
        }

		DeltasController _DeltasController;

		public ContractLayout()
        {
            this.Build();

            new AssetsController(this);
            _ContractController = new ContractController(this);
            _DeltasController = new DeltasController(this);

            InitLayout();
            InitSecureTokenSelect();
            InitHandlers();
            UpdateUI();
        }

        void InitLayout()
        {
			vboxDataPaste.ModifyFg(StateType.Normal, Constants.Colors.DialogBackground.Gdk);
            eventbox1.ModifyBg(StateType.Normal, Constants.Colors.DialogBackground.Gdk);
			eventboxSeperator1.ModifyBg(StateType.Normal, Constants.Colors.Seperator.Gdk);
			eventboxSeperator2.ModifyBg(StateType.Normal, Constants.Colors.Seperator.Gdk);

			spinbuttonAmount.ModifyFg(StateType.Normal, Constants.Colors.Text2.Gdk);
			spinbuttonAmount.ModifyFont(Constants.Fonts.ActionBarSmall);

            labelTotalCost.ModifyFg(StateType.Normal, Constants.Colors.TextBlue.Gdk);
			labelTotalCost.ModifyFont(Constants.Fonts.LogBig);

			labelHeader.ModifyFg(StateType.Normal, Constants.Colors.TextHeader.Gdk);
			labelHeader.ModifyFont(Constants.Fonts.ActionBarBig);

			//error labels
			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.Error.Gdk);
				label.ModifyFont(Constants.Fonts.ActionBarSmall);
			}, labelAmountError);

			//labels
			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.LabelText.Gdk);
				label.ModifyFont(Constants.Fonts.ActionBarIntermediate);
            }, labelDestination, labelData, labelBlocks, labelSelectSecureToken, labelBalanceHeader, labelCostPerBlock, label8, label9);

			//entries
			Apply((Entry entry) =>
			{
				entry.ModifyBg(StateType.Normal, Constants.Colors.Seperator.Gdk);
				entry.ModifyText(StateType.Normal, Constants.Colors.Text.Gdk);
				entry.ModifyFont(Constants.Fonts.ActionBarSmall);
				entry.ModifyBase(StateType.Normal, Constants.Colors.ButtonUnselected.Gdk);
			}, txtHash, spinbuttonAmount);

            txtCode.ModifyBg(StateType.Normal, Constants.Colors.Seperator.Gdk);
            txtCode.ModifyText(StateType.Normal, Constants.Colors.Text.Gdk);
            txtCode.ModifyFont(Constants.Fonts.ActionBarSmall);
            txtCode.ModifyBase(StateType.Normal, Constants.Colors.ButtonUnselected.Gdk);
		}

        void InitSecureTokenSelect()
        {
			comboboxAsset.Model = _SecureTokenComboboxStore;
			var textRendererSecukreToken = new CellRendererText();
			comboboxAsset.PackStart(textRendererSecukreToken, false);
			comboboxAsset.AddAttribute(textRendererSecukreToken, "text", 1);

			_SecureTokenComboboxStore.AppendValues(new byte[] { }, "None");

			comboboxAsset.Changed += (sender, e) =>
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

        void InitHandlers()
        {
            txtCode.Buffer.Changed += delegate {
				_ContractController.UpdateContractInfo();
				UpdateUI();
            };

			eventboxPasterData.ButtonPressEvent += delegate
			{
				try
				{
					var clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", true));
                    txtCode.Buffer.Text = clipboard.WaitForText();
				}
				catch { }
			};

            spinbuttonAmount.ValueChanged += (sender, e) =>
			{
                UpdateUI();
			};

            eventboxActivate.ButtonReleaseEvent += async delegate {
                HideButtons();
                labelStatus.Text = "";

                var result = await _ContractController.ActivateContract(CostTotal, System.Text.Encoding.ASCII.GetBytes(Code), SecureToken);

				Gtk.Application.Invoke(delegate
				{
					switch (result.Result)
					{
						case ContractActivationResult.ResultEnum.Error:
							labelStatus.Text = "Error transmiting tx: " + result.TxResult;
                            labelStatus.ModifyFg(StateType.Normal, Constants.Colors.Error.Gdk);

							break;
						case ContractActivationResult.ResultEnum.NotEnoughZen:
							labelStatus.Text = "Not enougn Zen";
                            labelStatus.ModifyFg(StateType.Normal, Constants.Colors.Error.Gdk);
							break;
						case ContractActivationResult.ResultEnum.Success:
							labelStatus.Text = "Contract activated";
							labelStatus.ModifyFg(StateType.Normal, Constants.Colors.TextBlue.Gdk);
							break;
					}

                    _ContractController.UpdateContractInfo();
					UpdateUI();
				});
            };
		}

        void UpdateUI()
		{
            CostTotal = (ulong)spinbuttonAmount.Value * (ulong)CostPerBlock;

			if (PortfolioDeltas == null || !PortfolioDeltas.ContainsKey(Tests.zhash))
			{
                HasEnoughZen = false;
			}
			else if (PortfolioDeltas[Tests.zhash] >= (long)CostTotal)
			{
				HasEnoughZen = true;
			}
			else
			{
				HasEnoughZen = true;
			}

            if (HasEnoughZen)
			{
				labelAmountError.Text = "";
			}
			else
			{
				labelAmountError.Text = "Not enough Zen";
			}

            if (HasEnoughZen && CostTotal > 0)
			{
                if (hboxSignAndReview.Children.Contains(imageActivateDisabled))
				    hboxSignAndReview.Remove(imageActivateDisabled);
				if (!hboxSignAndReview.Children.Contains(eventboxActivate))
					hboxSignAndReview.Add(eventboxActivate);
			}
			else
			{
				if (hboxSignAndReview.Children.Contains(eventboxActivate))
					hboxSignAndReview.Remove(eventboxActivate);
				if (!hboxSignAndReview.Children.Contains(imageActivateDisabled))
					hboxSignAndReview.Add(imageActivateDisabled);
			}
		}

		void HideButtons()
		{
			if (hboxSignAndReview.Children.Contains(eventboxActivate))
				hboxSignAndReview.Remove(eventboxActivate);
			if (!hboxSignAndReview.Children.Contains(imageActivateDisabled))
				hboxSignAndReview.Add(imageActivateDisabled);
		}

        public void Init()
        {
            txtHash.Text = "";
            txtCode.Buffer.Text = "";
			labelStatus.Text = "";
            txtHash.Text = "";
            spinbuttonAmount.Value = 0;

			TreeIter iter;
			comboboxAsset.Model.GetIterFirst(out iter);
			comboboxAsset.SetActiveIter(iter);
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

		public AssetDeltas PortfolioDeltas { get; set; }
    }
}
