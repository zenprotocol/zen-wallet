using System;
using Consensus;
using Gtk;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class SendRaw : DialogBase
	{
		Types.Transaction _Tx = null;

		public SendRaw()
		{
			this.Build();

			CloseControl = eventboxCancel;

			textviewRawTx.Buffer.Changed += textviewRawTx_Changed;

			eventboxSend.ButtonReleaseEvent += (object o, ButtonReleaseEventArgs args) =>
			{
				if (_Tx == null)
					return;

				var txResultEnum = App.Instance.Node.Transmit(_Tx);
				switch (txResultEnum)
				{
					case BlockChain.BlockChain.TxResultEnum.Accepted:
						CloseDialog();
						break;
					default:
						labelStatus.Text = txResultEnum.ToString();
						break;
				}
			};
		}

		void textviewRawTx_Changed(object sender, EventArgs e)
		{
			var text = ((TextBuffer)sender).Text;
			byte[] txBytes = null;

			try
			{
				txBytes  = Array.ConvertAll<string, byte>(text.Split('-'), s => Convert.ToByte(s, 16));
				labelStatus.Text = "";
			}
			catch
			{
				txBytes = null;
				labelStatus.Text = "Invalid input";
			}

			if (!App.Instance.Wallet.Parse(txBytes, out _Tx))
			{
				labelStatus.Text = "Invalid tx data";
			}
		}
	}
}
