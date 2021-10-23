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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace PWebServer
{
	internal class RelayWebSocket
	{
		string hostUrl;
		CancellationToken stopper;
		HttpListenerContext context;

		WebSocket downstreamSocket;
		ClientWebSocket upstreamSocket = new ClientWebSocket();

		Queue<byte[]> upstreamMessages = new Queue<byte[]>();
		Queue<byte[]> downstreamMessages = new Queue<byte[]>();

		public RelayWebSocket(HostModel host, HttpListenerContext context, CancellationToken stopper)
		{
			this.stopper = stopper;
			this.context = context;

			string pr = host.protocol ?? "http";
			string ho = host.host;
			int po = host.port <= 0 ? 80 : host.port;
			if (pr.EndsWith('s')) pr = "wss";
			else pr = "ws";
			hostUrl = $"{pr}://{ho}:{po}";
		}

		~RelayWebSocket()
		{
			Log.verbose($"\tWebsocket destructor {this.GetHashCode()}");
			try { upstreamSocket.Dispose(); } catch { }
			try { downstreamSocket.Dispose(); } catch { }
			try { context.Response.Close(); } catch { }
		}

		private async Task getStreams()
		{
			var uri = new Uri(hostUrl + context.Request.RawUrl);
			upstreamSocket.Options.Cookies = new CookieContainer();
			upstreamSocket.Options.KeepAliveInterval = new TimeSpan(0, 0, 30);
			upstreamSocket.Options.Cookies.SetCookies(uri, context.Request.Headers["Cookie"]);
			await upstreamSocket.ConnectAsync(uri, stopper);
			downstreamSocket = (await context.AcceptWebSocketAsync(null)).WebSocket;
		}

		public void Listen()
		{
			try
			{
				getStreams().Wait();
			}
			catch (Exception ex)
			{
				Log.error("failed to connect websocket: {0}", ex);
				context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
				return;
			}
			Task.WaitAll(
				Task.Run(readUpstream),
				Task.Run(readDownstream),
				Task.Run(writeDownstream),
				Task.Run(writeUpstream)
			);
		}

		private async Task writeDownstream()
		{
			while (!stopper.IsCancellationRequested)
			{
				if (downstreamSocket.State == WebSocketState.Closed ||
					downstreamSocket.State == WebSocketState.CloseReceived ||
					downstreamSocket.State == WebSocketState.Aborted)
				{
					try { await upstreamSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, downstreamSocket.CloseStatusDescription ?? "", stopper); }
					catch (Exception ex)
					{
						if (!stopper.IsCancellationRequested)
						{
							Log.error("{0}", ex);
						}
					}
					break;
				}

				byte[] message = null;
				lock(downstreamMessages)
				{
					if (downstreamMessages.Count > 0) message = downstreamMessages.Dequeue();
				}
				if (message != null && message.Length > 0)
				{
					try { await downstreamSocket.SendAsync(message, WebSocketMessageType.Text, true, stopper); }
					catch (Exception ex)
					{
						if (!stopper.IsCancellationRequested)
						{
							Log.error("{0}", ex);
						}
					}
				}
				else try { await Task.Delay(200, stopper); } catch { }
			}
		}

		private async Task writeUpstream()
		{
			while (!stopper.IsCancellationRequested)
			{
				if (upstreamSocket.State == WebSocketState.Closed ||
					upstreamSocket.State == WebSocketState.CloseReceived ||
					upstreamSocket.State == WebSocketState.Aborted)
				{
					try { await downstreamSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, upstreamSocket.CloseStatusDescription ?? "", stopper); }
					catch (Exception ex)
					{
						if (!stopper.IsCancellationRequested)
						{
							Log.error("{0}", ex);
						}
					}
					break;
				}

				byte[] message = null;
				lock (upstreamMessages)
				{
					if (upstreamMessages.Count > 0) message = upstreamMessages.Dequeue();
				}
				if (message != null && message.Length > 0)
				{
					try { await upstreamSocket.SendAsync(message, WebSocketMessageType.Text, true, stopper); }
					catch (Exception ex)
					{
						if (!stopper.IsCancellationRequested)
						{
							Log.error("{0}", ex);
						}
					}
				}
				else try { await Task.Delay(200, stopper); } catch { }
			}
		}

		private async Task readDownstream()
		{
			while(!stopper.IsCancellationRequested)
			{
				if (downstreamSocket.State == WebSocketState.Closed ||
					downstreamSocket.State == WebSocketState.CloseReceived ||
					downstreamSocket.State == WebSocketState.Aborted)
				{
					try { await upstreamSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", stopper); }
					catch (Exception ex)
					{
						if (!stopper.IsCancellationRequested)
						{
							Log.error("{0}", ex);
						}
					}
					break;
				}

				using var message = new MemoryStream();
				WebSocketReceiveResult result = null;
				try { result = await readSocket(downstreamSocket, message); }
				catch (Exception ex)
				{
					if (!stopper.IsCancellationRequested)
					{
						Log.error("{0}", ex);
					}
				}

				if (result == null) continue;
				if (result.MessageType == WebSocketMessageType.Close) break;
				if (message.Length > 0)
				{
					lock (upstreamMessages)
					{
						upstreamMessages.Enqueue(message.ToArray());
					}
				}
			}
		}

		private async Task readUpstream()
		{
			while (!stopper.IsCancellationRequested)
			{
				if (upstreamSocket.State == WebSocketState.Closed ||
					upstreamSocket.State == WebSocketState.CloseReceived ||
					upstreamSocket.State == WebSocketState.Aborted)
				{
					try { await downstreamSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", stopper); }
					catch (Exception ex)
					{
						if (!stopper.IsCancellationRequested)
						{
							Log.error("{0}", ex);
						}
					}
					break;
				}

				using var message = new MemoryStream();
				WebSocketReceiveResult result = null;
				try { result = await readSocket(upstreamSocket, message); }
				catch (Exception ex)
				{
					if (!stopper.IsCancellationRequested)
					{
						Log.error("{0}", ex);
					}
				}

				if (result == null) continue;
				if (result.MessageType == WebSocketMessageType.Close) break;
				if (message.Length > 0)
				{
					lock (downstreamMessages)
					{
						downstreamMessages.Enqueue(message.ToArray());
					}
				}
			}
		}

		private async Task<WebSocketReceiveResult> readSocket(WebSocket socket, MemoryStream message)
		{
			WebSocketReceiveResult result = null;
			do
			{
				var buffer = new ArraySegment<byte>(new byte[1024]);
				result = await socket.ReceiveAsync(buffer, stopper);
				message.Write(buffer.Array, buffer.Offset, result.Count);
			} while (result != null && !result.EndOfMessage);

			message.Seek(0, SeekOrigin.Begin);
			return result;
		}
	}
}
