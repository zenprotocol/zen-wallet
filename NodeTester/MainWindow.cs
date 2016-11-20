using System;
using System.Collections.Generic;
using System.Net;
using Open.Nat;
using NodeTester;
using System.Linq;
using Infrastructure;
using NodeCore;
using Gtk;

public partial class MainWindow : ResourceOwnerWindow
{
	int messagesCount = 0;
	int connectionsCount = 0;
	int peersCount = 0;

	public MainWindow () : base (Gtk.WindowType.Toplevel)
	{
		Build ();

//		statusbar3.Push (1, "xx");
//		statusbar3.Push (1, "yy");
//		statusbar3.Push (2, "zz");

		InitLogTreeView (treeviewLog);
		InitServerPane ();
		InitPeersPane ();
		InitPnPPane ();
		InitSummaryPage ();

		DeleteEvent += OnDeleteEvent;

		OwnResource(MessageProducer<NodeTester.Settings>.Instance.AddMessageListener(new MessageListener<NodeTester.Settings>((Settings) =>
		{
			entryCheckPort_Port.Text = Settings.ServerPort.ToString();
		})));

		buttonServerTest.Clicked += Button_ServerTest;
		buttonDiscover.Clicked += Button_Discover;
		buttonDiscoverStop.Clicked += Button_DiscoverStop;
		buttonStopServer.Clicked += Button_StopServer;
		buttonStartServer.Clicked += Button_StartServer;
		buttonGetExternalIP_3rd.Clicked += Button_GetExternalIp_3rd;
		buttonGetExternalIP_UPNP.Clicked += Button_GetExternalIP_UPnP;
		buttonGetUPnPMapping.Clicked += Button_GetUPnPMapping;
		buttonAddMapping.Clicked += Button_AddMapping;
		buttonRemoveMapping.Clicked += Button_RemoveMapping;
		buttonCheckPort.Clicked += Button_CheckPort;
		buttonDeviceList.Clicked += Button_DeviceList;

		OpenConsole();

		if (JsonLoader<NodeTester.Settings>.Instance.IsNew)
			new SettingsWindow ().Show ();
	}

	private void InitSummaryPage() {
		OwnResource(MessageProducer<Runtime.IRuntimeMessage>.Instance.AddMessageListener(new MessageListener<Runtime.IRuntimeMessage>(Message => {
			Gtk.Application.Invoke(delegate
			{
				if (Message is Runtime.PeersSummaryMessage) 
				{
					labelSummaryPeers.Text = ((Runtime.PeersSummaryMessage)Message).Count.ToString();
				}
				else if (Message is Runtime.ServerSummaryMessage)
				{
					labelSummaryServer.Text = ((Runtime.ServerSummaryMessage)Message).IsRunning ? "Ok" : " Not configurable";
				}
			});
		})));
	}

	private void InitPnPPane()
	{
		Gtk.ListStore listStore = new Gtk.ListStore(typeof(Mapping), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));

		treeviewUPNP.AppendColumn("Description", new Gtk.CellRendererText(), "text", 1);
		treeviewUPNP.AppendColumn("Protocol", new Gtk.CellRendererText(), "text", 2);
		treeviewUPNP.AppendColumn("Private IP", new Gtk.CellRendererText(), "text", 3);
		treeviewUPNP.AppendColumn("Public IP", new Gtk.CellRendererText(), "text", 4);
		treeviewUPNP.AppendColumn("Private Port", new Gtk.CellRendererText(), "text", 5);
		treeviewUPNP.AppendColumn("Public Port", new Gtk.CellRendererText(), "text", 6);
		treeviewUPNP.AppendColumn("Lifetime", new Gtk.CellRendererText(), "text", 7);
		treeviewUPNP.AppendColumn("Expiration", new Gtk.CellRendererText(), "text", 8);

