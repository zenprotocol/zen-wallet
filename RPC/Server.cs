using System;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using RPC.Data;

namespace RPC
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

		public Server(BlockChain.BlockChain blockChain)
		{
			_App = new App(blockChain);
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
					var message = server.ReceiveFrameString();

					var sendContractPayload = JsonConvert.DeserializeObject<SendContractPayload>(message);

					try
					{
						_App.SendContract(sendContractPayload.ContractHash, sendContractPayload.Data);
				        server.SendFrame("success");
					}
					catch (Exception e)
					{
				        server.SendFrame("error");
						Console.WriteLine("error: " + e.Message);
					}
			    }
			}
		}
	}
}