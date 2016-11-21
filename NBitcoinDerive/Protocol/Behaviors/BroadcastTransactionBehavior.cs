using System;
namespace NBitcoinDerive
{
	public partial class BroadcastTransactionBehavior : Gtk.ActionGroup
	{
		public BroadcastTransactionBehavior() :
				base("NBitcoinDerive.BroadcastTransactionBehavior")
		{
			this.Build();
		}
	}
}
