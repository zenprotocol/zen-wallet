using System;
namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class MessageBox : DialogBase
	{
		public MessageBox(String message)
		{
			this.Build();

			label1.Text = message;

			buttonClose.Clicked += delegate
			{
				CloseDialog();
			};
		}
	}
}
