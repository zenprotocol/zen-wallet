using System;
using CLRCLI.Widgets;
using CLRCLI;
using System.IO;
using System.Collections.Generic;
using Wallet.core.Data;
using Infrastructure;
using Zen.Data;
using System.Globalization;
using Wallet.Constants;
using Wallet.core;

namespace Zen
{
	class TUI
	{
		static ListBox listTrace;
		static Dialog dialog;
		static ListBox listMenu;
		static string currentMenu { get; set; }

		public static void Start (App app, String args)
		{
			var root = new RootWindow();
			var options = new Dictionary<string, List<string>>();

			options["main"] = new List<string>();
			options["main"].Add("Reconnect");
			options["main"].Add("Start GUI");
			options["main"].Add("Wallet Menu");
			options["main"].Add("BlockChain Menu");
			options["main"].Add("Miner Menu");
			options["main"].Add("Tests");
			options["main"].Add("Reset DB");
			options["main"].Add("Generate Graph");
			options["main"].Add("Stop");
			options["main"].Add("Exit");

			options["wallet"] = new List<string>();
		//	options["wallet"].Add("Reset");
		//	options["wallet"].Add("Sync");
			options["wallet"].Add("Add Genesis UTXO");
			options["wallet"].Add("List Keys");
			options["wallet"].Add("Import Test Key");
			options["wallet"].Add("Get Receive Address");
			options["wallet"].Add("My Wallet");
			options["wallet"].Add("Send Dialog");
			options["wallet"].Add("Back");

			options["blockchain"] = new List<string>();
			options["blockchain"].Add("Add Genesis");
			options["blockchain"].Add("Back");

			options["miner"] = new List<string>();
			options["miner"].Add("Start Miner");
			options["miner"].Add("Stop Miner");
			options["miner"].Add("Back");

			options["tests"] = new List<string>();

			foreach (var scriptFile in Directory.GetFiles("Scripts"))
			{
				options["tests"].Add(new FileInfo(scriptFile).Name);
			}
			options["tests"].Add("Back");


			var actions = new Dictionary<string, Action<string>>();
			Action<string> menu = (s) =>
			{
				currentMenu = s;
				listMenu.Items.Clear();
				listMenu.SelectedIndex = 0;
				options[currentMenu].ForEach(t => listMenu.Items.Add(t));
			};

			dialog = new Dialog(root) { Text = string.IsNullOrEmpty(app.Settings.NetworkProfile) ? "Zen" : app.Settings.NetworkProfile, Width = 75, Height = 19, Top = 2, Left = 2, Border = BorderStyle.Thick };

			var sendDialog = new Dialog(root) { Text = "Send", Width = 70, Height = 18, Top = 4, Left = 4, Border = BorderStyle.Thick, Visible = false };

			new Label(sendDialog) { Top = 1, Left = 1, Width = 66, Text = "Destination" };
			var address = new SingleLineTextbox(sendDialog) { Top = 3, Left = 1, Width = 65 };
			new Label(sendDialog) { Top = 5, Left = 1, Width = 66, Text = "Amount" };
			var amount = new SingleLineTextbox(sendDialog) { Top = 7, Left = 1, Width = 65 };
			var sendDialogSendButton = new Button(sendDialog) { Top = 10, Left = 1, Width = 15, Text = "Send" };
			var sendDialogCloseButton = new Button(sendDialog) { Top = 10, Left = 20, Width = 15, Text = "Close" };
			var status = new Label(sendDialog) { Top = 16, Left = 1, Width = 40, Text = "", Background = ConsoleColor.Black };

			sendDialogCloseButton.Clicked += (sender, e) => {
				sendDialog.Hide();
				dialog.Show();
			};

			sendDialogSendButton.Clicked += (sender, e) =>
			{
				ulong _amount;

				try
				{
					_amount = ulong.Parse(amount.Text);
				}
				catch
				{
					status.Text = "Invalid amount";
					return;
				}

				Address _address;

				try
				{
					_address = new Address(address.Text);
				}
				catch
				{
					status.Text = "Invalid address";
					return;
				}

				if (!app.Spend(_amount, _address))
				{
					status.Text = "Could not spend";
					return;
				}

				status.Text = "Success";
			};

			Action showSendDialog = () => {
				status.Text = "";
				dialog.Hide();
				sendDialog.Show();
				address.SetFocus();
			};

			actions["main"] = async a =>
			{
				switch (a)
				{
					case "Wallet Menu":
						menu("wallet");
						break;
					case "BlockChain Menu":
						menu("blockchain");
						break;
					case "Miner Menu":
						menu("miner");
						break;
					case "Reconnect":
						await app.Reconnect();
						break;
					case "Start GUI":
						app.GUI();
						break;
					case "Reset DB":
						app.ResetDB();
						break;
					case "Generate Graph":
						app.Dump();
						break;
					case "Stop":
						app.Stop();
						break;
					case "Tests":
						menu("tests");
						break;
					case "Exit":
						app.Stop();
						root.Detach();
						break;
				}
			};

			actions["blockchain"] = a =>
			{
				switch (a)
				{
					case "Add Genesis":
						app.AddGenesisBlock();
						break;
					case "Back":
						menu("main");
						break;
				}
			};

			actions["miner"] = a =>
			{
				switch (a)
				{
					case "Start Miner":
						app.MinerEnabled = true;
						break;
					case "Stop Miner":
						app.MinerEnabled = false;
						break;
					case "Back":
						menu("main");
						break;
				}
			};

			actions["tests"] = a =>
			{
				switch (a)
				{
					case "Back":
						menu("main");
						break;
					default:
						ScriptRunner.Execute(app, Path.Combine("Scripts", a));	
						break;
				}
			};

			var wallet_mode = "";
			actions["wallet"] = a =>
			{
				switch (a)
				{
					case "Add Genesis UTXO":
						wallet_mode = a;
						listMenu.Items.Clear();

						foreach (var output in JsonLoader<Outputs>.Instance.Value.Values)
						{
							listMenu.Items.Add(output.Amount + " " + output.Key);
						}
						listMenu.Items.Add("Back");
						break;
					case "List Keys":
						listMenu.Items.Clear();

						foreach (var key in app.WalletManager.GetKeys())
						{
							string info = key.Used ? "used" : "ununsed";

							if (key.Change)
								info += ",change";
									
							listMenu.Items.Add(info + " " + key.PrivateAsString);
						}
						listMenu.Items.Add("Back");
						break;
					case "Import Test Key":
						wallet_mode = a;

						listMenu.Items.Clear();

						foreach (var key in  JsonLoader<Keys>.Instance.Value.Values)
						{
							listMenu.Items.Add(key);
						}
						listMenu.Items.Add("Back");
						break;
					//case "Reset":
					//	app.ResetWallet();
					//	break;
					//case "Import":
					//	app.ImportWallet();
					//	break;
					case "Get Receive Address":
						listTrace.Items.Add(app.WalletManager.GetUnusedKey().Address.ToString());
						break;
					case "My Wallet":
						listMenu.Items.Clear();

						foreach (var txDelta in app.WalletManager.TxDeltaList)
						{
							listMenu.Items.Add(GetTxDeltaInfo(txDelta));
						}
						listMenu.Items.Add("Back");
						break;
					case "Send Dialog":
						showSendDialog();
						break;
					case "Back":
						menu("main");
						break;
					default:
						switch (wallet_mode)
						{
							case "Add Genesis UTXO":
								foreach (var output in JsonLoader<Outputs>.Instance.Value.Values)
								{
									if (a == output.Amount + " " + output.Key)
									{
										app.WalletManager.Import(Key.Create(output.Key));
									}
								}
								break;
							case "Import Test Key":
								app.WalletManager.Import(Key.Create(a));
								break;
						}
						break;
				}
			};

		//	dialogMenu.Show();
			listMenu = new ListBox(dialog) { Top = 1, Left = 1, Width = 72, Height = 6, Border = BorderStyle.Thin };
			listTrace = new ListBox(dialog) { Top = 8, Left = 1, Width = 72, Height = 11, Border = BorderStyle.Thin };

			menu("main");
			//	app.Settings.InitGenesisBlock = checkboxGenesis.Checked;

			listMenu.Clicked += (object sender, EventArgs e) => {
		//		var option = currentMenu + "_" + ((ListBox)sender).SelectedItem;

				if (actions.ContainsKey(currentMenu))
				{
					actions[currentMenu](((ListBox)sender).SelectedItem as string);
				}
				else
				{
					Console.Write("missing " + currentMenu);
				}
			};

			Action<ResetEventArgs> wallet_OnReset = a =>
			{
				foreach (var txDelta in a.TxDeltaList)
					listTrace.Items.Add(GetTxDeltaInfo(txDelta, "Wallet reset"));
			};

			Action<TxDeltaItemsEventArgs> wallet_OnItems = a =>
		   	{
				foreach (var txDelta in a)
					listTrace.Items.Add(GetTxDeltaInfo(txDelta, "Wallet item"));
		   	};

			app.WalletOnItemsHandler = wallet_OnItems;
			app.WalletOnResetHandler = wallet_OnReset;

		//	app.Init();
			root.Run();
		}

        static string GetTxDeltaInfo(TxDelta txDelta, string prefix = null)
		{
			string info = (prefix == null ? "" : prefix + ": ") + txDelta.TxState.ToString();

			info += ", " + txDelta.Time.ToString("g", DateTimeFormatInfo.InvariantInfo);
			if (txDelta.AssetDeltas.ContainsKey(Consensus.Tests.zhash))
			{
				var value = txDelta.AssetDeltas[Consensus.Tests.zhash];
				info += ", " + value;
			}
			else
			{
				info += ", " + "Other asset";
			}

			return info;
		}
						                
		public static void WriteColor(string message, ConsoleColor color)
		{
			if (listTrace != null)
			{
				listTrace.Items.Add(message.Split('\n')[0]);

				listTrace.SelectedIndex = listTrace.Items.Count - 1;
			}
			else
			{
				Console.ForegroundColor = color;
				Console.Write(message);
			}
		}
	}
}
