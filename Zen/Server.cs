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
using System.Threading.Tasks;
using System.Text;

namespace Zen
{
	public class Server
	{
		readonly int PORT = 5555;

		App _App;
		NetMQPoller _poller = new NetMQPoller();
		ResponseSocket _responseSocket = new ResponseSocket();

		public Server(App app)
		{
			_App = app;

			_poller = new NetMQPoller();
			_responseSocket = new ResponseSocket();
			_responseSocket.ReceiveReady += OnReceiveReady;
			_poller.Add(_responseSocket);
		}

		public void Start()
		{
			Console.WriteLine("RPC Server starting...");
			_responseSocket.Bind($"tcp://*:{PORT}");

			Task.Factory.StartNew(() => _poller.Run(),
			  TaskCreationOptions.LongRunning);
		}

		public void Stop()
		{
			_poller.Stop();
		}

		async void OnReceiveReady(object sender, NetMQSocketEventArgs e)
		{
			BasePayload request = null;
			ResultPayload response = null;

			try
			{
				var message = _responseSocket.ReceiveFrameString();
				var basePayload = JsonConvert.DeserializeObject<BasePayload>(message);

				request = (BasePayload)JsonConvert.DeserializeObject(message, basePayload.Type);

				TUI.WriteColor($"{request}->", ConsoleColor.Blue);
				response = await GetResult(request);
			}
			catch (Exception ex)
			{
				response = new ResultPayload() { Success = false, Message = ex.Message };
			}

			if (response != null)
			{
				try
				{
					TUI.WriteColor($"<-{response}", ConsoleColor.Blue);
					_responseSocket.SendFrame(JsonConvert.SerializeObject(response));
				}
				catch (Exception ex)
				{
					TUI.WriteColor($"RPCServer could not reply to a {request} payload, got exception: {ex.Message}", ConsoleColor.Red);
				}
			}

		}

		async Task<ResultPayload> GetResult(BasePayload payload)
		{
			var type = payload.Type;

			if (type == typeof(SpendPayload))
			{
				var spendPayload = (SpendPayload)payload;

				var _result = await _App.Spend(new Address(spendPayload.Address), spendPayload.Amount);

				return new ResultPayload { Success = _result };
			}

			if (type == typeof(SendContractPayload))
			{
				var sendContractPayload = (SendContractPayload)payload;

				var result = await new ExecuteContractAction()
				{
					ContractHash = sendContractPayload.ContractHash,
					Message = sendContractPayload.Data
				}.Publish();

				if (!result.Item1)
				{
					return new SendContractResultPayload { Success = false };
				}

                var transmitResult = await _App.NodeManager.Transmit(result.Item2);

				if (transmitResult != BlockChain.BlockChain.TxResultEnum.Accepted)
				{
					return new SendContractResultPayload { Success = false, Message = transmitResult.ToString() };
				}

				return new SendContractResultPayload { Success = true, TxHash = Consensus.Merkle.transactionHasher.Invoke(result.Item2) };
			}

			if (type == typeof(ActivateContractPayload))
			{
				var activateContractPayload = (ActivateContractPayload)payload;

				var amount = (ulong)BlockChain.ActiveContractSet.KalapasPerBlock(activateContractPayload.Code) * (ulong)activateContractPayload.Blocks;

                Consensus.Types.Transaction tx;
                var success = _App.WalletManager.GetContractActivationTx(Encoding.ASCII.GetBytes(activateContractPayload.Code), amount, out tx);

				var resultPayload = new ResultPayload();

				if (!success)
				{
					resultPayload.Message = "Could not get signed tx";
					resultPayload.Success = false;
				}
				else
				{
                    var transmitResult = await _App.NodeManager.Transmit(tx);

					if (transmitResult != BlockChain.BlockChain.TxResultEnum.Accepted)
					{
						resultPayload.Message = "Could not transmit. Result: " + transmitResult;
						resultPayload.Success = false;
					}
					else
					{
						resultPayload.Success = true;
					}
				}

				return resultPayload;
			}

			if (type == typeof(GetACSPayload))
			{
				return new GetACSResultPayload()
				{
					Success = true,
					Contracts = new GetActiveContactsAction().Publish().Result.Select(t => new ContractData()
					{
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
			//  var contractHash = ((GetContractCodePayload)payload).Hash;
			//             return new GetContractCodeResultPayload() { Success = true, Code = _App.GetContractCode(contractHash) };
			//}

			if (type == typeof(GetContractTotalAssetsPayload))
			{
				var contractHash = ((GetContractTotalAssetsPayload)payload).Hash;
				//  var totals = _App.GetTotalAssets(contractHash);
				return new GetContractTotalAssetsResultPayload
				{
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
				var result = await _App.Spend(new Address(_payload.Address), _payload.Amount);

				return new ResultPayload
				{
					Success = result
				};
			}

			if (type == typeof(EnsureTestKeyAcquiredPayload))
			{
				var privateKey = ((EnsureTestKeyAcquiredPayload)payload).PrivateKey;

				if (!_App.TestKeyImported(privateKey))
				{
                    _App.WalletManager.Import(Key.Create(privateKey));
				}

				return new ResultPayload
				{
					Success = _App.TestKeyImported(privateKey)
				};
			}

			//if (type == typeof(GetBalancePayload))
			//{
			//  var _payload = (GetBalancePayload)payload;

			//  return new GetBalanceResultPayload
			//  {
			//      Success = true,
			//                 Balance = _App.CalcBalance(_payload.Asset)
			//  };
			//}

			return null;
		}
	}
}