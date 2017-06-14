using System;
using System.Collections.Generic;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Zen.RPC.Common;
using System.Linq;
using Wallet.core.Data;
using BlockChain.Data;

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
                _Thread.Name = "RPCListener";

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
					BasePayload request = null;
					ResultPayload response = null;

					try
					{
						var message = server.ReceiveFrameString();
						var basePayload = JsonConvert.DeserializeObject<BasePayload>(message);

						request = (BasePayload)JsonConvert.DeserializeObject(message, basePayload.Type);

						TUI.WriteColor($"{request}->", ConsoleColor.Blue);
						response = GetResult(request);
					}
					catch (Exception e)
					{
						response = new ResultPayload() { Success = false, Message = e.Message };
					}

                    if (response != null)
                    {
                        try
                        {
							TUI.WriteColor($"<-{response}", ConsoleColor.Blue);
                            server.SendFrame(JsonConvert.SerializeObject(response));
                        } catch (Exception e)
                        {
                            TUI.WriteColor($"RPCServer could not reply to a {request} payload, got exception: {e.Message}", ConsoleColor.Red);
						}
                    }
					
			    }
			}
		}

		ResultPayload GetResult(BasePayload payload)
		{
			var type = payload.Type;

			if (type == typeof(SpendPayload))
			{
				var spendPayload = (SpendPayload)payload;

				var _result = _App.Spend(new Address(spendPayload.Address), spendPayload.Amount);

				return new ResultPayload() { Success = _result };
			}

			if (type == typeof(SendContractPayload))
			{
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

			if (type == typeof(ActivateContractPayload))
			{
				var activateContractPayload = (ActivateContractPayload)payload;

				var _result = _App.ActivateTestContractCode(
					activateContractPayload.Code,
					activateContractPayload.Blocks
				);

				return new ResultPayload() { Success = _result };
			}

			if (type == typeof(GetACSPayload))
			{
				return new GetACSResultPayload() { 
					Success = true,
					Contracts = new GetActiveContactsAction().Publish().Result.Select(t => new ContractData() {
						Hash = t.Hash,
						LastBlock = t.LastBlock,
						Code = new GetContractCodeAction(t.Hash).Publish().Result
					}).ToArray() 
				};
			} 

			if (type == typeof(HelloPayload))
			{
				return new HelloResultPayload();
			}

			//if (type == typeof(GetContractCodePayload))
			//{
			//	var contractHash = ((GetContractCodePayload)payload).Hash;
   //             return new GetContractCodeResultPayload() { Success = true, Code = _App.GetContractCode(contractHash) };
			//}

			if (type == typeof(GetContractTotalAssetsPayload))
			{
				var contractHash = ((GetContractTotalAssetsPayload)payload).Hash;
			//	var totals = _App.GetTotalAssets(contractHash);
				return new GetContractTotalAssetsResultPayload() { 
					Confirmed = 999, // totals.Item1, 
					Unconfirmed = 999 // totals.Item2
				};
			}

            if (type == typeof(GetContractPointedOutputsPayload))
			{
				var _payload = (GetContractPointedOutputsPayload)payload;
				var result = new GetContractPointedOutputsAction(_payload.ContractHash).Publish().Result;

                return new GetContractPointedOutputsResultPayload
				{
                    Success = true,
                    PointedOutputs = GetContractPointedOutputsResultPayload.Pack(result)
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