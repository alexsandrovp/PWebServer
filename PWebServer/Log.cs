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

namespace PWebServer
{
	internal static class Log
	{
		static readonly object locker = new object();

		private static void write(ConsoleColor color, string msg, params object[] args)
		{
			lock(locker)
			{
				var oldColor = Console.ForegroundColor;
				Console.ForegroundColor = color;
				try { Console.WriteLine(msg, args); }
				finally { Console.ForegroundColor = oldColor; }
			}
		}

		public static void error(string msg, params object[] args)
		{
			write(ConsoleColor.Red, msg, args);
		}

		public static void warning(string msg, params object[] args)
		{
			write(ConsoleColor.Yellow, msg, args);
		}

		public static void info(string msg, params object[] args)
		{
			write(ConsoleColor.Cyan, msg, args);
		}

		public static void debug(string msg, params object[] args)
		{
			write(ConsoleColor.White, msg, args);
		}

		public static void verbose(string msg, params object[] args)
		{
			write(ConsoleColor.DarkGray, msg, args);
		}
	}
}
