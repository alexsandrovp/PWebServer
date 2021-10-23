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
using System.Reflection;
using System.Text.RegularExpressions;

namespace PWebServer
{
	public static class Utils
	{
		public static string GetAssemblyFileVersion()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			return assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
		}

		public static string ToStringFixed(this string s, int minLength)
		{
			string ret = s ?? "";
			for (int i = ret.Length; i < minLength; ++i) ret += ' ';
			return ret;
		}

		public static string JoinPath(this string s, params string[] paths)
		{
			string res = s ?? "";
			foreach(string path in paths)
			{
				if (!string.IsNullOrEmpty(path))
				{
					bool endsWith = res.EndsWith(Path.DirectorySeparatorChar) || res.EndsWith(Path.AltDirectorySeparatorChar);
					bool startsWith = path.StartsWith(Path.DirectorySeparatorChar) || path.StartsWith(Path.AltDirectorySeparatorChar);
					if (endsWith && startsWith) res += path.TrimStart(Path.DirectorySeparatorChar).TrimStart(Path.AltDirectorySeparatorChar);
					else if (!endsWith && !startsWith) res += Path.DirectorySeparatorChar + path;
					else res += path;
				}
			}
			return res;
		}

		public static string ToUrl(this string s)
		{
			if (string.IsNullOrEmpty(s)) return s;
			return s.Replace('\\', '/');
		}

		public static string ToHostOnly(this string url)
		{
			Regex rgx = new Regex(@"(\S*:\/\/)?(\w*)(:.*)?");
			//Regex rgx = new Regex(@"(\S*:\/\/)?(\w*)(:\d*)?(.*)?");
			var match = rgx.Match(url);
			if (match.Success) return match.Groups[2].Value;
			return "";
		}

		public static string ReplaceProtocolHostPort(this string url, string protocol, string host, string port)
		{
			string ret = url ?? "";
			int pos = ret.IndexOf("://");
			if (pos >= 0)
			{
				ret = ret.Substring(pos + 3);
				if (protocol != null) ret = protocol + "://" + ret;
			}
			int hostStartPos = ret.IndexOf("://");
			if (hostStartPos < 0) hostStartPos = 0;
			else hostStartPos += 3;

			pos = ret.IndexOf(':', hostStartPos);
			if (pos >= 0)
			{
				string part1 = ret.Substring(0, hostStartPos);
				string part3 = ret.Substring(pos);
				Regex rgx = new Regex(@":(\d+)(.*)");
				var match = rgx.Match(part3);
				if (match.Success) part3 = string.IsNullOrWhiteSpace(port) ? match.Result("$2") : match.Result($":{port}$2");
				ret = part1 + host + part3;
			}
			else
			{
				pos = ret.IndexOfAny(new char[] { '/', '?' }, hostStartPos);
				string part1 = ret.Substring(0, hostStartPos);
				string part3 = pos >= 0 ? ret.Substring(pos) : "";
				if (string.IsNullOrWhiteSpace(port))
					ret = part1 + host + part3;
				else ret = part1 + host + $":{port}" + part3;
			}

			return ret;
		}
	}
}
