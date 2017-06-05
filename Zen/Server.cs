using System;
using System.Collections.Generic;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Zen.RPC.Common;
using System.Linq;
using Wallet.core.Data;

namespace Zen
{
	public class Server
	{
		readonly int PORT = 5555;
		readonly int TIMEOUT = 2 * 1000;

		bool _Started = false;
		bool _Stopping = false;
		Thread _Thread;
		object _Sync = new object();
		App _App;

		public Server(App app)
		{
			_App = app;
		}

		public void Start()
		{
			lock (_Sync)
			{
				if (_Started)
					return;

				_Thread = new Thread(Listen);

				_Thread.Start();
				_Started = true;
				_Stopping = false;
			}
		}

		public void Stop()
		{
			lock (_Sync)
			{
				_Stopping = true;
				_Thread.Join(TIMEOUT);
				_Started = false;
			}
		}

		void Listen()
		{
			using (var server = new ResponseSocket())
			{
				server.Bind($"tcp://*:{PORT}");

			    while (!_Stopping)
			    {
					ResultPayload resultPayload = null;

					try
					{
						var message = server.ReceiveFrameString();
						var basePayload = JsonConvert.DeserializeObject<BasePayload>(message);

						Console.WriteLine("RPCServer got a " + basePayload.Type + " request");

						var payload = (BasePayload)JsonConvert.DeserializeObject(message, basePayload.Type);

						resultPayload = GetResult(payload);
					}
					catch (Exception e)
					{
						resultPayload = new ResultPayload() { Success = false, Message = e.Message };
					}
					finally
					{
						if (resultPayload != null)
						{
							Console.WriteLine("RPCServer reply: " + (resultPayload.Success ? "Succeess" : "Failure") + " message: " + resultPayload.Message);
							server.SendFrame(JsonConvert.SerializeObject(resultPayload));
						}
					}
			    }
			}
		}

		ResultPayload GetResult(BasePayload payload)
		{
			var type = payload.Type;

			if (type == typeof(SendContractPayload))
			{
				var result = new ResultPayload();
				var sendContractPayload = (SendContractPayload)payload;

                Consensus.Types.Transaction autoTx;

                if (!_App.WalletManager.SendContract(sendContractPayload.ContractHash, sendContractPayload.Data, out autoTx))
                {
                    return new ResultPayload() { Success = false };
                }

				BlockChain.BlockChain.TxResultEnum transmitResult;
				if (!_App.Transmit(autoTx, out transmitResult))
				{
					return new ResultPayload() { Success = false, Message = transmitResult.ToString() };
				}

				return new ResultPayload() { Success = true };
			} 

			if (type == typeof(GetACSPayload))
			{
				return new GetACSResultPayload() { 
					Success = true,
					Contracts = _App.GetActiveContacts().Select(t => new ContractData() {
						Hash = t.Hash,
						LastBlock = t.LastBlock,
						Code = _App.GetContractCode(t.Hash),

					}).ToArray() 
				};
			} 

			if (type == typeof(HelloPayload))
			{
				return new HelloResultPayload();
			}

			if (type == typeof(GetContractCodePayload))
			{
				var contractHash = ((GetContractCodePayload)payload).Hash;
				return new GetContractCodeResultPayload() { Code = _App.GetContractCode(contractHash) };
			}

			if (type == typeof(GetContractTotalAssetsPayload))
			{
				var contractHash = ((GetContractTotalAssetsPayload)payload).Hash;
				var totals = _App.GetTotalAssets(contractHash);
				return new GetContractTotalAssetsResultPayload() { 
					Confirmed = totals.Item1, 
					Unconfirmed = totals.Item2
				};
			}

			if (type == typeof(GetOutpointPayload))
			{
				var _payload = (GetOutpointPayload)payload;
				var address = _payload.Address;
				var result = _App.FindOutpoint(new Address(address, _payload.IsContract ? AddressType.Contract : AddressType.PK ), _payload.Asset);

				return result == null ? new GetOutpointResultPayload() : new GetOutpointResultPayload()
				{
                    Success = true,
                    Index = result.index,
                    TXHash = result.txHash,
				};
			}

			if (type == typeof(GetPointedOutpointPayload))
			{
				var _payload = (GetPointedOutpointPayload)payload;
				var address = _payload.Address;
				var result = _App.FindPointedOutpoint(new Address(address, _payload.IsContract ? AddressType.Contract : AddressType.PK), _payload.Asset);

				return result == null ? new GetPointedOutpointResultPayload() : new GetPointedOutpointResultPayload()
				{
					Success = true,
					Outpoint = result.Item1,
                    Output = result.Item2,
				};
			}

			if (type == typeof(MakeTransactionPayload))
			{
				var _payload = (MakeTransactionPayload)payload;
				var result = _App.Spend(new Address(_payload.Address), _payload.Amount);

				return new ResultPayload()
				{
					Success = result
				};
			}
			    
			return null;
		}
	}
}