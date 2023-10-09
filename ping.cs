/*
 * Created by SharpDevelop.
 * User: User
 * Date: 27.09.2023
 * Time: 16:19
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Threading;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace RTSPTest
{
	/// <summary>
	/// ping ip address 
	/// </summary>
	public class PingMonitor
	{
		public class PingFailedEventArgs : EventArgs
		{
			public string ipAddress {get; set;}
			public string Reason {get; set;}
		}
		
		public event EventHandler<PingFailedEventArgs> PingFailed;
		private string ipAddress;
		private int interval;
		private Ping ping;
		private CancellationTokenSource cancellationTokenSource;
		
		public PingMonitor()
		{
			
		}

		public PingMonitor(string ipAddress, int interval)
		{
			this.ipAddress = ipAddress;
			this.interval = interval;
			ping = new Ping();
			cancellationTokenSource = new CancellationTokenSource();
		}

		public async void Start()
		{
			var token = cancellationTokenSource.Token;

			while (true)
			{
				try
				{
					var reply = await ping.SendPingAsync(ipAddress);
					if (reply.Status != IPStatus.Success)
					{
						OnPingFailed(new PingFailedEventArgs { ipAddress = ipAddress, Reason = reply.Status.ToString() });
					}
                }
				catch (Exception ex ){
                    OnPingFailed(new PingFailedEventArgs { ipAddress = ipAddress, Reason = ex.Message });
                }


				if (token.IsCancellationRequested)
				{
					break;
				}
				await Task.Delay(interval, token);
			}
		}

		public void Stop()
		{
			cancellationTokenSource.Cancel();
		}

		 protected virtual void OnPingFailed(PingFailedEventArgs e)
		{
			var handler = PingFailed;
			if (handler != null)
			{
				handler(this, e);
			}
		}
	}
	
	
}
