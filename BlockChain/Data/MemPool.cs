namespace BlockChain.Data
{
	public class MemPool
	{
		public readonly ICTxPool ICTxPool = new ICTxPool();
		public readonly TxPool TxPool = new TxPool();
		public readonly OrphanTxPool OrphanTxPool = new OrphanTxPool();
		public readonly ContractPool ContractPool = new ContractPool();

		public MemPool()
		{
			//ICTxPool.TxPool = TxPool;
			ICTxPool.ContractPool = ContractPool;
			ICTxPool.OrphanTxPool = OrphanTxPool;

			TxPool.ICTxPool = ICTxPool;
			TxPool.ContractPool = ContractPool;
			TxPool.OrphanTxPool = OrphanTxPool;
		}
	}
}