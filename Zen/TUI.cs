using System;
using CLRCLI.Widgets;
using CLRCLI;
using System.IO;
using System.Collections.Generic;
using Wallet.core.Data;
using Infrastructure;
using Zen.Data;

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
			options["main"].Add("Stop");
			options["main"].Add("Exit");

			options["wallet"] = new List<string>();
		//	options["wallet"].Add("Reset");
		//	options["wallet"].Add("Sync");
			options["wallet"].Add("Add Key");
			options["wallet"].Add("List Keys");
			options["wallet"].Add("Get Receive Address");
			options["wallet"].Add("Back");

			options["blockchain"] = new List<string>();
			options["blockchain"].Add("Add Genesis");
			options["blockchain"].Add("Back");

			options["miner"] = new List<string>();
			options["miner"].Add("Start Miner");
			options["miner"].Add("Stop Miner");
			options["miner"].Add("Back");

			options["tests"] = new List<string>();
			options["tests"].Add("Import all genesis");
			options["tests"].Add("Localhost Client");
			options["tests"].Add("Localhost Server");
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

			//var dialogMenu = new Dialog(root) { Text = "Menu", Width = 50, Height = 10, Top = 6, Left = 6, Border = BorderStyle.Thick, Visible = false };
			//var x = new SingleLineTextbox(dialogMenu)
			//{
			//	Top = 1,
			//	Left = 1,
			//	Width = 20
			//};
			//Action addDialog = () =>
			//{
			//	dialog.Hide();
			//	//listMenu = new ListBox(dialog) { Top = 1, Left = 1, Width = 70, Height = 6, Border = BorderStyle.Thin };
			//	//listTrace = new ListBox(dialog) { Top = 8, Left = 1, Width = 73, Height = 11, Border = BorderStyle.Thin };
			//	dialogMenu.Show();
			//	x.SetFocus();
			//};

			//dialogMenu.Hide();

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

			actions["wallet"] = a =>
			{
				switch (a)
				{
					case "Add Key":
						listMenu.Items.Clear();

						foreach (var output in JsonLoader<Outputs>.Instance.Value.Values)
						{
							listMenu.Items.Add(output.Amount + " " + output.Key);
						}
						listMenu.Items.Add("Back");
						break;
					case "List Keys":
						listMenu.Items.Clear();

						foreach (var key in app.ListKeys())
						{
							string info = key.Used ? "used" : "ununsed";

							if (key.Change)
								info += ",change";
									
							listMenu.Items.Add(info + " " + key.Address);
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
						listTrace.Items.Add(app.GetUnusedKey().Address.ToString());
						break;
					case "Back":
						menu("main");
						break;
					default:
						foreach (var output in JsonLoader<Outputs>.Instance.Value.Values)
						{
							if (a == output.Amount + " " + output.Key)
							{
								app.ImportKey(output.Key);
							}
						}
						break;
				}
			};

			actions["tests"] = a =>
			{
				switch (a)
				{
					case "Import all genesis":
						app.AddGenesisBlock();

						JsonLoader<Outputs>.Instance.Value.Values.ForEach(o => app.ImportKey(o.Key));

						app.Reconnect();
						app.GUI();
						break;
					case "Back":
						menu("main");
						break;

//					case "Localhost Client":
//						app.Settings.EndpointOption = Settings.EndpointOptionsEnum.LocalhostClient;
////						app.Start();
//						break;
//					case "Localhost Server":
//						app.Settings.EndpointOption = Settings.EndpointOptionsEnum.LocalhostServer;
////						app.Start();
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

		//	app.Init();
			root.Run();
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
