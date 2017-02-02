//using System;
//using Infrastructure;
//using Wallet.core;

//namespace Wallet
//{
//	public class WalletEventManager : Singleton<WalletEventManager>
//	{
//		public UpdateInfo UpdateInfo { get; private set; }

//		public WalletEventManager()
//		{
//			MessageProducer<UpdateInfo>.Instance.AddMessageListener(new EventLoopMessageListener<UpdateInfo>(u => {
//				UpdateInfo = u;
//			}));
//		}
//	}
//}
