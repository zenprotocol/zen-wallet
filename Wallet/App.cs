using System;
using BlockChain.Data;
using Infrastructure;
using Network;
using Wallet.core;

namespace Wallet
{
	public class App : Singleton<App>
	{
		public event Action OnClose;
		public MainWindow MainWindow { get; private set; }

		//TODO: remove wallet and node, remove singleton, make into resource-owner
		public WalletManager Wallet { get; private set; }
		public Network.NodeManager Node { get; private set; }
        public AssetsMetadata AssetsMetadata { get; private set; }

		ResourceOwner resources = new ResourceOwner();

		public App()
		{
			GLib.ExceptionManager.UnhandledException += (GLib.UnhandledExceptionArgs e) =>
			{
				Console.WriteLine(e.ExceptionObject as Exception);
			};

			System.Threading.Thread.GetDomain().UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
			{
				Console.WriteLine(e.ExceptionObject as Exception);
			};
		}

		public void Start(WalletManager walletManager, Network.NodeManager nodeManager)
		{
			Wallet = walletManager;
			Node = nodeManager;

            AssetsMetadata = new AssetsMetadata(Wallet);

            resources.OwnResource(MessageProducer<BlockChainMessage>.Instance.AddMessageListener(new MessageListener<BlockChainMessage>(message => {
                if (message is BlockMessage)
                {
                    StatusMessage(new BlockChainAcceptedMessage { Value = ((BlockMessage)message).BlockNumber });
                }
            })));

            resources.OwnResource(Singleton<MessageProducer<IStatusMessage>>.Instance.AddMessageListener(new MessageListener<IStatusMessage>(StatusMessage)));

			Gtk.Application.Init();

			MainWindow = new MainWindow();
			DialogBase.parent = MainWindow;
			MainWindow.Show();

            if (StatusMessageProducer._Queue != null)
            {
                lock (StatusMessageProducer._Queue)
                {
                    IStatusMessage message;
                    while (StatusMessageProducer._Queue.TryDequeue(out message))
                    {
                        StatusMessage(message);
                    }
                }
            }

			Gtk.Application.Run();
		}

        void StatusMessage(IStatusMessage message)
		{
			if (MainWindow != null)
			{
				Gtk.Application.Invoke(delegate
				{
					MainWindow.MainWindowController.StatusMessage = message;
				});
			}
			else
			{
				//TODO
				Console.WriteLine("App listener: " + message);
			}
		}

		public void Quit()
		{
            resources.Dispose();
			MainWindow.Hide();
			Gtk.Application.Quit();

			//	a.RetVal = true;
			//	Hide();

	
            if (OnClose != null)
				OnClose();
		}
	}
}