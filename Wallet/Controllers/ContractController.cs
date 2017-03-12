using System.IO;
using System.Text;
using Gtk;
using Infrastructure;

namespace Wallet
{
	public class ContractController : Singleton<ContractController>
	{
		byte[] _Code;
		byte[] _Hash;
		public ContractView ContractView { set; get; }

		public void CreateOrExtend() {
			new ContractActivation().ShowDialog(_Hash, _Code);
		}

		public void UpdateContractInfo(string contractText)
		{
			_Code = Encoding.ASCII.GetBytes(contractText);
			_Hash = Consensus.Merkle.innerHash(Encoding.ASCII.GetBytes(contractText));

			ContractView.Hash = _Hash;
			ContractView.IsActive = App.Instance.Wallet.IsContractActive(_Hash);
		}

		public void Save()
		{
			var filechooser = new FileChooserDialog("Choose contract file",
				(Window)MainAreaController.Instance.MainView,
                FileChooserAction.Save,
				"Cancel", ResponseType.Cancel,
				"Save", ResponseType.Accept);

			if (filechooser.Run() == (int)ResponseType.Accept)
			{
				File.WriteAllText(filechooser.Filename, ContractView.Code);
			}

			filechooser.Destroy();
		}

		public void Load() {
			var filechooser = new FileChooserDialog("Choose contract file",
				(Window)MainAreaController.Instance.MainView,
				FileChooserAction.Open,
				"Cancel", ResponseType.Cancel,
				"Open", ResponseType.Accept);

			if (filechooser.Run() == (int)ResponseType.Accept)
			{
				ContractView.Code = File.ReadAllText(filechooser.Filename);
			}

			filechooser.Destroy();
		}

		public void Verify() {
		}

	}
}

