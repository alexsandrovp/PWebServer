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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PWebServer;

namespace UnitTests
{
	[TestClass]
	public class TestUtils
	{
		void testReplaceProtocolHostPort(string protocol, string host, string port)
		{
			string check = string.IsNullOrWhiteSpace(port) ? $"{host}" : $"{host}:{port}";
			Assert.AreEqual("localhost".ReplaceProtocolHostPort(protocol, host, port), check);
			Assert.AreEqual("localhost.com".ReplaceProtocolHostPort(protocol, host, port), check);
			Assert.AreEqual("localhost/".ReplaceProtocolHostPort(protocol, host, port), $"{check}/");
			Assert.AreEqual("localhost/what".ReplaceProtocolHostPort(protocol, host, port), $"{check}/what");
			Assert.AreEqual("localhost/what?prop=123".ReplaceProtocolHostPort(protocol, host, port), $"{check}/what?prop=123");
			Assert.AreEqual("localhost?prop=123".ReplaceProtocolHostPort(protocol, host, port), $"{check}?prop=123");
			Assert.AreEqual("localhost/?prop=123".ReplaceProtocolHostPort(protocol, host, port), $"{check}/?prop=123");

			check = string.IsNullOrWhiteSpace(port) ? $"{host}" : $"{host}:{port}";
			Assert.AreEqual("localhost:123".ReplaceProtocolHostPort(protocol, host, port), $"{check}");
			Assert.AreEqual("localhost.com:123".ReplaceProtocolHostPort(protocol, host, port), $"{check}");
			Assert.AreEqual("localhost:123/".ReplaceProtocolHostPort(protocol, host, port), $"{check}/");
			Assert.AreEqual("localhost:123/what".ReplaceProtocolHostPort(protocol, host, port), $"{check}/what");
			Assert.AreEqual("localhost:123/what?prop=123".ReplaceProtocolHostPort(protocol, host, port), $"{check}/what?prop=123");
			Assert.AreEqual("localhost:123?prop=123".ReplaceProtocolHostPort(protocol, host, port), $"{check}?prop=123");
			Assert.AreEqual("localhost:123/?prop=123".ReplaceProtocolHostPort(protocol, host, port), $"{check}/?prop=123");

			check = string.IsNullOrWhiteSpace(protocol) ? $"{host}" : $"{protocol}://{host}";
			check = string.IsNullOrWhiteSpace(port) ? $"{check}" : $"{check}:{port}";
			Assert.AreEqual("http://localhost".ReplaceProtocolHostPort(protocol, host, port), $"{check}");
			Assert.AreEqual("http://localhost.com".ReplaceProtocolHostPort(protocol, host, port), $"{check}");
			Assert.AreEqual("http://localhost/".ReplaceProtocolHostPort(protocol, host, port), $"{check}/");
			Assert.AreEqual("http://localhost/what".ReplaceProtocolHostPort(protocol, host, port), $"{check}/what");
			Assert.AreEqual("http://localhost/what?prop=123".ReplaceProtocolHostPort(protocol, host, port), $"{check}/what?prop=123");
			Assert.AreEqual("http://localhost?prop=123".ReplaceProtocolHostPort(protocol, host, port), $"{check}?prop=123");
			Assert.AreEqual("http://localhost/?prop=123".ReplaceProtocolHostPort(protocol, host, port), $"{check}/?prop=123");

			check = string.IsNullOrWhiteSpace(port) ? $"{host}" : $"{host}:{port}";
			check = string.IsNullOrWhiteSpace(protocol) ? $"{check}" : $"{protocol}://{check}";
			Assert.AreEqual("http://localhost:123".ReplaceProtocolHostPort(protocol, host, port), $"{check}");
			Assert.AreEqual("http://localhost.com:123".ReplaceProtocolHostPort(protocol, host, port), $"{check}");
			Assert.AreEqual("http://localhost:123/".ReplaceProtocolHostPort(protocol, host, port), $"{check}/");
			Assert.AreEqual("http://localhost:123/what".ReplaceProtocolHostPort(protocol, host, port), $"{check}/what");
			Assert.AreEqual("http://localhost:123/what?prop=123".ReplaceProtocolHostPort(protocol, host, port), $"{check}/what?prop=123");
			Assert.AreEqual("http://localhost:123?prop=123".ReplaceProtocolHostPort(protocol, host, port), $"{check}?prop=123");
			Assert.AreEqual("http://localhost:123/?prop=123".ReplaceProtocolHostPort(protocol, host, port), $"{check}/?prop=123");
		}

		[TestMethod]
		public void TestReplaceProtocolHostPort()
		{
			testReplaceProtocolHostPort(null, null, null);
			testReplaceProtocolHostPort(null, "xyz", null);
			testReplaceProtocolHostPort(null, "xyz", "333");
			testReplaceProtocolHostPort("https", "xyz", null);
			testReplaceProtocolHostPort("ws", "xyz", "444");
		}
	}
}
