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
		public Action<Node, Payload> ReceivingMessageAction { get; set; }
		public Action<Node, Payload> SendingMessageAction { get; set; }

		public void OnReceivingMessage(IncomingMessage message, Action next)
		{
			ReceivingMessageAction (message.Node, message.Message.Payload);
			next ();
		}

		public void OnSendingMessage(Node node, Payload payload, Action next)
		{
			SendingMessageAction (node, payload);
			next ();
		}
	}

	public class ServerManager : NodeCore.ServerManager
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
		public class MessageRecievedMessage : IMessage { public NodeInfo NodeInfo { get; set; } public MessageInfo MessageInfo { get; set; } }
		public class MessageSentMessage : IMessage { public NodeInfo NodeInfo { get; set; } public MessageInfo MessageInfo { get; set; } }

		private LogMessageContext LogMessageContext = new LogMessageContext("Server");
		private NodeServer Server = null;

		private void PushMessage(IMessage message) {
			Infrastructure.MessageProducer<IMessage>.Instance.PushMessage (message);
		}

		protected override void InitHandlers() {
			MessagingFilter MessagingFilter = new MessagingFilter () { 
				ReceivingMessageAction = (Node, Payload) => {
					String fromNode = Node == null ? "-" : Node.RemoteSocketAddress + ":" + Node.RemoteSocketPort;

					LogMessageContext.Create ("Recieved message (" + fromNode + ") : " + Utils.GetPayloadContent(Payload));
					MessageRecievedMessage MessageRecievedMessage = new MessageRecievedMessage ();

					if (Node != null) {
						MessageRecievedMessage.NodeInfo = new NodeInfo () { 
							Address = fromNode,
							PeerAddress = Node.RemoteSocketAddress + ":" + Node.RemoteSocketPort
						};
					}

					MessageRecievedMessage.MessageInfo = new MessageInfo () {
						Type = Payload.GetType ().ToString (),
						Content = Utils.GetPayloadContent(Payload)
					};

					PushMessage (MessageRecievedMessage);
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

			Server.NodeAdded += (NodeServer sender, Node node) => {
				LogMessageContext.Create("Node connected (" + node.RemoteSocketAddress + ":" + node.RemoteSocketPort + ")");
				PushMessage (new NodeConnectedMessage () { 
					NodeInfo = new NodeInfo() { 
						Address = node.RemoteSocketAddress + ":" + node.RemoteSocketPort,
						PeerAddress = node.Peer.Endpoint.ToString()
					} 
				});

				node.Filters.Add(MessagingFilter);
			};
		}

		public String Test(IResourceOwner resourceOwner) {
			bool started = !IsRunning;
			String returValue = null;

			if (started) {
				Start (resourceOwner, IPAddress.Parse("127.0.0.1"));
			}
				
			try {				
				Node node = Node.Connect(Server.Network, Server.ExternalEndpoint);
				if (!node.IsConnected) {
					returValue = "Unable to connect";
				} else {
					node.VersionHandshake();

					if (node.State != NodeState.HandShaked) {
						returValue = "Unable to handshake";
					} else {
						node.SendMessageAsync(new GetAddrPayload());
						AddrPayload addrPayload = node.ReceiveMessage<AddrPayload>();

						if (addrPayload.Addresses.Length == NodeCore.AddressManager.Instance.GetBitcoinAddressManager().GetAddr().Length)
						{
							int matchCount = 0;

							NetworkAddress[] NetworkAddresses = NodeCore.AddressManager.Instance.GetBitcoinAddressManager().GetAddr();

							foreach(NetworkAddress NetworkAddress in NetworkAddresses) 
							{
								foreach (NetworkAddress NetworkAddress_ in addrPayload.Addresses) 
								{
									if (NetworkAddress.Endpoint.ToString() == NetworkAddress_.Endpoint.ToString()) 
									{
										matchCount++;
									}
								}
							}

							if (addrPayload.Addresses.Length == matchCount) {
								returValue = "Success";
							} else {
								returValue = "Address(es) missing";
							}
						} else {
							returValue = "Addresses count mismatch";
						}

					}
				}
			} catch (Exception e) {
				returValue = "Error: " + e.Message; 
			}

			if (started) {
				Stop ();
			}

			return returValue;
		}
	}
}

