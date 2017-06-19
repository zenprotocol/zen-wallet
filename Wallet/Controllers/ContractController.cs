using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Consensus;
using ContractGenerator;
using Gtk;
using Infrastructure;

namespace Wallet
{
    public class ContractActivationResult
    {
        public enum ResultEnum
        {
            Success,
            NotEnoughZen,
            Error,
        }

        public ResultEnum Result { get; set; }
        public BlockChain.BlockChain.TxResultEnum TxResult { get; set; }
    }

	public class ContractController : Singleton<ContractController>
	{
		byte[] _Code;
		byte[] _Hash;
		public ContractView ContractView { set; get; }

        Func<ulong, byte[], byte[], ContractActivationResult> ActivateContract = (kalapas, code, secureToken) =>
        {
            Types.Transaction tx;
            if (!App.Instance.Wallet.SacrificeToContract(code, kalapas, out tx, secureToken))
            {
                return new ContractActivationResult() { Result = ContractActivationResult.ResultEnum.NotEnoughZen };
            }

            var txResult = App.Instance.Node.Transmit(tx);

            if (txResult != BlockChain.BlockChain.TxResultEnum.Accepted)
            {
                return new ContractActivationResult() { Result = ContractActivationResult.ResultEnum.Error, TxResult = txResult };
            }

            return new ContractActivationResult() { Result = ContractActivationResult.ResultEnum.Success };
        };

		public void CreateOrExtend() {
			new ContractActivation().ShowDialog(_Hash, _Code, ActivateContract);
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

		public Task<ContractGenerationData> Verify(string fsCode)
		{
			return Verify(Encoding.ASCII.GetBytes(fsCode));
		}

		public Task<ContractGenerationData> Verify(byte[] fsCode) {
			return ContractMockValidationMock.Instance.Generate(fsCode);
		}
	}
}

