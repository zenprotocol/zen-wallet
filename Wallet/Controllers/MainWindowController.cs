using System;
using System.Collections.Generic;
using Network;

namespace Wallet
{
	public class MainWindowController
	{
		Dictionary<string, Type> _Tabs = new Dictionary<string, Type>
		{
			{ "Wallet", typeof(WalletLayout) },
			{ "Portfolio", typeof(Portfolio) },
			{ "Contract", typeof(Contract) },
			{ "Balance", typeof(LogLayout) }
		};

		string DEFAULT_TAB = "Portfolio";

		IMainAreaView mainAreaView;
		public IMainAreaView MainAreaView { 
			set { 
				mainAreaView = value; 
				mainAreaView.Control = _Tabs[DEFAULT_TAB];
			} 
		}

		public Widgets.IStatusView StatusView { private get; set; }

		public IMainMenuView MainMenuView { 
			set { 
				value.Default = DEFAULT_TAB; 
			} 
		}

		public String MainAreaSelected { 
			set {
				mainAreaView.Control = _Tabs[value];
			}
		}

		ulong accepted = 0;
		ulong maxRequested = 0;
		ulong requested = 0;

		public IStatusMessage StatusMessage
		{
			set
			{
				string text = "";

				if (value is NodeUpnpStatusMessage)
                {
                    var _value = ((NodeUpnpStatusMessage)value).Value;
                    switch (_value)
                    {
                        case OutboundStatusEnum.Disabled:
                            text = "Inbound connectivity disabled";
                            break;
                        case OutboundStatusEnum.Initializing:
                            text = "Querying UPnP device...";
                            break;
						case OutboundStatusEnum.Accepting:
                            text = "Accepting inbound connections";
							break;
						case OutboundStatusEnum.HasValidAddress:
                            text = "Found external IP";
                            break;
                        case OutboundStatusEnum.HasInvalidAddress:
							text = "No inbound connectivity";
							break;
                    }
                    StatusView.Text2 = text;
                }
                else if (value is NodeConnectionInfoStatusMessage)
				{
					var v = ((NodeConnectionInfoStatusMessage)value).Value;
					StatusView.Text1 = $"Peers: {v.Item1}/{v.Item2}";
				}
				else if (value is BlockChainBlockNumberMessage)
				{
					if (value is BlockChainAcceptedMessage)
					{
						accepted = ((BlockChainAcceptedMessage)value).Value;
					}
					else if (value is BlockChainSyncMessage)
					{
						requested = ((BlockChainSyncMessage)value).Value;

						if (requested > maxRequested)
						{
							maxRequested = requested;
						}
					}

					if (accepted >= maxRequested)
					{
						text = $"Tip: {accepted}";
					}
                    else if (accepted > 0)
					{
                        text = $"Accepted: {accepted}/{maxRequested}, Downloaded: {maxRequested - requested}";
					}
                    else
                    {
                        text = $"Downloaded: {maxRequested - requested}/{maxRequested}";
					}

					StatusView.Text3 = text;
				}
            }
        }
    }
}

