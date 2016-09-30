using System;

namespace Wallet
{
	public abstract class MenuBase : WidgetBase
	{
		public virtual String Selection { get; set; }

		public int Default { 
			set {
				FindChild<MenuButton> (value).Select ();
			}
		}
	}
}