namespace BlockChain.Data
{
	public class ContractPool : HashDictionary<ContractsPoolItem>
	{
		readonly HashDictionary<byte[]> _Txs = new HashDictionary<byte[]>();

		public void AddRef(byte[] txHash, ACSItem acsItem)
		{
			if (!ContainsKey(acsItem.Hash)) this[acsItem.Hash] = ContractsPoolItem.FromACSItem(acsItem);

			var contractsPoolItem = this[acsItem.Hash];

			contractsPoolItem.Refs++;

			_Txs[txHash] = acsItem.Hash;
		}

		public new void Remove(byte[] txHash)
		{
			if (!_Txs.ContainsKey(txHash))
				return;

			var contractHash = _Txs[txHash];
			var contract = this[contractHash];

			_Txs.Remove(txHash);
			contract.Refs--;

			if (contract.Refs == 0)
			{
				base.Remove(contractHash);
			}
		}
	}
}
