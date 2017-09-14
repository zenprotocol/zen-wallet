using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BlockChain;
using BlockChain.Data;
using Consensus;
using ContractGenerator;
using Gtk;
using Infrastructure;
using Wallet.core.Data;
using Wallet.Widgets.Contract;

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

	public class ContractController
	{
		byte[] _Code;
		byte[] _Hash;

		IContractView _ContractView;

		public ContractController(IContractView contractView)
		{
			_ContractView = contractView;
		}

        public Task<ContractActivationResult> ActivateContract(ulong kalapas, byte[] code, byte[] secureToken)
        {
            return Task.Run(() =>
	        {
	            Types.Transaction tx;


	            if (!App.Instance.Wallet.GetContractActivationTx(code, kalapas, out tx, secureToken))
	            {
                    return new ContractActivationResult() { Result = ContractActivationResult.ResultEnum.NotEnoughZen };
	            }

                var txResult = App.Instance.Node.Transmit(tx).Result;

	            if (txResult != BlockChain.BlockChain.TxResultEnum.Accepted)
	            {
	                return new ContractActivationResult() { Result = ContractActivationResult.ResultEnum.Error, TxResult = txResult };
	            }

	            return new ContractActivationResult() { Result = ContractActivationResult.ResultEnum.Success };
	        });
        }
                                  
		public void UpdateContractInfo()
		{
            var contractCode = _ContractView.Code;

            _Code = Encoding.ASCII.GetBytes(contractCode);

			_Hash = Merkle.innerHash(Encoding.ASCII.GetBytes(contractCode));

			_ContractView.Hash = _Hash;
            _ContractView.CostPerBlock = ActiveContractSet.KalapasPerBlock(contractCode);

			//new GetIsContractActiveAction(_Hash).Publish().Result
            // var isActive = new GetIsContractActiveAction(_Hash).Publish();
            //_ContractView.IsActive = ;
		}

		public void Save()
		{
			var filechooser = new FileChooserDialog("Choose contract file",
				App.Instance.MainWindow,
				FileChooserAction.Save,
				"Cancel", ResponseType.Cancel,
				"Save", ResponseType.Accept);

			if (filechooser.Run() == (int)ResponseType.Accept)
			{
				File.WriteAllText(filechooser.Filename, _ContractView.Code);
			}

			filechooser.Destroy();
		}

		public void Load()
		{
			var filechooser = new FileChooserDialog("Choose contract file",
				App.Instance.MainWindow,
				FileChooserAction.Open,
				"Cancel", ResponseType.Cancel,
				"Open", ResponseType.Accept);

			if (filechooser.Run() == (int)ResponseType.Accept)
			{
				_ContractView.Code = File.ReadAllText(filechooser.Filename);
			}

			filechooser.Destroy();
		}

		public Task<ContractGenerationData> Verify(string fsCode)
		{
			return Verify(Encoding.ASCII.GetBytes(fsCode));
		}

		public Task<ContractGenerationData> Verify(byte[] fsCode)
		{
			return ContractMockValidationMock.Instance.Generate(fsCode);
		}
	}
}

