/*
Copyright (c) 2021 Alex Vargas <alexsandrovp@gmail.com>

This software is provided 'as-is', without any express or implied
warranty. In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not
   claim that you wrote the original software. If you use this software
   in a product, an acknowledgment in the product documentation would be
   appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be
   misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.
*/

using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PWebServer
{
	internal class Server
	{
		Relay relay;
		StaticContent staticContent;
		HttpListener listener = new HttpListener();
		CancellationTokenSource stopper = new CancellationTokenSource();
		ServerConfig config;

		public Server(string directory) : this(directory, "+", 8080, false) { }

		public Server(string directory, string address, int port, bool https) : this(directory, new string [] { address }, port, https) { }

		public Server(string directory, string[] addresses, int port, bool https)
		{
			string protocol = https ? "https" : "http";
			foreach (string address in addresses)
				listener.Prefixes.Add($"{protocol}://{address}:{port}/");

			config = new ServerConfig(directory);

			staticContent = new StaticContent(config);
			relay = new Relay(config.Config.hosts, stopper.Token);

			Log.info($"Serving directory {config.ServedDirectory.FullName}");
			foreach (var prefix in listener.Prefixes)
				Log.info($"Listening at {prefix}");
		}

		public void Stop()
		{
			stopper.Cancel();
			listener.Stop();
		}

		public async Task Listen()
		{
			listener.Start();
			while(!stopper.IsCancellationRequested)
			{
				HttpListenerContext context;
				try { context = await listener.GetContextAsync(); }
				catch { break; } // listener stopped
				_ = Task.Run(() => respond(context), stopper.Token);
			}
		}

		private void respond(HttpListenerContext context)
		{
			if (context == null)
			{
				Log.warning("null request");
				return;
			}

			Log.debug($"{(context.Request.HttpMethod + ':').ToStringFixed(6)} {context.Request.Url.AbsolutePath}");
			foreach (var toRelay in config.Config.relay)
			{
				if (context.Request.RawUrl.StartsWith(toRelay.Key))
				{
					relay.Deflect(context, toRelay.Value).Wait();
					return;
				}
			}

			staticContent.Process(context, stopper.Token);
		}
	}
}
