using System;
using System.Threading.Tasks;
using Consensus;
using Gtk;

namespace Wallet
{
    //TODO: delete
	[System.ComponentModel.ToolboxItem(true)]
	public partial class SendRaw : DialogBase
	{
		Types.Transaction _Tx = null;

		public SendRaw()
		{
			this.Build();

			CloseControl = eventboxCancel;

			textviewRawTx.Buffer.Changed += textviewRawTx_Changed;

            eventboxSend.ButtonReleaseEvent += async delegate
			{
				if (_Tx == null)
					return;

				var txResultEnum = await Task.Run(() => App.Instance.Node.Transmit(_Tx));
				
                Gtk.Application.Invoke(delegate {
					switch (txResultEnum)
					{
						case BlockChain.BlockChain.TxResultEnum.Accepted:
							CloseDialog();
							break;
						default:
							labelStatus.Text = txResultEnum.ToString();
							break;
					}
				});
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
