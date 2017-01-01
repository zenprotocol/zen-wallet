using System;
using CLRCLI.Widgets;
using CLRCLI;
using Infrastructure;
using NBitcoinDerive;

namespace Zen
{
	class TUI
	{
		public static void Start (string[] args)
		{
			var root = new RootWindow();

			var dialog = new Dialog(root) { Text = "Zen", Width = 60, Height = 15, Top = 4, Left = 4, Border = BorderStyle.Thick };
			new Label(dialog) {Text = "This is a dialog!", Top = 2, Left = 2};
			var buttonClose = new Button(dialog) { Text = "Close", Top = 2, Left = 6 };
			var buttonMenu = new Button(dialog) { Text = "Menu", Top = 2, Left = 18 };
			var buttonHelp = new Button(dialog) { Text = "GUI", Top = 2, Left = 30 };
		//	var buttonConsole = new Button(dialog) { Text = "Console", Top = 4, Left = 42 };
			var list = new ListBox(dialog) { Top = 7, Left = 4, Width = 32, Height = 6, Border = BorderStyle.Thin };
			var peersList = new ListBox(dialog) { Top = 7, Left = 40, Width = 16, Height = 6, Border = BorderStyle.Thin };
//			var line = new VerticalLine(dialog) { Top = 4, Left = 40, Width = 1, Height = 6, Border = BorderStyle.Thick };

			var dialog2 = new Dialog(root) { Text = "ooooh", Width = 32, Height = 5, Top = 6, Left = 6, Border = BorderStyle.Thick, Visible = false };
			var buttonQuit = new Button(dialog2) { Text = "Quit", Width = 8, Height = 3, Top = 1, Left = 1 };
			var buttonBack = new Button(dialog2) { Text = "Back", Width = 8, Height = 3, Top = 1, Left = 12 };


			var dialogHelp = new Dialog(root) { Text = "Help", Width = 32, Height = 5, Top = 6, Left = 6, Border = BorderStyle.Thick, Visible = false };
		//	new SingleLineTextbox (dialogHelp) { Text = "dfgkljdghlksjdfghslkdjhlskjdfhglksjdfg" , Top =1, Left =1};
			var buttkkkkonBack = new SingleLineTextbox(dialogHelp) { Text = "Back", Width = 8, Height = 3, Top = 1, Left = 12 };

			buttonQuit.Clicked += (s, e) => { root.Detach(); };
			buttonBack.Clicked += (s, e) => { dialog.Hide(); dialog2.Show(); };

			list.Items.Add ("Wallet");
			list.Items.Add ("Tester");
			list.Items.Add ("Console");
			list.Items.Add ("Test");

			list.ItemSelectedEvent += (object sender, ListBox.ItemSelectedArgs e) => {
				switch (e.Idx) {
				case 0:
					App.Instance.Mode = ModeEnum.GUI;
					break;
				case 1:
					App.Instance.Mode = ModeEnum.Tester;
					break;
				case 2:
					App.Instance.Mode = ModeEnum.Console;
					break;
				case 3:
					//Infrastructure.JsonLoader<NodeTester.Settings>.Instance.FileName = "xx";
					//app.Network = TestNetwork.Instance;
					break;
				}

				App.Instance.Start();
			};


			//App.Instance.OnNetworkChanged += network => {
			Network network = JsonLoader<NBitcoinDerive.Network>.Instance.Value;
			if (network.Seeds != null) {
				foreach (String seed in network.Seeds) {
					peersList.Items.Add (seed);
				}
			}
			//};

			buttonClose.Clicked += buttonClose_Clicked;

			buttonHelp.Clicked += (object sender, EventArgs e) => {
				dialog.Hide(); 
				dialogHelp.Show();
			};

			//buttonConsole.Clicked += (object sender, EventArgs e) => {
			//	NodeConsole.MainClass.Main(null);
			//};

			//	list.
			//Console.WriteLine ("XXX");

	//		new Thread (() => {
	//			while (true) {
	//				Thread.Sleep(2000);
	////				Console.WriteLine ("XXX11");
	////				root.Detach();
	//			}
	//		}).Start();

			root.Run();

		//	System.Console.WriteLine ("XXX11");
		}

		static void buttonClose_Clicked(object sender, EventArgs e)
		{
			(sender as Button).RootWindow.Detach();
			Console.Clear ();
		//	System.Console.WriteLine ("!");
		//	System.Console.ReadLine ();
		//	(sender as Button).RootWindow.Run();
		}

	}
}
