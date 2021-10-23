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

using System.IO;
using System.Management.Automation;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Threading.Tasks;

namespace PWebServer
{
	[Cmdlet(VerbsLifecycle.Start, "PWebServer")]
	public class PWebServerCmdlet : PSCmdlet
	{
		Server server;
		Task serverTask;

		[Parameter(Position = 0, Mandatory = false)]
		public DirectoryInfo Folder { get; set; }

		[Parameter(Mandatory = false)]
		public int Port { get; set; } = -1;

		[Parameter(Mandatory = false)]
		public SwitchParameter Secure { get; set; } = false;

		[Parameter(Mandatory = false)]
		public string ListenAt { get; set; }

		[Parameter(Mandatory = false)]
		public SwitchParameter ShowIps { get; set; } = false;

		public PWebServerCmdlet() { }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Ignore")]
		static bool IsElevated
		{
			get
			{
				try { return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator); } catch { }
				return false;
			}
		}

		protected override void BeginProcessing()
		{
			bool elevated = IsElevated;

			QuickEdit.Disable();

			Log.info($"PWebServer version {Utils.GetAssemblyFileVersion()}");

			if (ShowIps)
			{
				Log.verbose("Network addresses");
				foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
				{
					//Console.WriteLine("Name: " + netInterface.Name);
					//Console.WriteLine("Description: " + netInterface.Description);
					//Console.WriteLine("Addresses: ");
					var ipProps = netInterface.GetIPProperties();
					foreach (var addr in ipProps.UnicastAddresses)
						Log.verbose($"{addr.Address}");
				}
				Log.verbose("");
			}
			
			if (ListenAt == null)
			{
				if (elevated) ListenAt = "+";
				else ListenAt = "localhost";
			}
			if (Port < 0)
			{
				int defaultPort = elevated ? Secure ? 443 : 80 : 8080;
				Port = defaultPort;
			}
			string psDir = this.SessionState.Path.CurrentFileSystemLocation.Path;
			Directory.SetCurrentDirectory(psDir);
			if (Folder == null) Folder = new DirectoryInfo(psDir);
			else if (!Path.IsPathRooted(Folder.ToString()))
				Folder = new DirectoryInfo(Path.Combine(psDir, Folder.ToString()));
			server = new Server(Folder.FullName, ListenAt, Port, Secure);
			serverTask = server.Listen();
		}

		protected override void EndProcessing()
		{
			serverTask.Wait();
			Log.debug("\nGood bye\n");
		}

		protected override void StopProcessing()
		{
			server.Stop();
		}

		protected override void ProcessRecord()
		{
		}
	}
}
