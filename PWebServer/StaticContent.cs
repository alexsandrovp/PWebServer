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
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace PWebServer
{
	internal class StaticContent
	{
		ServerConfig config;

		public StaticContent(ServerConfig config)
		{
			this.config = config;
		}

		/// <summary>
		/// You HAVE TO set context.Response.StatusCode BEFORE starting to write to context.Response.OutputStream
		/// Check stopper.IsCancellationRequested periodically to know if you have to abort
		/// </summary>
		/// <param name="context"></param>
		/// <returns>http status</returns>
		public virtual HttpStatusCode Process(HttpListenerContext context, CancellationToken stopper)
		{
			HttpStatusCode status = HttpStatusCode.NotFound;
			try
			{
				if (redirect(context))
				{
					return HttpStatusCode.PermanentRedirect;
				}

				string subPath = context.Request.Url.AbsolutePath;
				if (subPath.Length == 0) subPath += "/index.html";
				else if (subPath.EndsWith('/')) subPath += "index.html";

				string file = Path.Combine(config.ServedDirectory.FullName, subPath.TrimStart('/'));

				bool found = File.Exists(file);
				if (found) status = HttpStatusCode.OK;
				else
				{
					found = findMappings(subPath, out file);
					if (found) status = HttpStatusCode.OK;
				}

				context.Response.StatusCode = (int)status;

				//now we can write to the output stream
				if (found && !stopper.IsCancellationRequested)
				{
					string ext = Path.GetExtension(file).ToLower();
					if (config.MimeTypes.ContainsKey(ext))
						context.Response.ContentType = config.MimeTypes[ext];
					else context.Response.ContentType = "application/octet-stream";
					using var input = File.OpenRead(file);
					input.CopyToAsync(context.Response.OutputStream, stopper).Wait();
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex);
				status = HttpStatusCode.InternalServerError;
			}
			finally
			{
				try { context.Response.Close(); }
				catch { }
			}
			return status;
		}

		private bool redirect(HttpListenerContext context)
		{
			foreach (var rgxStr in config.Config.redirect.Keys)
			{
				var rgx = new Regex(rgxStr);
				var match = rgx.Match(context.Request.Url.AbsolutePath);
				if (match.Success)
				{
					string mapped = match.Result(config.Config.redirect[rgxStr]);
					context.Response.RedirectLocation = mapped;
					context.Response.StatusCode = (int)HttpStatusCode.PermanentRedirect;
					return true;
				}
			}
			return false;
		}

		private bool findMappings(string subPath, out string file)
		{
			file = null;
			string mapped = subPath;
			foreach(var rgxStr in config.Config.mapping.Keys)
			{
				var rgx = new Regex(rgxStr);
				var match = rgx.Match(mapped);
				if (match.Success)
				{
					mapped = match.Result(config.Config.mapping[rgxStr]);
					var fi = new FileInfo(Path.Combine(config.ServedDirectory.FullName, mapped.TrimStart('/')));
					file = fi.FullName;
					return File.Exists(file);
				}
			}
			return false;
		}
	}
}
