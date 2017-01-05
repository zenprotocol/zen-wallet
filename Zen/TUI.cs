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
		public static void Start (App app)
		{
			var root = new RootWindow();

			var dialog = new Dialog(root) { Text = "Zen", Width = 60, Height = 17, Top = 3, Left = 4, Border = BorderStyle.Thick };
			new Label(dialog) {Text = "Profile: " + app.Profile, Top = 15, Left = 8, Foreground = ConsoleColor.Black };
			new Label(dialog) {Text = "Blockchain DB: " + app.BlockChainDB, Top = 15, Left = 25, Foreground = ConsoleColor.Black };
			var buttonClose = new Button(dialog) { Text = "Close", Top = 2, Left = 6 };
			var checkboxGenesis = new Checkbox(dialog) { Text = "Genesis", Foreground = ConsoleColor.Black, Top = 2, Left = 18 };
			var buttonMenu = new Button(dialog) { Text = "Menu", Top = 2, Left = 30 };
			var list = new ListBox(dialog) { Top = 7, Left = 4, Width = 32, Height = 6, Border = BorderStyle.Thin };

			new Label(dialog) { Top = 6, Left = 40, Width = 16, Height = 6, Text = "Peers", Foreground = ConsoleColor.Black };
			var peersList = new ListBox(dialog) { Top = 7, Left = 40, Width = 16, Height = 6, Border = BorderStyle.Thin };
//			var line = new VerticalLine(dialog) { Top = 4, Left = 40, Width = 1, Height = 6, Border = BorderStyle.Thick };


			var dialogMenu = new Dialog(root) { Text = "Menu", Width = 50, Height = 10, Top = 6, Left = 6, Border = BorderStyle.Thick, Visible = false };
			var buttonBack = new Button(dialogMenu) { Text = "Back", Width = 8, Height = 3, Top = 1, Left = 30 };
			var menuList = new ListBox(dialogMenu) { Top = 7, Left = 10, Width = 16, Height = 6, Border = BorderStyle.Thin };

			list.Items.Add ("Wallet");
			list.Items.Add ("Tester");
			list.Items.Add ("Console");
			list.Items.Add ("Test");

			menuList.Items.Add ("Wipe BlockChain DB");

			checkboxGenesis.Checked = app.InitGenesisBlock;
			checkboxGenesis.Clicked += (sender, e) => {
				app.InitGenesisBlock = checkboxGenesis.Checked;
			};

			app.OnInitProfile += network => {
				if (network.Seeds != null) {
					foreach (String seed in network.Seeds) {
						peersList.Items.Add (seed);
					}
				}
			};
				
			list.ItemSelectedEvent += (object sender, ListBox.ItemSelectedArgs e) => {
				switch (e.Idx) {
				case 0:
					app.Mode = AppModeEnum.GUI;
					break;
				case 1:
					app.Mode = AppModeEnum.Tester;
					break;
				case 2:
					app.Mode = AppModeEnum.Console;
					break;
				case 3:
					//Infrastructure.JsonLoader<NodeTester.Settings>.Instance.FileName = "xx";
					//app.Network = TestNetwork.Instance;
					break;
				}

				app.Start();

			//	root.Detach();
			//	root.Run();
			};

			menuList.Clicked += (object sender, EventArgs e) => {
				Console.Write("x");
			};
			menuList.ItemSelectedEvent += (object sender, ListBox.ItemSelectedArgs e) => {
				switch (e.Idx) {
				case 0:
					if (Directory.Exists(app.BlockChainDB)) {
						Directory.Delete(app.BlockChainDB, true);
					}
					break;
				}

		//		dialogMenu.Hide ();
			//	dialog.Show ();
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
