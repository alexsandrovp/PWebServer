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
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace PWebServer
{
	internal class CookieEx
	{
		public string Name { get; set; } = null;
		public string Value { get; set; } = null;
		public string Domain { get; set; } = null;
		public string Path { get; set; } = null;
		public string SameSite { get; set; } = null;
		public DateTime? Expires { get; set; } = null;
		public int MaxAge { get; set; } = -1;
		public bool Secure { get; set; } = false;
		public bool HttpOnly { get; set; } = false;

		private CookieEx() { }
		public CookieEx(Cookie cookie, int maxAge = -1, string sameSite = null)
		{
			Name = cookie.Name;
			Value = cookie.Value;
			MaxAge = maxAge;
			SameSite = sameSite;
			Expires = cookie.Expires.Year > 1 ? cookie.Expires : null;
			Path = cookie.Path;
			Domain = cookie.Domain;
			Secure = cookie.Secure;
			HttpOnly = cookie.HttpOnly;
		}

		public CookieEx(string setCookie)
		{
			var props = setCookie.Split(';').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
			if (props.Length <= 0) throw new Exception("invalid set-cookie string");

			string prop = props[0];
			int pos = prop.IndexOf('=');
			string key = (pos >= 0 ? prop.Substring(0, pos) : prop).Trim();
			string val = (pos >= 0 ? prop.Substring(pos + 1) : "").Trim();
			Name = key;
			Value = val;
			for (int i = 1; i < props.Length; ++i)
			{
				prop = props[i];
				pos = prop.IndexOf('=');
				key = (pos >= 0 ? prop.Substring(0, pos) : prop).Trim();
				val = (pos >= 0 ? prop.Substring(pos + 1) : "").Trim();
				switch (key.ToLower())
				{
					case "max-age":
						{
							int j = 0;
							if (int.TryParse(val, out j)) MaxAge = j;
						}
						break;
					case "expires":
						{
							DateTime dt;
							if (DateTime.TryParse(val, out dt)) Expires = dt.ToUniversalTime();
						}
						break;
					case "path":
						if (val.Length > 0) Path = val;
						break;
					case "httponly":
						HttpOnly = true;
						break;
					case "secure":
						Secure = true;
						break;
					case "domain":
						if (val.Length > 0) Domain = val;
						break;
					case "samesite":
						if (val.Length > 0) SameSite = val;
						break;
				}
			}
		}

		public override string ToString()
		{
			return Name + '=' + Value;
		}

		public string ToSetCookie()
		{
			StringBuilder sb = new StringBuilder(500);
			sb.AppendFormat("{0}={1}", Name, Value);
			if (MaxAge >= 0) sb.AppendFormat("; Max-Age={0}", MaxAge);
			if (Expires != null) sb.AppendFormat("; Expires={0}", Expires.Value.ToString("R"));
			if (!string.IsNullOrWhiteSpace(Domain)) sb.AppendFormat("; Domain={0}", Domain);
			if (!string.IsNullOrWhiteSpace(Path)) sb.AppendFormat("; Path={0}", Path);
			if (HttpOnly) sb.Append("; HttpOnly");
			if (Secure) sb.Append("; Secure");
			if (!string.IsNullOrWhiteSpace(SameSite)) sb.AppendFormat("; SameSite={0}", SameSite);
			return sb.ToString();
		}

		public static string ReplaceDomain(string setCookie, string newDomain)
		{
			return Regex.Replace(setCookie, @"(Domain\s*=.*?)(;|$)", $"Domain={newDomain}$2", RegexOptions.IgnoreCase);
		}

		public static CookieEx[] FromMSSetCookie(string msSetCookie, string newDomain = null)
		{
			return SplitMSSetCookie(msSetCookie, newDomain).Select(c => new CookieEx(c)).ToArray();
		}

		public static string[] SplitMSSetCookie(string msSetCookie, string newDomain = null)
		{
			var tokens = msSetCookie.Split(';', StringSplitOptions.RemoveEmptyEntries);

			string cookie = null;
			bool newCookie = true;
			List<string> cookies = new List<string>();
			foreach (var token in tokens)
			{
				if (newCookie)
				{
					newCookie = false;
					if (cookie != null) cookies.Add(cookie);
					cookie = token;
				}
				else
				{
					string t = token.Trim();
					string check = t.ToLower();
					int commaCount = 0, posComma1 = -1, posComma2 = -1;
					for (int i = 0; i < token.Length; ++i)
					{
						if (token[i] == ',')
						{
							commaCount++;
							if (commaCount == 2)
							{
								posComma2 = i;
								break;
							}
							posComma1 = i;
						}
					}
					if (commaCount == 0) cookie += ";" + token;
					else if (check.StartsWith("expires"))
					{
						if (commaCount == 2)
						{
							string expires = token.Substring(0, posComma2);
							cookie += ";" + expires;
							string temp = token.Substring(posComma2 + 1);
							if (!string.IsNullOrWhiteSpace(cookie)) cookies.Add(cookie);
							cookie = temp;
						}
						else cookie += ";" + token;
					}
					else
					{
						string temp = token.Substring(0, posComma1);
						cookie += ";" + temp;
						if (!string.IsNullOrWhiteSpace(cookie)) cookies.Add(cookie);
						temp = token.Substring(posComma1 + 1);
						cookie = temp;
					}
				}
			}
			if (!string.IsNullOrWhiteSpace(cookie)) cookies.Add(cookie);
			if (newDomain == null) return cookies.ToArray();
		
			return cookies.Select(s => ReplaceDomain(s, newDomain)).ToArray();
		}
	}
}
