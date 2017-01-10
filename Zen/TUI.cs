using System;
using CLRCLI.Widgets;
using CLRCLI;
using Infrastructure;
using NBitcoinDerive;
using System.IO;

namespace Zen
{
	class TUI
	{
		public static void Start (App app, String args)
		{
			var root = new RootWindow();

			var dialog = new Dialog(root) { Text = "Zen", Width = 60, Height = 18, Top = 3, Left = 4, Border = BorderStyle.Thick };

			var buttonClose = new Button(dialog) { Text = "Close", Top = 2, Left = 6 };
			var checkboxGenesis = new Checkbox(dialog) { Text = "Genesis", Foreground = ConsoleColor.Black, Top = 2, Left = 18 };
			var buttonMenu = new Button(dialog) { Text = "Menu", Top = 2, Left = 30 };
			var list = new ListBox(dialog) { Top = 7, Left = 4, Width = 32, Height = 6, Border = BorderStyle.Thin };

			new Label(dialog) { Top = 6, Left = 40, Width = 16, Height = 6, Text = "Peers", Foreground = ConsoleColor.Black };
			var peersList = new ListBox(dialog) { Top = 7, Left = 40, Width = 16, Height = 6, Border = BorderStyle.Thin };
//			var line = new VerticalLine(dialog) { Top = 4, Left = 40, Width = 1, Height = 6, Border = BorderStyle.Thick };

			new Label(dialog) {Text = "Command line args: " + args, Top = 14, Left = 8, Foreground = ConsoleColor.Black };
			new Label(dialog) {Text = "Profile: " + app.Settings.NetworkProfile, Top = 15, Left = 8, Foreground = ConsoleColor.Black };
			new Label(dialog) {Text = "Blockchain DB: " + app.Settings.BlockChainDB, Top = 15, Left = 25, Foreground = ConsoleColor.Black };
			new Label(dialog) { Text = "Port: " + app.Settings.Port + " Connections: " + app.Settings.Connections + " Peers: " + app.Settings.PeersToFind, Top = 16, Left = 8, Foreground = ConsoleColor.Black };

			var dialogMenu = new Dialog(root) { Text = "Menu", Width = 50, Height = 10, Top = 6, Left = 6, Border = BorderStyle.Thick, Visible = false };
			var buttonBack = new Button(dialogMenu) { Text = "Back", Width = 8, Height = 3, Top = 1, Left = 30 };
			var menuList = new ListBox(dialogMenu) { Top = 7, Left = 5, Width = 16, Height = 6, Border = BorderStyle.Thin };
			var outputsList = new ListBox(dialogMenu) { Top = 7, Left = 35, Width = 16, Height = 6, Border = BorderStyle.Thin };

			list.Items.Add ("Wallet");
			list.Items.Add ("Tester");
			list.Items.Add ("Console");

			menuList.Items.Add ("Wipe BlockChain DB");

			checkboxGenesis.Checked = app.Settings.InitGenesisBlock;
			checkboxGenesis.Clicked += (sender, e) => {
				app.Settings.InitGenesisBlock = checkboxGenesis.Checked;
			};

			app.OnInitProfile += network => {
				if (network.Seeds != null) {
					foreach (String seed in network.Seeds) {
						peersList.Items.Add (seed);
					}
				}
			};
				
			list.Clicked += (object sender, EventArgs e) => {
				switch (((ListBox)sender).SelectedIndex) {
				case 0:
					app.Settings.Mode = Settings.AppModeEnum.GUI;
					break;
				case 1:
					app.Settings.Mode = Settings.AppModeEnum.Tester;
					break;
				case 2:
					app.Settings.Mode = Settings.AppModeEnum.Console;
					break;
				}

				app.Start();

				peersList.Items.Clear ();
				root.Detach();
				Console.Clear();
				root.Run();
			};

			menuList.Clicked += (object sender, EventArgs e) => {
				switch (((ListBox)sender).SelectedIndex) {
				case 0:
					if (Directory.Exists(app.Settings.BlockChainDB)) {
						Directory.Delete(app.Settings.BlockChainDB, true);
					}
					break;
				}
			};
				
			buttonClose.Clicked += (s, e) => {
				root.Detach ();
			};

			buttonMenu.Clicked += (s, e) => {
				dialog.Hide ();
				dialogMenu.Show ();
				dialogMenu.SetFocus();
			};

			buttonBack.Clicked += (s, e) => {
				dialogMenu.Hide ();
				dialog.Show ();
			};
					
			root.Run();
		}
	}
}
