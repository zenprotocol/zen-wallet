using System;
using Infrastructure;

namespace Wallet
{
	public class SendDialogController : Singleton<SendDialogController>
	{
		private ISendDialogView sendDialogView; 

		public ISendDialogView SendDialogView { 
			set { 
				sendDialogView = value; 

			} 
		}

		public void Spend()
		{

		}
	}
}

