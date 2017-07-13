using System;
using Wallet.core;

namespace RPC
{
	public class App
	{
		BlockChain.BlockChain _BlockChain = null;
		//Wallet.core.WalletManager _WalletManager = null;

		public App(BlockChain.BlockChain blockChain/*, WalletManager walletManager*/)
		{
			_BlockChain = blockChain;
			//_WalletManager = walletManager;
		} 

		public void SendContract(byte[] contractHash, byte[] data)
		{
			Consensus.Types.Transaction tx;
			BlockChain.ContractHelper.Execute(out tx, new BlockChain.ContractArgs()
			{
				ContractHash = contractHash,
				Message = data
			});
		}

		//public void SendRawTransaction(byte[] rawTxBytes)
		//{
		//	Consensus.Types.Transaction tx;
		//	_WalletManager.Parse(rawTxBytes, out tx);
		//	_WalletManager.Sign
		//}
	}
}
