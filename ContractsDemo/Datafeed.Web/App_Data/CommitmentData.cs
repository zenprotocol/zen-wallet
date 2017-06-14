using System;
namespace Datafeed.Web.App_Data
{
	public class CommitmentData
	{
		public string Id { get; set; }
		public string Time { get; set; }
		public string Ticker { get; set; }
		public decimal Value { get; set; }
		public string Data { get; set; }
		public string Proof { get; set; }
	}
}
