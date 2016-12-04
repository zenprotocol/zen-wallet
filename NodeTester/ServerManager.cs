using System;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using System.Net;
using NBitcoin.Protocol.Filters;
using Infrastructure;
using NodeCore;

namespace NodeTester
{
	class MessagingFilter : INodeFilter 
	{
		public Action<Node, IncomingMessage> ReceivingMessageAction { get; set; }
		public Action<Node, Object> SendingMessageAction { get; set; }

		public void OnReceivingMessage(IncomingMessage message, Action next)
		{
			ReceivingMessageAction (message.Node, message);
			next ();
		}

		public void OnSendingMessage(Node node, Object payload, Action next)
		{
			SendingMessageAction (node, payload);
			next ();
		}
	}

	public class ServerManager : Singleton<ServerManager>
	{
		public struct NodeInfo {
			public String Address;
			public String PeerAddress;
		}

		public struct MessageInfo {
			public String Type;
			public String Content;
		}

		public interface IMessage {}

		public class ConnectedMessage : IMessage {}
		public class DisconnectedMessage : IMessage {}
		public class ErrorMessage : IMessage {}
		public class NodeConnectedMessage : IMessage { public NodeInfo NodeInfo { get; set; } }
		public class MessageReceivedMessage : IMessage { public NodeInfo NodeInfo { get; set; } public MessageInfo MessageInfo { get; set; } }
		public class MessageSentMessage : IMessage { public NodeInfo NodeInfo { get; set; } public MessageInfo MessageInfo { get; set; } }

		private LogMessageContext LogMessageContext = new LogMessageContext("Server");

		private Server _Server = null;

		private void PushMessage(IMessage message) {
			Infrastructure.MessageProducer<IMessage>.Instance.PushMessage (message);
		}

		public bool IsListening { 
			get {
				return _Server != null && _Server.IsListening;
			}
		}

		private void InitHandlers() {
			MessagingFilter MessagingFilter = new MessagingFilter () { 
				ReceivingMessageAction = (Node, Payload) => {
					String fromNode = Node == null ? "-" : Node.RemoteSocketAddress + ":" + Node.RemoteSocketPort;

					LogMessageContext.Create ("Received message (" + fromNode + ") : " + Utils.GetPayloadContent(Payload));
					MessageReceivedMessage MessageReceivedMessage = new MessageReceivedMessage ();

					if (Node != null) {
						MessageReceivedMessage.NodeInfo = new NodeInfo () { 
							Address = fromNode,
							PeerAddress = Node.RemoteSocketAddress + ":" + Node.RemoteSocketPort
						};
					}

					MessageReceivedMessage.MessageInfo = new MessageInfo () {
						Type = Payload.GetType ().ToString (),
						Content = Utils.GetPayloadContent(Payload)
					};

					PushMessage (MessageReceivedMessage);
				},
				SendingMessageAction = (Node, Payload) => {
					String toNode = Node == null ? "-" : Node.RemoteSocketAddress + ":" + Node.RemoteSocketPort;

					LogMessageContext.Create ("Sending message (" + toNode + ") : " + Utils.GetPayloadContent(Payload));
					MessageSentMessage MessageSentMessage = new MessageSentMessage ();

					if (Node != null) {
						MessageSentMessage.NodeInfo = new NodeInfo () { 
							Address = toNode,
							PeerAddress = Node.RemoteSocketAddress + ":" + Node.RemoteSocketPort
						};
					}

					MessageSentMessage.MessageInfo = new MessageInfo () {
						Type = Payload.GetType ().ToString (),
						Content = Utils.GetPayloadContent(Payload)
					};

					PushMessage (MessageSentMessage);
				},
			};

			_Server.OnNodeAdded((sender, node) => {
				LogMessageContext.Create("Node connected (" + node.RemoteSocketAddress + ":" + node.RemoteSocketPort + ")");
				PushMessage (new NodeConnectedMessage () { 
					NodeInfo = new NodeInfo() { 
						Address = node.RemoteSocketAddress + ":" + node.RemoteSocketPort,
						PeerAddress = node.Peer.Endpoint.ToString()
					} 
				});

				node.Filters.Add(MessagingFilter);
			});
		}

		public void Start(IResourceOwner resourceOwner, IPAddress ExternalAddress)
		{
			Start(resourceOwner, new IPEndPoint(ExternalAddress, JsonLoader<Settings>.Instance.Value.ServerPort));
		}

		public void Start (IResourceOwner resourceOwner, IPEndPoint externalEndpoint)
		{
			if (IsListening) {
				Stop ();
			}

			NBitcoin.Network network = TestNetwork.Instance;

			_Server = new Server(resourceOwner, externalEndpoint, network);
			WalletManager.Instance.Setup(_Server.Behaviors);


			InitHandlers();

			if (_Server.Start())
			{
				PushMessage(new ConnectedMessage());
				Trace.Information("Server " + (_Server.IsListening ? "listening" : "not listening"));
			}
		}

		public void Stop() {
			if (_Server != null) {
				PushMessage(new DisconnectedMessage());

				Trace.Information ("Server Stopped");
				_Server.Stop ();
				_Server = null;
			}
		}

		//public String Test(IResourceOwner resourceOwner) {
		//	bool started = !IsListening;
		//	String returValue = null;

		//	if (started) {
		//		Start (resourceOwner, IPAddress.Parse("127.0.0.1"));
		//	}
				
		//	try {				
		//		Node node = Node.Connect(_Server.Network, _Server.ExternalEndpoint);
		//		if (!node.IsConnected) {
		//			returValue = "Unable to connect";
		//		} else {
		//			node.VersionHandshake();

		//			if (node.State != NodeState.HandShaked) {
		//				returValue = "Unable to handshake";
		//			} else {
		//				node.SendMessageAsync(new GetAddrPayload());
		//				AddrPayload addrPayload = node.ReceiveMessage<AddrPayload>();

		//				if (addrPayload.Addresses.Length == NodeCore.AddressManager.Instance.GetBitcoinAddressManager().GetAddr().Length)
		//				{
		//					int matchCount = 0;

		//					NetworkAddress[] NetworkAddresses = NodeCore.AddressManager.Instance.GetBitcoinAddressManager().GetAddr();

		//					foreach(NetworkAddress networkAddress in NetworkAddresses) 
		//					{
		//						foreach (NetworkAddress NetworkAddress_ in addrPayload.Addresses) 
		//						{
		//							if (networkAddress.Endpoint.ToString() == NetworkAddress_.Endpoint.ToString()) 
		//							{
		//								matchCount++;
		//							}
		//						}
		//					}

		//					if (addrPayload.Addresses.Length == matchCount) {
		//						returValue = "Success";
		//					} else {
		//						returValue = "Address(es) missing";
		//					}
		//				} else {
		//					returValue = "Addresses count mismatch";
		//				}

		//			}
		//		}
		//	} catch (Exception e) {
		//		returValue = "Error: " + e.Message; 
		//	}

		//	if (started) {
		//		Stop ();
		//	}

		//	return returValue;
		//}
	}
}

