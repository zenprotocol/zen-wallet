using System;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Zen.RPC.Common;

namespace Zen.RPC
{
    public class Client
    {
		//public static string Address { get; set; }

        public static async Task<T> Send<T>(string address, BasePayload message)
        {
            using (var client = new RequestSocket())
            {
             //   try
             //   {
				client.Connect($"tcp://{address}");
                //} catch (Exception e)
                //{
                //    return new ResultPayload() { Success = false, Message = "Error connecting to RPC server:" + e.Message };
                //}

              //  try
             //   {
                    client.SendFrame(JsonConvert.SerializeObject(message));
                //}
                //catch (Exception e)
                //{
                //    return new ResultPayload() { Success = false, Message = "Error during send:" + e.Message };
                //} 

           //     try
           //     {
                    var basePayload = JsonConvert.DeserializeObject<T>(client.ReceiveFrameString());
                    return basePayload;
                    //if (basePayload.Type == typeof(HelloResultPayload))
                    //{
                    //    var sendContractPayload = JsonConvert.DeserializeObject<SendContractPayload>(message);
                    //    _App.SendContract(sendContractPayload.ContractHash, sendContractPayload.Data);
                    //    server.SendFrame(JsonConvert.SerializeObject(new ResultPayload() { Success = true }));
                    //}
                    //else if (basePayload.Type == typeof(GetACSPayload))
                    //{
                    //    server.SendFrame(JsonConvert.SerializeObject(new GetACSResultPayload() { Contracts = _App.GetActiveContracts().ToArray() }));
                    //}
                    //else if (basePayload.Type == typeof(HelloPayload))
                    //{
                    //    server.SendFrame(JsonConvert.SerializeObject(new HelloResultPayload()));
                    //}

                    //return result is ResultPayload ? (ResultPayload)result : new ResultPayload() { Success = false };
                //}
                //catch (Exception e)
                //{
                //    return new ResultPayload() { Success = false, Message = "Error during receive:" + e.Message };
                //}
            }
        }
    }
}
