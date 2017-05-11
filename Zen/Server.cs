using System;
using System.Collections.Generic;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Zen.RPC.Common;
using System.Linq;

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
					var message = server.ReceiveFrameString();
					var basePayload = JsonConvert.DeserializeObject<BasePayload>(message);

					try
					{
						if (basePayload.Type == typeof(SendContractPayload))
						{
							var sendContractPayload = JsonConvert.DeserializeObject<SendContractPayload>(message);
							_App.SendContract(sendContractPayload.ContractHash, sendContractPayload.Data);
							server.SendFrame(JsonConvert.SerializeObject(new ResultPayload() { Success = true }));
						} 
						else if (basePayload.Type == typeof(GetACSPayload))
						{
							server.SendFrame(JsonConvert.SerializeObject(new GetACSResultPayload() { Contracts = _App.GetActiveContacts().ToArray() }));
						} 
						else if (basePayload.Type == typeof(HelloPayload))
						{
							server.SendFrame(JsonConvert.SerializeObject(new HelloResultPayload()));
						}
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