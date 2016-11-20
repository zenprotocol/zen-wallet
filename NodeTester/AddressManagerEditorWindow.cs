using System;
using Gtk;
using NBitcoin.Protocol;
using System.Net;

namespace NodeTester
{
	public partial class AddressManagerEditorWindow : Gtk.Window
	{
		Gtk.ListStore listStore = new Gtk.ListStore (typeof (string), typeof (string));

		public AddressManagerEditorWindow () :
			base (Gtk.WindowType.Toplevel)
		{
			this.Build ();

			InitAddressesTreeView (treeviewAddresses);
		}

		private void InitAddressesTreeView(TreeView treeView) {
			treeView.AppendColumn ("Address", new Gtk.CellRendererText (), "text", 0);
			treeView.AppendColumn ("Time", new Gtk.CellRendererText (), "text", 1);
			treeView.AppendColumn ("Ago", new Gtk.CellRendererText (), "text", 2);

			PopulateList ();

			treeView.Model = listStore;
		}

		private void PopulateList() {
			listStore.Clear();

			foreach (IPEndPoint IPEndPoint in NodeCore.AddressManager.Instance.GetNetworkAddresses()) {
				listStore.AppendValues (IPEndPoint.ToString (), "xx", "xx");// networkAddress.Time.ToString(), networkAddress.Ago.ToString());
			}
		}
			
		protected void Menu_Add (object sender, EventArgs e)
		{
			new AddressManagerEditorAddWindow (IPEndPoint => {
				new LogMessageContext ("Address Add Window").Create("Created " + IPEndPoint.ToString ());

				bool added = NodeCore.AddressManager.Instance.Add (IPEndPoint);

				if (!added) {
					MessageDialog md = new MessageDialog(this, 
						DialogFlags.DestroyWithParent, MessageType.Error, 
						ButtonsType.Close, "Addresss exists");
					md.Run();
					md.Destroy();
				}

				PopulateList();
			});
		}


		protected void Menu_Save (object sender, EventArgs e)
		{
			NodeCore.AddressManager.Instance.Save ();
		}
	}
}

