using System;
using NBitcoin;
using Infrastructure;
using NBitcoinDerive;
using System.Threading;
using Wallet.core;

namespace Zen
{
	public enum AppModeEnum {
		Tester,
		GUI,
		Console,
	}

	public class App
	{
		private static App _Instance;

		public AppModeEnum? Mode { get; set; }
		#if DEBUG
		public Boolean LanMode { get; set; }
		public Boolean DisableInboundMode { get; set; }
		#endif

		private static readonly object _lock = new object();

		public static App Instance {
			get {
				lock (_lock)
				{
					_Instance = _Instance ?? new App();
					return _Instance;
				}
			}
		}
			
		private App ()
		{
			Mode = null;


	////		Container.Instance.Register<WalletManager2>(() => { return new WalletManager2(); });
	//		Container.Instance.Register<WalletManager>(() => { return new WalletManager(); });

	//		Container.Instance.Register<ITest, Test>();
			// 2. Configure the container (register)
		//	_Container.Register<IUserRepository, SqlUserRepository>(Lifestyle.Transient);

		//	_Container.Register<ILogger, MailLogger>(Lifestyle.Singleton);

			// 3. Optionally verify the container's configuration.
	//		_Container.Verify();

			// 4. Register the container as MVC3 IDependencyResolver.
		//	DependencyResolver.SetResolver(new SimpleInjectorDependencyResolver(container));	


		}
	
		//public event Action<NBitcoinDerive.Network> OnNetworkChanged;

		public void Start(bool clearConsole = true) {
			
//			if (OnNetworkChanged != null) 
//			{
//				OnNetworkChanged (JsonLoader<NBitcoinDerive.Network>.Instance.Value);
//			}

			if (!Mode.HasValue)
				return;

			if (clearConsole)
				Console.Clear ();

			var blockchain = new BlockChain.BlockChain ("blockchain_db");
			var walletManager = new WalletManager (blockchain);
			var nodeManager = new NodeManager (blockchain);
			#if DEBUG
			nodeManager.Start (LanMode, DisableInboundMode);
			#else
			nodeManager.Start();
			#endif

			if (Mode != null) {
				switch (Mode.Value) {
				case AppModeEnum.Console:
					Console.WriteLine("Press ENTER to stop");
					Console.ReadLine();
					nodeManager.Dispose();
					break;
				case AppModeEnum.GUI:
					Wallet.App.Instance.Start(nodeManager, walletManager);
					break;
				case AppModeEnum.Tester:
					NodeTester.MainClass.Main(nodeManager, walletManager);
					break;
				}
			}
//			_Container.Verify();
		}

//		public Network Network { 
//			set 
//			{
//				Container.Instance.Register<Network>(() => { return value; });
//			} 
//		}
	}
}

