﻿using System;
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
		String Code { get; set; }
        decimal CostPerBlock { set; }
        ulong CostTotal { get; set; }
	}

    [System.ComponentModel.ToolboxItem(true)]
    public partial class ContractLayout : WidgetBase, IContractView, IAssetsView, IPortfolioVIew, IControlInit
    {
		ContractController _ContractController;
		readonly string NONE = "None";

		UpdatingStore<byte[]> _SecureTokenComboboxStore = new UpdatingStore<byte[]>(
			0,
			typeof(byte[]),
			typeof(string)
		);
		
		// public bool IsActive { get; set; }
		public byte[] Hash { set { txtHash.Text = Convert.ToBase64String(value); } }
        public string Code { get { return txtCode.Buffer.Text; } set { txtCode.Buffer.Text = value; } }

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
			}, labelAmountError, labelSecureTokenError);

			//labels
			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.LabelText.Gdk);
				label.ModifyFont(Constants.Fonts.ActionBarIntermediate);
			}, labelDestination, labelData, labelBlocks, labelSelectSecureToken, labelCostTotal, labelCostPerBlock, label8, label9);

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

            //remove the secure-token box
            vbox.Remove(vbox5);
		}

        void InitSecureTokenSelect()
        {
			comboboxAsset.Model = _SecureTokenComboboxStore;
			var textRendererSecukreToken = new CellRendererText();
			comboboxAsset.PackStart(textRendererSecukreToken, false);
			comboboxAsset.AddAttribute(textRendererSecukreToken, "text", 1);

			_SecureTokenComboboxStore.AppendValues(new byte[] { }, NONE);
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

			comboboxAsset.Changed += (sender, e) =>
			{
				UpdateUI();
			};

            eventboxActivate.ButtonReleaseEvent += async delegate {
                HideButtons();
                labelStatus.Text = "";

                byte[] secureToken = null;
				TreeIter iter;
				if (comboboxAsset.GetActiveIter(out iter))
				{
					secureToken = (byte[])_SecureTokenComboboxStore.GetValue(iter, 0);
                    if ((string)_SecureTokenComboboxStore.GetValue(iter, 1) == NONE)
                        secureToken = null;
				}

                var result = await _ContractController.ActivateContract(CostTotal, System.Text.Encoding.ASCII.GetBytes(Code), secureToken);

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
			bool hasZen;

			if (PortfolioDeltas == null || !PortfolioDeltas.ContainsKey(Tests.zhash))
			{
                hasZen = false;
			}
			else if (PortfolioDeltas[Tests.zhash] >= (long)CostTotal)
			{
				hasZen = true;
			}
			else
			{
				hasZen = true;
			}

            if (hasZen)
			{
				labelAmountError.Text = "";
			}
			else
			{
				labelAmountError.Text = "Not enough Zen";
			}

			bool hasToken = true;
			TreeIter iter;
			if (comboboxAsset.GetActiveIter(out iter))
			{
				var secureToken = (byte[])comboboxAsset.Model.GetValue(iter, 0);
				var secureTokenCaption = (string)comboboxAsset.Model.GetValue(iter, 1);

				if (secureTokenCaption == NONE)
				{
					hasToken = true;
				}
				else if (PortfolioDeltas == null || !PortfolioDeltas.ContainsKey(secureToken) || PortfolioDeltas[secureToken] < 1)
				{
					hasToken = false;
				}
			}

            if (hasToken)
            {
				labelSecureTokenError.Text = "";
			}
			else
			{
				labelSecureTokenError.Text = "No Asset";
			}

			if (hasZen && hasToken && CostTotal > 0)
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

			labelSecureTokenError.Text = "";
			labelAmountError.Text = "";

			TreeIter storeIter;
			var canIter = comboboxAsset.Model.GetIterFirst(out storeIter);

            while (canIter)
            {
				var secureToken = (string)comboboxAsset.Model.GetValue(storeIter, 1);

				if (secureToken == NONE)
				{
					comboboxAsset.SetActiveIter(storeIter);
					break;
				}

                canIter = comboboxAsset.Model.IterNext(ref storeIter);
            }
		}

        public ICollection<AssetMetadata> Assets
		{
			set
			{
                foreach (var item in value)
                    AssetUpdated = item;
			}
		}

		public AssetMetadata AssetUpdated
		{
			set
			{
				_SecureTokenComboboxStore.Upsert(t => t.SequenceEqual(value.Asset), value.Asset, value.Display);
			}
		}

		AssetDeltas _PortfolioDeltas;
		public AssetDeltas PortfolioDeltas { 
			get 
			{
				return _PortfolioDeltas;
			} 
			set 
			{
				_PortfolioDeltas = value;
				UpdateUI();
			}
		}
    }
}
