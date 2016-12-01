using System;
using Gtk;
using Infrastructure;

namespace NodeTester
{
	public partial class SettingsWindow : Gtk.Window
	{
		Gtk.ListStore listStore = new Gtk.ListStore (typeof (string), typeof (string));

		public SettingsWindow () :
			base (Gtk.WindowType.Toplevel)
		{
			this.Build ();

			InitSeedsTreeView (treeviewSeeds);

			entryPeersToFind.Text = "" + JsonLoader<Settings>.Instance.Value.PeersToFind;
			entryMaximumNodeConnection.Text = "" + JsonLoader<Settings>.Instance.Value.MaximumNodeConnection;
			entryServerPort.Text = "" + JsonLoader<Settings>.Instance.Value.ServerPort;
		//	entryExternalEndpoint.Text = JsonLoader<Settings>.Instance.Value.ExternalEndpoint;
			checkbuttonAutoConfigure.Active = JsonLoader<Settings>.Instance.Value.AutoConfigure;
		}
			
		private void InitSeedsTreeView(TreeView treeView) {
			treeView.AppendColumn ("Seed", new Gtk.CellRendererText (), "text", 0);
			treeView.AppendColumn ("Type", new Gtk.CellRendererText (), "text", 1);

			PopulateList ();

			treeView.Model = listStore;
		}

		private void PopulateList() {
			listStore.Clear();

			foreach (String address in JsonLoader<Settings>.Instance.Value.IPSeeds) {
				listStore.AppendValues (address, "IP");
			}
		}

		protected void Button_Save (object sender, EventArgs e)
		{
			try {
				JsonLoader<Settings>.Instance.Value.PeersToFind = int.Parse(entryPeersToFind.Text);
			} catch
			{
				MessageDialog md = new MessageDialog(this, 
					DialogFlags.DestroyWithParent, MessageType.Error, 
					ButtonsType.Close, "Invalid Peers To Find setting");
				md.Run();
				md.Destroy();

				return;
			}

			try {
				JsonLoader<Settings>.Instance.Value.MaximumNodeConnection = int.Parse(entryMaximumNodeConnection.Text);
			} catch
			{
				MessageDialog md = new MessageDialog(this, 
					DialogFlags.DestroyWithParent, MessageType.Error, 
					ButtonsType.Close, "Invalid Maximum Node Connection setting");
				md.Run();
				md.Destroy();

				return;
			}

			try {
				JsonLoader<Settings>.Instance.Value.ServerPort = int.Parse(entryServerPort.Text);
			} catch
			{
				MessageDialog md = new MessageDialog(this, 
					DialogFlags.DestroyWithParent, MessageType.Error, 
					ButtonsType.Close, "Invalid Server Port setting");
				md.Run();
				md.Destroy();

				return;
			}

			JsonLoader<Settings>.Instance.Value.AutoConfigure = checkbuttonAutoConfigure.Active;

			JsonLoader<Settings>.Instance.Save ();
			Destroy ();
		}

		protected void Button_Cancel (object sender, EventArgs e)
		{
			Destroy ();
		}

		protected void Button_AddSeed (object sender, EventArgs e)
		{
			new AddressManagerEditorAddWindow ((IPEndPoint) => {
				JsonLoader<Settings>.Instance.Value.IPSeeds.Add(IPEndPoint.ToString());
				PopulateList();
			}).Show ();
		}

		protected void Button_DeleteSeed (object sender, EventArgs e)
		{
			TreeIter iter;

			treeviewSeeds.Selection.GetSelected (out iter);

			String seed = (String) treeviewSeeds.Model.GetValue (iter, 0);

			JsonLoader<Settings>.Instance.Value.IPSeeds.Remove (seed);

			PopulateList ();
		}

		protected void Button_SetExternalEndpoint (object sender, EventArgs e)
		{
	//		new AddressManagerEditorAddWindow ((IPEndPoint) => {
	//			JsonLoader<Settings>.Instance.Value.ExternalEndpoint = entryExternalEndpoint.Text = IPEndPoint.ToString();
	//		}).Show ();
		}
	}
}

