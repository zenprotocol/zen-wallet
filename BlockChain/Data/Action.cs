using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using BlockChain.Store;
using Consensus;
using Infrastructure;
using static BlockChain.BlockVerificationHelper;

namespace BlockChain.Data
{
    public class QueueAction
    {
        //public static ITargetBlock<QueueAction> Target;
		
        public void Publish()
        {
			//Target.Post(this);
			MessageProducer<QueueAction>.Instance.PushMessage(this);
        }
    }

    public class MessageAction : QueueAction
    {
        public BlockChainMessage Message { get; set; }

        public MessageAction(BlockChainMessage message)
        {
            Message = message;
        }
    }

    public class HandleOrphansOfTxAction : QueueAction
    {
        public byte[] TxHash { get; set; }

        public HandleOrphansOfTxAction(byte[] txHash)
        {
            TxHash = txHash;
        }
    }


    public class AsyncQueueAction<TResult> : QueueAction
    {
        TaskCompletionSource<TResult> _TaskCompletion = new TaskCompletionSource<TResult>();

        public void SetResult(TResult result)
        {
            _TaskCompletion.SetResult(result);
        }

        public new Task<TResult> Publish()
        {
			//Target.Post(this);
			MessageProducer<QueueAction>.Instance.PushMessage(this);

            return _TaskCompletion.Task;
        }
    }

    public class HandleBlockAction : AsyncQueueAction<BkResult>
    {
        public byte[] BkHash { get; set; }
        public Types.Block Bk { get; set; }
        public bool IsOrphan { get; set; }

        public HandleBlockAction(byte[] bkHash, Types.Block bk, bool isOrphan)
        {
            BkHash = bkHash;
            Bk = bk;
            IsOrphan = isOrphan;
        }

        public HandleBlockAction(Types.Block bk)
        {
            BkHash = Merkle.blockHeaderHasher.Invoke(bk.header);
            Bk = bk;
            IsOrphan = false;
        }
    }

    public class HandleTransactionAction : AsyncQueueAction<BlockChain.TxResultEnum>
    {
        public Types.Transaction Tx { get; set; }
		public bool CheckInDb { get; set; }
	}

    public class GetActiveContactsAction : AsyncQueueAction<List<ACSItem>>
    {
    }

    public abstract class ContactAsyncQueueAction<T> : AsyncQueueAction<T>
    {
        public byte[] ContractHash { get; set; }

        public ContactAsyncQueueAction(byte[] contractHash)
        {
            ContractHash = contractHash;
        }
    }

    public class GetContractPointedOutputsAction : ContactAsyncQueueAction<List<Tuple<Types.Outpoint, Types.Output>>>
    {
        public GetContractPointedOutputsAction(byte[] contractHash) : base(contractHash)
        {
        }
    }

    public class GetIsContractActiveAction : ContactAsyncQueueAction<bool>
    {
        public UInt32 NextBlocks { get; set; }

        public GetIsContractActiveAction(byte[] contractHash, UInt32 nextBlocks = 0) : base(contractHash)
        {
            NextBlocks = nextBlocks;
        }
    }

    //public class GetUTXOAction : AsyncQueueAction<Types.Output>
    //{
    //	public Types.Outpoint Outpoint { get; set; }
    //	public bool IsInBlock { get; set; }
    //}

    public class GetIsConfirmedUTXOExistAction : AsyncQueueAction<bool>
    {
        public Types.Outpoint Outpoint { get; set; }
    }

    public class GetContractCodeAction : ContactAsyncQueueAction<byte[]>
    {
        public GetContractCodeAction(byte[] contractHash) : base(contractHash)
        {
        }
    }

    public class ActivateContractCodeAction : AsyncQueueAction<bool>
    {
        public string ContractCode { get; set; }
        public int Blocks { get; set; }
    }

    public class GetBlockAction : AsyncQueueAction<Types.Block>
    {
        public byte[] BkHash { get; set; }
    }

    public class GetTxAction : AsyncQueueAction<Types.Transaction>
    {
        public byte[] TxHash { get; set; }
    }

    public class ExecuteContractAction : AsyncQueueAction<Tuple<bool, Types.Transaction>>
    {
        public byte[] ContractHash { get; set; }
        public byte[] Message { get; set; }
    }

    public class GetUTXOSetAction : AsyncQueueAction<Tuple<HashDictionary<List<Types.Output>>, HashDictionary<Types.Transaction>>>
    {
        public Func<Types.Output, bool> Predicate { get; set; }
    }

#if TEST
    public class GetBlockLocationAction : AsyncQueueAction<LocationEnum>
    {
        public byte[] Block { get; set; }
    }
#endif
}
