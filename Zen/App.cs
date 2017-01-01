using System;
using NBitcoin;
using Infrastructure;
using Wallet.core;
using Wallet.core;
using NBitcoinDerive;
using System.Threading;

namespace Zen
{
	public enum ModeEnum {
		Tester,
		GUI,
		Console,
	}

	public class App
	{
		private static App _Instance;

		public ModeEnum? Mode { get; set; }
		#if DEBUG
		public Boolean LanMode { get; set; }
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

			Container.Instance.Register<WalletManager2>(() => { return new WalletManager2(); });
			Container.Instance.Register<WalletManager>(() => { return new WalletManager(); });

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

		public void Start() {
			
//			if (OnNetworkChanged != null) 
//			{
//				OnNetworkChanged (JsonLoader<NBitcoinDerive.Network>.Instance.Value);
//			}

			if (!Mode.HasValue)
				return;
		

			if (Mode != null) {
				switch (Mode.Value) {
				case ModeEnum.Console:
					Console.Clear ();
					NodeManager nodeManager = new NodeManager ();

					#if DEBUG
					nodeManager.Start (LanMode);
					#else
					nodeManager.Start();
					#endif

					Console.WriteLine("Press ENTER to stop");
					Console.ReadLine();
					nodeManager.Dispose();

			//		NodeConsole.MainClass.Main(null);
					break;
				case ModeEnum.GUI:
					Wallet.App.Instance.Start();
					break;
				case ModeEnum.Tester:
			//		NodeTester.MainClass.Main(null);
					break;
				}
			}
//			_Container.Verify();
		}

		public Network Network { 
			set 
			{
				Container.Instance.Register<Network>(() => { return value; });
			} 
		}
	}
}

