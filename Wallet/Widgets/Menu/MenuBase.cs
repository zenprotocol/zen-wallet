using System;

namespace Wallet
{
	public abstract class MenuBase : WidgetBase
	{
		public virtual MenuButton Selection { get; set; }
	}
}