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

using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PWebServer
{
	internal class ServerConfig
	{
		public DirectoryInfo AssemblyLocation { get; private set; }
		public DirectoryInfo ServedDirectory { get; private set; }
		public Dictionary<string, string> MimeTypes { get; private set; }
		public ServerConfigModel Config { get; private set; }

		public ServerConfig(string servedDirectory)
		{
			ServedDirectory = new DirectoryInfo(servedDirectory);
			if (!ServedDirectory.Exists) throw new IOException($"missing served directory {ServedDirectory.FullName}");
			AssemblyLocation = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
			if (!AssemblyLocation.Exists) throw new IOException($"missing assembly directory (?!) {AssemblyLocation.FullName}");

			string jsonFile = Path.Combine(AssemblyLocation.FullName, "mimetypes.json");
			if (!File.Exists(jsonFile)) throw new IOException($"missing {jsonFile}");
			MimeTypes = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(jsonFile));
			if (MimeTypes == null) throw new IOException($"malformed json {jsonFile}");

			string serverFile = Path.Combine(ServedDirectory.FullName, "server.json");
			if (!File.Exists(serverFile)) Config = new ServerConfigModel();
			else
			{
				Log.verbose($"Loading custom server configuration");
				Config = JsonConvert.DeserializeObject<ServerConfigModel>(File.ReadAllText(serverFile));
				if (Config == null || Config.mapping == null) throw new IOException($"malformed json {serverFile}");
			}

			List<string> toRemove = new List<string>();
			foreach(var toRelay in Config.relay)
			{
				if (!Config.hosts.ContainsKey(toRelay.Value))
				{
					Log.warning($"ignoring redirection to invalid host {toRelay.Value}");
					toRemove.Add(toRelay.Key);
				}
			}

			foreach(var key in toRemove)
			{
				Config.relay.Remove(key);
			}
		}
	}

	internal class ServerConfigModel
	{
		public Dictionary<string, string> redirect { get; set; } = new Dictionary<string, string>();
		public Dictionary<string, string> mapping { get; set; } = new Dictionary<string, string>();
		public Dictionary<string, string> relay { get; set; } = new Dictionary<string, string>();
		public Dictionary<string, HostModel> hosts { get; set; } = new Dictionary<string, HostModel>();
	}

	internal class HostModel
	{
		public string protocol { get; set; } = "http";
		public string host { get; set; }
		public int port { get; set; } = 80;
	}
}