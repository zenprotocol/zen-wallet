using System;
using System.Threading;

namespace Wallet
{
	public class MainController
	{
		private static MainController instance = null;

		private DataModel dataModel = new DataModel();
		private IListener listener;
		private Thread tempThread;
		private bool stopping = false;

		public static MainController GetInstance() {
			if (instance == null) {
				instance = new MainController ();
			}

			return instance;
		}

		public MainController ()
		{
			tempThread = new Thread (Reset);
			tempThread.Start ();
		}

		public void AddListener(IListener listener) {
			this.listener = listener;
		}

		public void TestMethod() {
			dataModel.DecimalOne = 999;
			dataModel.DecimalTwo = 444;

			if (listener != null) {
				listener.UpdateUI (dataModel);
			}
		}

		public void Quit() {
			stopping = true;
			tempThread.Join ();
		}

		private void Reset() {
			Random random = new Random();

			while (!stopping) {
				dataModel.DecimalOne = random.Next(1, 13);
				dataModel.DecimalTwo = random.Next(1, 7);

				if (listener != null) {
					//Alternative: Runtime.DispatchService.GuiDispatch (new StatefulMessageHandler (UpdateGui), n);
					listener.UpdateUI (dataModel);
				}

				Thread.Sleep(1000);
			}
		}
	}
}

