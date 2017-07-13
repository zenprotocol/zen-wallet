using System;
using Sodium;
using Wallet.core.Data;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class SignMessageDialog : DialogBase
	{
		public SignMessageDialog(Key key)
		{
			this.Build();

            textview1.Buffer.Text = Convert.ToBase64String(key.Public);

			buttonSign.Clicked += delegate
			{
                try
                {
                    var message = Convert.FromBase64String(textview1.Buffer.Text);
                    var signed = PublicKeyAuth.SignDetached(message, key.Private);
                    textview1.Buffer.Text = Convert.ToBase64String(signed);
                } catch (Exception e)
                {
                    textview1.Buffer.Text = "Error: " + e.Message;
                }
            };
		}
	}
}
