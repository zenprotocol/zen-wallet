using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Consensus;
using Infrastructure;

namespace Miner
{
    public class Hasher : IDisposable
    {
		readonly CancellationTokenSource _cancel = new CancellationTokenSource();
		ManualResetEvent continueEvent = new ManualResetEvent(true);

		public uint Difficulty { get; set; }
        Types.BlockHeader _Header = null;

        public event Action OnMined;

        public Hasher()
		{
			Task.Factory.StartNew(Main, TaskCreationOptions.LongRunning);
		}

		public void Dispose()
		{
			_cancel.Cancel();
            Continue();
		}

		public void Pause(string reason)
		{
			MinerTrace.Information("Hasher paused: " + reason);
			continueEvent.Reset();
		}

		public void Continue()
		{
			MinerTrace.Information("Hasher resumed");
			continueEvent.Set();
		}

		public void SetHeader(Types.BlockHeader header)
        {
            //Pause("setting header");
            _Header = header;
			var random = new Random();
			random.NextBytes(_Header.nonce);


            //Continue();
		}

        void Main()
        {
            MinerTrace.Information("Hasher started");

            while (true)
            {
                if (_Header == null)
                    Pause("no header");
                
				continueEvent.WaitOne();

				if (_cancel.IsCancellationRequested)
				{
					break;
				}

				var time = DateTime.Now.ToUniversalTime();

				var bkHash = Merkle.blockHeaderHasher.Invoke(_Header);

				var c = 0;

				if (Difficulty != 0)
				{
					var bits = new BitArray(bkHash);
					var len = bits.Length - 1;
					for (var i = 0; i < len; i++)
						if (!bits[len - i])
							c++;
						else
							break;
				}

				if (c >= Difficulty)
				{
					MinerTrace.Information($"Hasher solved a {Difficulty} difficulty");
                    if (OnMined != null)
                        OnMined();
                }
				else
				{
                    //TODO: just increment the nonce, no need to ramdon again
					var random = new Random();
					random.NextBytes(_Header.nonce);
				}
            }

			MinerTrace.Information("Hasher stopped");
        }
    }
}
