using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace SimpleHttp
{
	static class HttpHelperExtensions
	{
		public static byte[] ReadAllBytes(this Stream stream)
		{
			using (var buffer = new MemoryStream())
			{
				stream.CopyTo(buffer);
				return buffer.ToArray();
			}
		}

		public static string ReadAllText(this Stream stream)
		{
			using (var reader = new StreamReader(stream, Encoding.UTF8))
			{
				return reader.ReadToEnd();
			}
		}

		public static void Write(this Stream stream, byte[] data)
		{
			stream.Write(data, 0, data.Length);
		}

		public static void Write(this Stream stream, string text)
		{
			stream.Write(Encoding.UTF8.GetBytes(text));
		}

		public static byte[] ReadBodyData(this HttpListenerRequest request)
		{
			return request.InputStream.ReadAllBytes();
		}

		public static string ReadBodyText(this HttpListenerRequest request)
		{
			return request.InputStream.ReadAllText();
		}

		public static void WriteBodyData(this HttpListenerResponse response, byte[] data)
		{
			response.OutputStream.Write(data);
		}

		public static void WriteBodyText(this HttpListenerResponse response, string text)
		{
			response.OutputStream.Write(text);
		}

		public static Dictionary<string, string> ToDictionary(this NameValueCollection nvc)
		{
			return nvc.AllKeys.ToDictionary(k => k, k => nvc[k]);
		}
	}
}
