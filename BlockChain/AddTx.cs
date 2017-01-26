using System;
using System.Collections.Generic;
using Store;
using Consensus;

namespace BlockChain
{
	public class AddTx
	{
		private readonly BlockChain _BlockChain;
		private Dictionary<Types.Outpoint, InputLocationEnum> _InputLocations;
		private Keyed<Types.Transaction> _Tx;
		private TransactionContext _DbTx;
		private readonly List<Action> _DoActions;
		private readonly List<Action> _UndoActions;

		private enum InputLocationEnum
		{
			Mempool,
			TxStore,
		}

		public enum Result
		{
			Added,
			AddedOrphan,
			Rejected
		}

		public AddTx(
			BlockChain blockChain, 
       		TransactionContext dbTx, 
			Keyed<Types.Transaction> tx,
			List<Action> doActions,
			List<Action> undoActions
		)
		{
			_DoActions = doActions;
			_UndoActions = undoActions;
			_BlockChain = blockChain;
			_Tx = tx;
			_DbTx = dbTx;
			_InputLocations = new Dictionary<Types.Outpoint, InputLocationEnum>();
		}

		public Result Start()
		{
			if (!IsValid() || IsInMempool() || IsInTxStore())
			{
				BlockChainTrace.Information("not valid/in mempool/in txstore");
				return Result.Rejected;
			}

			if (IsMempoolContainsSpendingInput())
			{
				BlockChainTrace.Information("Mempool contains spending input");
				return Result.Rejected;
			}

			if (IsOrphan())
			{
				BlockChainTrace.Information("Added as orphan");
				_BlockChain.TxMempool.Add(_Tx, true);
				return Result.AddedOrphan;
			}

			//TODO: 5. For each input, if the referenced transaction is coinbase, reject if it has fewer than COINBASE_MATURITY confirmations.

			if (!IsValidInputs())
			{
				BlockChainTrace.Information("invalid inputs");
				return Result.Rejected;
			}

			if (!IsValidPointedInputs())
			{
				BlockChainTrace.Information("invalid pointed inputs");
				return Result.Rejected;
			}

			//TODO: 7. Apply fee rules. If fails, reject
			//TODO: 8. Validate each input. If fails, reject

			BlockChainTrace.Information("Transaction added to mempool");
			_BlockChain.TxMempool.Add(_Tx);
			_DoActions.Add(() => TxAddedMessage.Publish(_Tx, false));
			_UndoActions.Add(() => _BlockChain.TxMempool.Remove(_Tx.Key));

			foreach (var transaction in _BlockChain.TxMempool.GetOrphansOf(_Tx)) 
			{
				BlockChainTrace.Information("Start with orphan");
				new AddTx(
					_BlockChain,
					_DbTx,
					_Tx,
					_DoActions,
					_UndoActions
				).Start();
			}

			return Result.Added;
		}

		private bool IsValid()
		{
			return true;
		}

		private bool IsInMempool()
		{
			var result = _BlockChain.TxMempool.ContainsKey(_Tx.Key);

			BlockChainTrace.Information($"IsInMempool = {result}");

			return result;
		}

		private bool IsInTxStore()
		{
			var result = _BlockChain.BlockStore.TxStore.ContainsKey(_DbTx, _Tx.Key);

			BlockChainTrace.Information($"IsInTxStore = {result}");

			return result;
		}

		private bool IsMempoolContainsSpendingInput()
		{
			foreach (var inputs in _Tx.Value.inputs)
			{
				if (_BlockChain.TxMempool.ContainsInputs(_Tx))
				{
					return true;
				}
			}

			return false;
		}

		private bool IsOrphan()
		{
			foreach (var input in _Tx.Value.inputs)
			{
				if (_BlockChain.TxMempool.ContainsKey(input.txHash))
				{
					_InputLocations[input] = InputLocationEnum.Mempool;
				}
				else if (_BlockChain.BlockStore.TxStore.ContainsKey(_DbTx, input.txHash))
				{
					_InputLocations[input] = InputLocationEnum.TxStore;
				}
				else {
					return true;
				}
			}

			return false;
		}

		private bool IsValidInputs()
		{
			foreach (var input in _Tx.Value.inputs)
			{
				if (!ParentOutputExists(input))
				{
					BlockChainTrace.Information("parent output does not exist");
					return false;
				}

				if (ParentOutputSpent(input))
				{
					BlockChainTrace.Information("parent output spent");
					return false;
				}
			}

			return true;
		}

		private bool IsValidPointedInputs()
		{
//			var pointedTransaction = Consensus.TransactionValidation.toPointedTransaction (_Tx, _Tx.Value.inputs);
//
//			for (int i=0; i<_Tx.Value.inputs.Length; i++)
//			{
//				if (!Consensus.TransactionValidation.validateAtIndex (pointedTransaction, i))
//					return false;
//			}

			return true;
		}

		private bool ParentOutputExists(Types.Outpoint input)
		{
			Keyed<Types.Transaction> parentTransaction = null;

			switch (_InputLocations[input])
			{
				case InputLocationEnum.Mempool:
					parentTransaction = _BlockChain.TxMempool.Get(input.txHash);
					break;
				case InputLocationEnum.TxStore:
					parentTransaction = _BlockChain.BlockStore.TxStore.Get(_DbTx, input.txHash);
					break;
			}

			if (input.index >= parentTransaction.Value.outputs.Length)
			{
				BlockChainTrace.Information("Input index not found");
				return false;
			}

			return true;
		}

		private bool ParentOutputSpent(Types.Outpoint input)
		{
			byte[] newArray = new byte[input.txHash.Length + 1];
			input.txHash.CopyTo(newArray, 0);
			newArray[input.txHash.Length] = (byte)input.index;

			if (_InputLocations[input] == InputLocationEnum.TxStore && !_BlockChain.UTXOStore.ContainsKey(_DbTx, newArray))
			{
				BlockChainTrace.Information("Output has been spent");
				return true;
			}

			return false;
		}
	}
}