﻿using System;
using System.Collections.Generic;
using Consensus;
using Wallet.core.Data;
using System.Linq;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class SendDialogStep1 : WidgetBase
	{
		byte[] asset;

		public SendDialogStep1()
		{
			this.Build();

			dialogfieldAmount.Caption = "AMOUNT";
			dialogfieldTo.Caption = "TO";
			dialogcombofieldAsset.Caption = "ASSET";

			var i = 0;
			int selectedIdx = 0;
			var keys = new Dictionary<string, byte[]>();

			Gtk.TreeIter iter;
			dialogcombofieldAsset.ComboBox.Model.IterNthChild(out iter, selectedIdx);
			dialogcombofieldAsset.ComboBox.SetActiveIter(iter);

			dialogcombofieldAsset.ComboBox.Changed += (sender, e) =>
			{
				var comboBox = sender as Gtk.ComboBox;

                if (comboBox.GetActiveIter(out iter))
                {
                    var value = new GLib.Value();
                    comboBox.Model.GetValue(iter, 0, ref value);
                    asset = keys[value.Val as string];
                }
			};

			eventboxSend.ButtonReleaseEvent += (object o, Gtk.ButtonReleaseEventArgs args) =>
			{
				ulong amount;
				Address address;

				try
				{
					amount = ulong.Parse(dialogfieldAmount.Value);
				}
				catch
				{
					labelMessage.Text = "Invalid amount";
					return;
				}

				if (amount <= 0)
				{
					labelMessage.Text = "Invalid amount";
					return;
				}

				try
				{
					address = new Address(dialogfieldTo.Value);
				} catch
				{
					labelMessage.Text = "Invalid address";
					return;
				}

				Types.Transaction tx;

				if (App.Instance.Wallet.Sign(
					address,
					asset,
					amount,
					out tx
				))
				{
					FindParent<SendDialog>().Next(tx);
				}
				else
				{
                    labelMessage.Text = "Not enough " + App.Instance.AssetsMetadata.TryGetValue(asset);
				}
			};

			buttonRaw.Clicked += delegate
			{
				new SendRaw().ShowDialog();
			};
		}
	}
}

