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
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PWebServer
{
	internal class Relay
	{
		CancellationToken stopper;
		Dictionary<string, HostModel> hosts;

		public Relay(Dictionary<string, HostModel> hosts, CancellationToken stopper)
		{
			this.stopper = stopper;
			this.hosts = hosts;
		}

		public async Task Deflect(HttpListenerContext context, string hostId)
		{
			var host = hosts[hostId];
			if (context.Request.IsWebSocketRequest)
			{
				_ = Task.Run(new RelayWebSocket(host, context, stopper).Listen);
				return;
			}

			string targetProtocol = host.protocol ?? "http";
			string targetHost = host.host;
			int targetPort = host.port <= 0 ? 80 : host.port;

			string url = $"{targetProtocol}://{targetHost}:{targetPort}".JoinPath(context.Request.RawUrl).ToUrl();
			HttpWebRequest relayRequest = WebRequest.CreateHttp(url);
			//relayRequest.KeepAlive = true; //TODO: necessary?
			relayRequest.UserAgent = context.Request.UserAgent;
			relayRequest.CookieContainer = new CookieContainer();
			relayRequest.Method = context.Request.HttpMethod;
			relayRequest.ContentType = context.Request.ContentType;
			relayRequest.ContentLength = context.Request.ContentLength64;
			//relayRequest.Proxy.Credentials = CredentialCache.DefaultCredentials;

			//copy request headers
			for (int i = 0; i < context.Request.Headers.Count; ++i)
			{
				string name = context.Request.Headers.GetKey(i) ?? "";
				if (!string.IsNullOrWhiteSpace(name))
				{
					string[] values = context.Request.Headers.GetValues(i) ?? new string[0];
					if (values.Length == 1)
					{
						switch (name.ToLower())
						{
							case "host":
							case "origin":
							case "referer":
								relayRequest.Headers[name] = values[0].ReplaceProtocolHostPort(targetProtocol, targetHost, targetPort > 0 ? targetPort.ToString() : null);
								break;
							default:
								relayRequest.Headers[name] = values[0];
								break;
						}
					}
					else
					{
						Log.warning($"more than one header value\n\t{context.Request.RawUrl}\n\tName: {name}\n\tcount: {values.Length}");
					}
				}
			}

			Task copyTask = null;
			if (context.Request.ContentLength64 > 0)
			{
				try { copyTask = context.Request.InputStream.CopyToAsync(relayRequest.GetRequestStream(), stopper); }
				catch { }
			}
			
			HttpWebResponse wresp = null;
			try
			{
				wresp = await relayRequest.GetResponseAsync() as HttpWebResponse;
				if (copyTask != null) try { await copyTask; } catch { };
				
				//TODO: //context.Response.ContentEncoding = wresp.ContentEncoding;
				context.Response.ContentType = wresp.ContentType;
				if (wresp.ContentLength >= 0) context.Response.ContentLength64 = wresp.ContentLength;
				context.Response.StatusDescription = wresp.StatusDescription;
				copyResponseHeaders(wresp, context);
				context.Response.StatusCode = (int)wresp.StatusCode;
				await wresp.GetResponseStream().CopyToAsync(context.Response.OutputStream, stopper);
			}
			catch (WebException ex)
			{
				if (copyTask != null) try { await copyTask; } catch { };

				var errorResponse = ex.Response as HttpWebResponse;
				if (errorResponse == null)
				{
					context.Response.StatusDescription = ex.Message;
					context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
				}
				else
				{
					copyErrorResponseHeaders(errorResponse, context);
					context.Response.StatusDescription = errorResponse.StatusDescription;
					context.Response.StatusCode = (int)errorResponse.StatusCode;
				}
			}
			catch(Exception ex)
			{
				Log.error("{0}", ex);
				context.Response.StatusCode = (int)(wresp.StatusCode == HttpStatusCode.OK ? HttpStatusCode.InternalServerError : wresp.StatusCode);
			}
			finally
			{
				try { context.Response.Close(); }
				catch { }
			}
		}

		internal static void copyErrorResponseHeaders(HttpWebResponse wresp, HttpListenerContext context)
		{
			int i;
			string requestDomain = context.Request.Headers["Host"].ToHostOnly();
			//copy response headers
			for (i = 0; i < wresp.Headers.Count; ++i)
			{
				string name = wresp.Headers.GetKey(i) ?? "";
				if (!string.IsNullOrWhiteSpace(name))
				{
					bool isSetCookie = name.ToLower() == "set-cookie";
					string[] values = wresp.Headers.GetValues(i) ?? new string[0];
					foreach (var val in values)
					{
						if (isSetCookie)
						{
							/*
							if (wresp.StatusCode == HttpStatusCode.Unauthorized)
							{
								foreach (Cookie cookie in context.Request.Cookies)
									context.Response.AppendHeader(name, $"{cookie.Name}=;path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT");
							}
							*/
							var cookies = CookieEx.FromMSSetCookie(val, requestDomain);
							foreach (var cookie in cookies)
							{
								//cookie.Value = "";
								string ck = cookie.ToSetCookie();
								context.Response.AppendHeader(name, ck);
							}
						}
						//else context.Response.AppendHeader(name, val); // for some reason, I cannot set all headers...
					}
				}
			}
		}

		internal static void copyResponseHeaders(HttpWebResponse wresp, HttpListenerContext context)
		{
			int i;
			//copy response headers
			for (i = 0; i < wresp.Headers.Count; ++i)
			{
				string name = wresp.Headers.GetKey(i) ?? "";
				if (!string.IsNullOrWhiteSpace(name))
				{
					if (name.ToLower() == "set-cookie") continue;
					string[] values = wresp.Headers.GetValues(i) ?? new string[0];
					foreach (var val in values)
						context.Response.AppendHeader(name, val);
				}
			}

			// copy cookies
			string requestDomain = context.Request.Headers["Host"].ToHostOnly();
			foreach (Cookie cookie in wresp.Cookies)
			{
				cookie.Domain = requestDomain;
				context.Response.AppendHeader("Set-Cookie", new CookieEx(cookie).ToSetCookie());
			}
		}

		internal static List<KeyValuePair<string, string>> getRequestHeaders(HttpListenerContext context,
			string replaceProtocol, string replaceHost, int replacePort)
		{
			List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
			for (int i = 0; i < context.Request.Headers.Count; ++i)
			{
				string name = context.Request.Headers.GetKey(i) ?? "";
				if (!string.IsNullOrWhiteSpace(name))
				{
					string val = context.Request.Headers[name];
					switch (name.ToLower())
					{
						case "host":
						case "origin":
						case "referer":
							result.Add(new KeyValuePair<string, string>(name, val.ReplaceProtocolHostPort(replaceProtocol, replaceHost, replacePort > 0 ? replacePort.ToString() : null)));
							break;
						default:
							result.Add(new KeyValuePair<string, string>(name, val));
							break;
					}
				}
			}
			return result;
		}
	}
}
