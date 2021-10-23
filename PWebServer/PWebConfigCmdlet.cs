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
using System.Management.Automation;
using System.Text;

namespace PWebServer
{
	[Cmdlet(VerbsCommon.New, "PWebConfig")]
	public class PWebConfigCmdlet : PSCmdlet
	{
		readonly string defaultJson = @"{

	// use this section to configure redirection within the static server
	// for example, if your site is hosted in a subfolder and you want your user to
	// access it without typing the subfolder: localhost:8080/ => localhost:8080/subfolder/
	""redirect"": {
		//""^/$"": ""/subfolder/""
	},

	// mapping applies only if the static server can't find a particular resource
	// you shouldn't switch folders here, because that would mess with the urlreferrer
	""mapping"": {
		// ""^(/?.*)/$"": ""$1/index.html"" // default mapping always enabled
		//""^/somefolder$"": ""/somefolder/index.html"",
		//""^/js/(.*).js$"": ""/js/$1.min.js"",
		//""^/css/(.*).css$"": ""/css/$1.min.css""
	},

	// redirection applies to any request that starts with one of these keys
	""relay"": {
		//""/mybackend1"": ""myBackend"",
		//""/mybackend2"": ""myBackend""
	},

	// backend data for relay section
	""hosts"": {
		//""myBackend"": {
		//	""protocol"": ""https"",
		//	""host"": ""my.backend.com"",
		//	""port"": 443
		//}
	}
}";

		[Parameter(Position = 0, ValueFromPipeline = true)]
		public string OutputFolder { get; set; }

		protected override void ProcessRecord()
		{
			string interm = OutputFolder;
			if (string.IsNullOrEmpty(interm) || interm == ".")
				interm = this.SessionState.Path.CurrentLocation.Path;

			var outputFolder = new DirectoryInfo(interm);
			if (!outputFolder.Exists)
			{
				WriteError(new ErrorRecord(
					new Exception("folder does not exist: " + outputFolder.FullName),
					null, ErrorCategory.ResourceUnavailable, OutputFolder));
				return;
			}

			string file = Path.Combine(outputFolder.FullName, "server.json");
			if (File.Exists(file))
			{
				WriteError(new ErrorRecord(
					new Exception("file already exists: " + file),
					null, ErrorCategory.ResourceExists, file));
				return;
			}

			try
			{
				File.WriteAllText(file, defaultJson, Encoding.UTF8);
			}
			catch (UnauthorizedAccessException ex)
			{
				WriteError(new ErrorRecord(ex, null, ErrorCategory.PermissionDenied, file));
				return;
			}
			catch (Exception ex)
			{
				WriteError(new ErrorRecord(ex, null, ErrorCategory.NotSpecified, file));
				return;
			}
		}
	}
}
