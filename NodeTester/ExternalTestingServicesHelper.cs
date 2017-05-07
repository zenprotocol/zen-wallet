using System;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using Open.Nat;
using System.Net;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections.Specialized;
using Infrastructure;
using System.Collections;

namespace NodeTester
{
	public class ExternalTestingServicesHelper
	{
		private static LogMessageContext logMessageContext = new LogMessageContext("External Service");

		static ExternalTestingServicesHelper() {
			ServicePointManager.DefaultConnectionLimit = 1000;
			WebRequest.DefaultWebProxy = null;
		}

		public static async Task<IPAddress> GetExternalIPAsync()
		{
			logMessageContext.Create ("Getting IP");

			return await Task.Run(() =>
			{
				Task<String>[] tasks = null;

				try
				{
					tasks = new[] {
						new {IP = "91.198.22.70", DNS ="checkip.dyndns.org"}//,
						//new {IP = "74.208.43.192", DNS = "www.showmyip.com"}
					}.Select(site =>
					{
						return Task.Run(() =>
							{
								var ip = IPAddress.Parse(site.IP);

								try
								{
									//WTF - should check if ip's missing or...?
									ip = Dns.GetHostAddresses(site.DNS).First();
								}
								catch (Exception ex)
								{
									Trace.Error("GetExternalIP", ex);
								}

								WebClient client = new WebClient();
								client.Proxy = null;

								Trace.WebClient("get ip start");
								var page = client.DownloadString("http://" + ip);
								Trace.WebClient("get ip end");

								var match = Regex.Match(page, "[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}");

								logMessageContext.Create("Resolved IP using " + site.DNS);

								return match.Value;
							});
					}).ToArray();


					Task.WaitAny(tasks);

					var result = tasks.First(t => t.IsCompleted && !t.IsFaulted).Result;
					IPAddress resultIPAddress = IPAddress.Parse(result);

					String ipAdressInvestigate = Utils.IPAdressInvestigate(resultIPAddress);

					if (ipAdressInvestigate != null)
					{
						logMessageContext.Create(ipAdressInvestigate);
					}

					return resultIPAddress;
				}
				catch (InvalidOperationException)
				{
					Trace.Error("GetExternalIP", tasks.Select(t => t.Exception).FirstOrDefault());
				}
				catch (Exception e) {
					Trace.Error("GetExternalIP", e);
				}
			
				return null;
			});
		}

		public static async Task<Boolean?> CheckPortAsync(String ip, String port)
		{
			logMessageContext.Create ("Checking port (ping.eu)");

			return await Task.Run(() =>
			{
				using (WebClient client = new WebClient())
				{
					client.Proxy = null;

					var values = new NameValueCollection();
					values["port"] = port;
					values["host"] = ip;
					values["go"] = "Go";

					byte[] response;

					try
					{
						Trace.WebClient("get check start");
						response = client.UploadValues("http://ping.eu/action.php?atype=5", values);
						Trace.WebClient("get check end");
					}
					catch (Exception e)
					{
						Trace.Error("CheckPort", e);
						return null;
					}

					var responseString = Encoding.Default.GetString(response);

					return new Boolean?(responseString.IndexOf("open", StringComparison.CurrentCultureIgnoreCase) != -1);
				}
			});
		}
	}
}

