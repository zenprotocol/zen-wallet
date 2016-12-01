using System;
using System.Threading;

namespace Wallet
{
	public class ContractController
	{
		private static ContractController instance = null;

		public ContractView ContractView { set; get; }

		public static ContractController GetInstance() {
			if (instance == null) {
				instance = new ContractController ();
			}

			return instance;
		}

		public void Create() {
			ContractView.ContractCodeAssertion = "ContractCodeAssertion: Create clicked";
			ContractView.ContractCodeContent = "ContractCodeContent: Create clicked";
			ContractView.ContractCodeHash = "ContractCodeHash: Create clicked";
			ContractView.Proof = "Proof: Create clicked";
		}

		public void Load() {
			ContractView.ContractCodeAssertion = "ContractCodeAssertion: Load clicked";
			ContractView.ContractCodeContent = "ContractCodeContent: Load clicked";
			ContractView.ContractCodeHash = "ContractCodeHash: Load clicked";
			ContractView.Proof = "Proof: Load clicked";
		}

		public void Verify() {
			ContractView.ContractCodeAssertion = "ContractCodeAssertion: Verify clicked";
			ContractView.ContractCodeContent = "ContractCodeContent: Verify clicked";
			ContractView.ContractCodeHash = "ContractCodeHash: Verify clicked";
			ContractView.Proof = "Proof: Verify clicked";
		}
	}
}