		treeviewUPNP.Model = listStore;
	}

	private void InitServerPane() {
		Gtk.ListStore listStoreConnected = new Gtk.ListStore (typeof (string), typeof (string));
		Gtk.ListStore listStoreMessages = new Gtk.ListStore (typeof (string), typeof (string), typeof (string), typeof (string));

		treeviewConnections.AppendColumn ("Address", new Gtk.CellRendererText (), "text", 0);
		treeviewConnections.AppendColumn ("Peer Address", new Gtk.CellRendererText (), "text", 1);

		treeviewMessages.AppendColumn ("Node", new Gtk.CellRendererText (), "text", 0);
		treeviewMessages.AppendColumn ("Direction", new Gtk.CellRendererText (), "text", 1);
		treeviewMessages.AppendColumn ("Type", new Gtk.CellRendererText (), "text", 2);
		treeviewMessages.AppendColumn ("Content", new Gtk.CellRendererText (), "text", 3);


		if (NodeTester.ServerManager.Instance.IsRunning) {
			buttonStartServer.Sensitive = false;
			buttonStopServer.Sensitive = true;
			labelServerStatus.Text = "Server Connected";
		} else {
			buttonStartServer.Sensitive = true;
			buttonStopServer.Sensitive = false;
			labelServerStatus.Text = "Server Disconnected";
		}

		OwnResource(MessageProducer<NodeTester.ServerManager.IMessage>.Instance.AddMessageListener (new MessageListener<NodeTester.ServerManager.IMessage> (ServerManagerMessage => {
			Gtk.Application.Invoke(delegate {
				bool clear = false;

				if (ServerManagerMessage is NodeTester.ServerManager.ConnectedMessage) {
					buttonStartServer.Sensitive = false;
					buttonStopServer.Sensitive = true;
					labelServerStatus.Text = "Connected";
					labelServerTab.Text = "Server (Connected)";
					clear = true;
				} else if (ServerManagerMessage is NodeTester.ServerManager.DisconnectedMessage) {
					buttonStartServer.Sensitive = true;
					buttonStopServer.Sensitive = false;
					labelServerStatus.Text = "Disconnected";
					labelServerTab.Text = "Server (Disconnected)";
					clear = true;
				} else if (ServerManagerMessage is NodeTester.ServerManager.ErrorMessage) {
					buttonStartServer.Sensitive = true;
					buttonStopServer.Sensitive = true;
					labelServerStatus.Text = "Error";
					labelServerTab.Text = "Server (Error)";
					clear = true;
				} else if (ServerManagerMessage is NodeTester.ServerManager.NodeConnectedMessage) {
					NodeTester.ServerManager.NodeInfo NodeInfo = ((NodeTester.ServerManager.NodeConnectedMessage)ServerManagerMessage).NodeInfo;

					listStoreConnected.AppendValues (NodeInfo.Address, NodeInfo.PeerAddress);
					connectionsCount++;
				} else if (ServerManagerMessage is NodeTester.ServerManager.MessageRecievedMessage) {
					NodeTester.ServerManager.MessageRecievedMessage MessageRecievedMessage = (NodeTester.ServerManager.MessageRecievedMessage)ServerManagerMessage;

					NodeTester.ServerManager.NodeInfo NodeInfo = MessageRecievedMessage.NodeInfo;
					NodeTester.ServerManager.MessageInfo MessageInfo = MessageRecievedMessage.MessageInfo;

					listStoreMessages.AppendValues (NodeInfo.PeerAddress, "Recieved", MessageInfo.Type, MessageInfo.Content);
					messagesCount++;
				} else if (ServerManagerMessage is NodeTester.ServerManager.MessageSentMessage) {
					NodeTester.ServerManager.MessageSentMessage MessageSentMessage = (NodeTester.ServerManager.MessageSentMessage)ServerManagerMessage;

					NodeTester.ServerManager.NodeInfo NodeInfo = MessageSentMessage.NodeInfo;
					NodeTester.ServerManager.MessageInfo MessageInfo = MessageSentMessage.MessageInfo;

					listStoreMessages.AppendValues (NodeInfo.PeerAddress, "Sent", MessageInfo.Type, MessageInfo.Content);
					messagesCount++;
				}

				if (clear) {
					listStoreConnected.Clear();
					listStoreMessages.Clear();
					messagesCount = 0;
					connectionsCount = 0;
				}

				labelMessages.Text = "Messages" + (messagesCount == 0 ? "" : " (" + messagesCount + ")");
				labelConnections.Text = "Connections" + (connectionsCount == 0 ? "" : " (" + connectionsCount + ")");
			});
		})));
				

		treeviewConnections.Model = listStoreConnected;
		treeviewMessages.Model = listStoreMessages;
	}

	private void InitPeersPane() {
		Gtk.ListStore listStorePeers = new Gtk.ListStore (typeof (string));
	
		treeviewPeers.AppendColumn ("Address", new Gtk.CellRendererText (), "text", 0);

		buttonDiscover.Sensitive = true;
		buttonDiscoverStop.Sensitive = false;
		labelDiscovery.Text = "";
		labelPeers.Text = "Peers";

		OwnResource(MessageProducer<NodeTester.DiscoveryManager.IMessage>.Instance.AddMessageListener (new MessageListener<NodeTester.DiscoveryManager.IMessage> (Message => {
			Gtk.Application.Invoke(delegate {
				bool clear = false;

				if (Message is NodeTester.DiscoveryManager.StartedMessage) {
					buttonDiscover.Sensitive = false;
					buttonDiscoverStop.Sensitive = true;
					labelDiscovery.Text = "Discovering...";
					labelPeers.Text = "Peers (discovering)";
					clear = true;
				} else if (Message is NodeTester.DiscoveryManager.StoppedMessage) {
					buttonDiscover.Sensitive = true;
					buttonDiscoverStop.Sensitive = false;
					labelDiscovery.Text = "Discovering Stopped";
					clear = true;
				} else if (Message is NodeTester.DiscoveryManager.DoneMessage) {
					buttonDiscover.Sensitive = true;
					buttonDiscoverStop.Sensitive = false;
					labelDiscovery.Text = "Discovering Done";
				} else if (Message is NodeTester.DiscoveryManager.ErrorMessage) {
					buttonStartServer.Sensitive = true;
					buttonStopServer.Sensitive = true;
					labelPeers.Text = "Peers (error)";
					labelDiscovery.Text = "Discovering Error";
					clear = true;
				} else if (Message is NodeTester.DiscoveryManager.PeerFoundMessage) {
					NodeTester.DiscoveryManager.NodeInfo NodeInfo = ((NodeTester.DiscoveryManager.PeerFoundMessage)Message).NodeInfo;

					listStorePeers.AppendValues (NodeInfo.Address);
					peersCount++;
					labelPeers.Text = "Peers (" + peersCount + ")";
				}

				if (clear) {
					listStorePeers.Clear();
					peersCount = 0;
				}
			});
		})));


		treeviewPeers.Model = listStorePeers;
	}

	private void InitLogTreeView(TreeView treeView) {
		Gtk.ListStore listStore = new Gtk.ListStore (typeof (string), typeof (string));

		treeView.AppendColumn ("Tag", new Gtk.CellRendererText (), "text", 0);
		treeView.AppendColumn ("Text", new Gtk.CellRendererText (), "text", 1);

		OwnResource(MessageProducer<LogMessage>.Instance.AddMessageListener (new EventLoopMessageListener<LogMessage> (LogMessage => {
			Gtk.Application.Invoke(delegate {
				listStore.AppendValues (LogMessage.Tag, LogMessage.Text);
			});
		})));

		treeView.Model = listStore;
	}

	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;

		if (consoleWindow != null)
		{
			((ResourceOwnerWindow)consoleWindow).DisposeResources();

			consoleWindow.Destroy();
		}

		DisposeResources ();
	}
		
	protected void Menu_Settings (object sender, EventArgs e)
	{
		new SettingsWindow ().Show ();
	}

	protected void Menu_Addresses (object sender, EventArgs e)
	{
		new AddressManagerEditorWindow ().Show ();
	}

	protected void Button_Discover (object sender, EventArgs e)
	{
		//DiscoveryManager.Instance.Start(this, new IPEndPoint(IPAddress.Parse("127.0.0.1"), JsonLoader<Settings>.Instance.Value.ServerPort));
		NodeTester.DiscoveryManager.Instance.Start(this, IPAddress.Parse("127.0.0.1"));
	}
		
	protected void Menu_Console (object sender, EventArgs e)
	{
		OpenConsole();
	}

	ConsoleWindow consoleWindow = null;

	private void OpenConsole()
	{
		consoleWindow = new ConsoleWindow();

		consoleWindow.DestroyEvent += (o, args) =>
		{
			consoleWindow = null;
			DisposeResources(); //WTF??
		};

		consoleWindow.Show();
	}

	protected void Button_DiscoverStop (object sender, EventArgs e)
	{
		NodeTester.DiscoveryManager.Instance.Stop ();
	}

	protected void Button_StopServer (object sender, EventArgs e)
	{
		NodeTester.ServerManager.Instance.Stop ();
	}

	protected void Button_StartServer (object sender, EventArgs e)
	{
		new AddressManagerEditorAddWindow((IPEndPoint) =>
		{
			NodeTester.ServerManager.Instance.Start(this, IPEndPoint);
		}).Show();
	}

	protected void Button_ServerTest (object sender, EventArgs e)
	{
		String testResult = NodeTester.ServerManager.GetInstance<NodeTester.ServerManager>().Test (this);

		this.ShowMessage ("Test result: " + testResult);
	}

	protected void Menu_ClearLog (object sender, EventArgs e)
	{
		((Gtk.ListStore)treeviewLog.Model).Clear ();
	}
				
	protected void Menu_SelfTest (object sender, EventArgs e)
	{
		new SelfTest ().Start ();
	}
		
	protected void Menu_CheckRemote (object sender, EventArgs e)
	{
		new AddressManagerEditorAddWindow ((IPEndPoint) => {
			String result = new RemoteServerTest().Start(IPEndPoint);
			this.ShowMessage(result);
		}).Show ();
	}

	protected async void Button_GetExternalIp_3rd(object sender, EventArgs e)
	{
		//throw new Exception();
		//try
		//{
			updateIPAddressResult(entryExternalIP3rdParty, await ExternalTestingServicesHelper.GetExternalIPAsync());
		//}
		//catch {
		
		//}
	}

	protected async void Button_GetExternalIP_UPnP(object sender, EventArgs e)
	{
		updateIPAddressResult (entryExternalIPUPnP, await NATTestsHelper.Instance.GetExternalIPAsync ());
	}

	private void updateIPAddressResult(Entry entry, IPAddress address) {
		if (address != null) {
			Gtk.Application.Invoke(delegate
			{
				entry.Text = address.ToString();

				Gdk.Color col = new Gdk.Color();
				Gdk.Color.Parse(address.IsRoutable(false) ? "green" : "red", ref col);
				entry.ModifyBase(StateType.Normal, col);

				entryCheckPort_IP.Text = address.ToString();
				entryCheckPort_IP.ModifyBase(StateType.Normal, col);
			});
		} else {
			this.ShowMessage ("Error getting ip");
		}
	}

	protected async void Button_GetUPnPMapping(object sender, EventArgs e)
	{
		((ListStore)treeviewUPNP.Model).Clear();

		IEnumerable<Mapping> mappings = await NATTestsHelper.Instance.GetAllMappingsAsync();

		if (mappings != null) {
			if (mappings.Count() == 0)
			{
				this.ShowMessage("Found 0 mappings");
			}
			foreach (Mapping mapping in mappings) {
				((ListStore)treeviewUPNP.Model).AppendValues (
					mapping,
					mapping.Description,
					mapping.Protocol.ToString (),
					mapping.PrivateIP.ToString (),
					mapping.PublicIP.ToString (),
					mapping.PrivatePort.ToString (),
					mapping.PublicPort.ToString (),
					mapping.Lifetime.ToString (),
					mapping.Expiration.ToString ()
				);
			}
		} else {
			this.ShowMessage ("Error");
		}
	}

	protected async void Button_DeviceList(object sender, EventArgs e)
	{
		int count = await NATTestsHelper.Instance.ListDevicesAsync (checkIncludePMP.Active);
		this.ShowMessage (count == -1 ? "Error getting devices" : count + " devices found");
	}

	protected async void Button_RemoveMapping(object sender, EventArgs e)
	{
		TreeIter iter;
		treeviewUPNP.Selection.GetSelected(out iter);
		Mapping Mapping = (Mapping)treeviewUPNP.Model.GetValue(iter, 0);

		bool done = await NATTestsHelper.Instance.RemoveMappingAsync (Mapping);

		this.ShowMessage (done ? "Done" : "Error");
	}

	protected async void Button_AddMapping(object sender, EventArgs e)
	{
		IPAddress InternalIPAddress;

		IPAddress[] PrivateIPs = NodeCore.Utils.GetAllLocalIPv4();

		if (PrivateIPs.Count() == 0)
		{
			this.ShowMessage("Local addresses not found", MessageType.Warning);
			return;
		}
		else {
			InternalIPAddress = PrivateIPs.First();

			if (PrivateIPs.Count() > 1)
			{
				this.ShowMessage("Found " + PrivateIPs.Count() + " internal addresses", MessageType.Warning);
			}
		}

		bool done = await NATTestsHelper.Instance.AddMappingAsync (InternalIPAddress);

		this.ShowMessage (done ? "Done" : "Error");
	}

	protected async void Button_CheckPort(object sender, EventArgs e)
	{
		bool? result = await ExternalTestingServicesHelper.CheckPortAsync(entryCheckPort_IP.Text, entryCheckPort_Port.Text);

		this.ShowMessage (result.HasValue ? (result.Value ? "Open" : "Closed") : "check failed");
	}

	protected void Menu_Configure (object sender, EventArgs e)
	{
		Runtime.Instance.Configure (this);
	}
}