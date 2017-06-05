using System;
namespace ContractsDiscovery.Web.App_Data
{
    public class ContractInteraction
    {
		public string Action { get; set; }
		public string Address { get; set; }
		public string Data { get; set; }
		public string Amount { get; set; }
        public string Message { get; set; }
	}
}