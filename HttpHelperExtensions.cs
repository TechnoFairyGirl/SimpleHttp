using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace SimpleHttp
{
	public static class HttpHelperExtensions
	{
		public static long CopyBlockTo(this Stream inStream, Stream outStream, long? offset, long? length)
		{
			if (offset != null)
				inStream.Seek((long)offset, SeekOrigin.Current);

			byte[] buffer = new byte[4096];
			long bytesCopied = 0;

			while (true)
			{
				var bytesRead = inStream.Read(buffer, 0, length == null ? buffer.Length :
					(int)Math.Min((long)length - bytesCopied, buffer.Length));
				outStream.Write(buffer, 0, bytesRead);
				bytesCopied += bytesRead;
				if (bytesRead == 0 || (length != null && bytesCopied >= (long)length))
					break;
			}

			return bytesCopied;
		}

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
			using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, true))
				return reader.ReadToEnd();
		}

		public static void Write(this Stream stream, byte[] data) =>
			stream.Write(data, 0, data.Length);

		public static void Write(this Stream stream, string text) =>
			stream.Write(Encoding.UTF8.GetBytes(text));

		public static byte[] ReadBodyData(this HttpListenerRequest request) =>
			request.InputStream.ReadAllBytes();

		public static string ReadBodyText(this HttpListenerRequest request) =>
			request.InputStream.ReadAllText();

		public static void WriteBodyData(this HttpListenerResponse response, byte[] data) =>
			response.OutputStream.Write(data);

		public static void WriteBodyText(this HttpListenerResponse response, string text) =>
			response.OutputStream.Write(text);

		public static Dictionary<string, string> ToDictionary(
			this NameValueCollection nvc, bool lowercaseKeys = false) =>
			nvc.AllKeys.ToDictionary(k => lowercaseKeys ? k.ToLowerInvariant() : k, k => nvc[k]);
	}
}
