using System;
using CLRCLI.Widgets;
using CLRCLI;
using System.IO;
using System.Collections.Generic;
using Wallet.core.Data;
using Infrastructure;

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
			options["main"].Add("Start Console");
			options["main"].Add("Start GUI");
			options["main"].Add("Wallet Menu");
			options["main"].Add("BlockChain Menu");
			options["main"].Add("Tests");
			options["main"].Add("Stop");
			options["main"].Add("Exit");

			options["wallet"] = new List<string>();
		//	options["wallet"].Add("Reset");
		//	options["wallet"].Add("Sync");
			options["wallet"].Add("Add Key");
			options["wallet"].Add("Back");

			options["blockchain"] = new List<string>();
			options["blockchain"].Add("Add Genesis");
			options["blockchain"].Add("Back");

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

			dialog = new Dialog(root) { Text = "Zen", Width = 75, Height = 19, Top = 2, Left = 2, Border = BorderStyle.Thick };
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

			actions["main"] = a =>
			{
				switch (a)
				{
					case "Wallet Menu":
						menu("wallet");
						break;
					case "BlockChain Menu":
						menu("blockchain");
						break;
					case "Start Console":
						app.Start();
						dialog.Text += " (running)";
						break;
					case "Start GUI":
						app.GUI();
						dialog.Text += " (running)";
						break;
					case "Stop":
						app.Stop();
						dialog.Text = "Zen";
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
					//case "Reset":
					//	app.ResetWallet();
					//	break;
					//case "Import":
					//	app.ImportWallet();
					//	break;
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

						app.Start();
						app.GUI();
						break;
					case "Back":
						menu("main");
						break;
					case "Localhost Client":
						app.Settings.EndpointOptions.EndpointOption =
								NBitcoinDerive.EndpointOptions.EndpointOptionsEnum.LocalhostClient;
//						app.Start();
						break;
					case "Localhost Server":
						app.Settings.EndpointOptions.EndpointOption =
							   NBitcoinDerive.EndpointOptions.EndpointOptionsEnum.LocalhostServer;
//						app.Start();
						break;
				}
			};

		//	dialogMenu.Show();
			listMenu = new ListBox(dialog) { Top = 1, Left = 1, Width = 72, Height = 6, Border = BorderStyle.Thin };
			listTrace = new ListBox(dialog) { Top = 8, Left = 1, Width = 72, Height = 11, Border = BorderStyle.Thin };

			menu("main");
			//	app.Settings.InitGenesisBlock = checkboxGenesis.Checked;

			app.OnInitProfile += network => {
			//	if (network.Seeds != null) {
			//		foreach (String seed in network.Seeds) {
					//	peersList.Items.Add (seed);
			//		}
			//	}
			};
							
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
		}

			//	listTrace.Items.Add(new Label(listTrace) { Text = message.Trim(), Foreground = color });


	}
}
