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
		public static UTF8Encoding UTF8 { get; } = new UTF8Encoding(false);

		public static long CopyBlockTo(this Stream inStream, Stream outStream, long length)
		{
			byte[] buffer = new byte[4096];
			long bytesCopied = 0;

			while (true)
			{
				var bytesRead = inStream.Read(buffer, 0, (int)Math.Min(length - bytesCopied, buffer.Length));
				outStream.Write(buffer, 0, bytesRead);
				bytesCopied += bytesRead;
				if (bytesCopied >= length || bytesRead == 0)
					break;
			}

			return bytesCopied;
		}

		public static long CopyBlockTo(this Stream inStream, Stream outStream, long offset, long length)
		{
			if (offset != 0)
			{
				if (inStream.CanSeek || offset < 0)
					inStream.Seek(offset, SeekOrigin.Current);
				else
				{
					for (long i = 0; i < offset; i++)
						inStream.ReadByte();
				}
			}

			return inStream.CopyBlockTo(outStream, length);
		}

		public static byte[] ReadAllBytes(this Stream stream)
		{
			using (var buffer = new MemoryStream())
			{
				stream.CopyTo(buffer);
				return buffer.ToArray();
			}
		}

		public static byte[] ReadBytes(this Stream stream, long length)
		{
			using (var buffer = new MemoryStream())
			{
				stream.CopyBlockTo(buffer, length);
				return buffer.ToArray();
			}
		}

		public static string ReadAllText(this Stream stream)
		{
			using (var reader = new StreamReader(stream, UTF8, false, 4096, true))
				return reader.ReadToEnd();
		}

		public static string ReadLine(this Stream stream)
		{
			using (var reader = new StreamReader(stream, UTF8, false, 4096, true))
				return reader.ReadLine();
		}

		public static string[] ReadAllLines(this Stream stream)
		{
			using (var reader = new StreamReader(stream, UTF8, false, 4096, true))
			{
				var lines = new List<string>();
				while (!reader.EndOfStream)
					lines.Add(reader.ReadLine());
				return lines.ToArray();
			}
		}

		public static string[] ReadLines(this Stream stream, long count)
		{
			using (var reader = new StreamReader(stream, UTF8, false, 4096, true))
			{
				var lines = new List<string>();
				for (long i = 0; i < count && !reader.EndOfStream; i++)
					lines.Add(reader.ReadLine());
				return lines.ToArray();
			}
		}

		public static string[] ReadLinesFromEnd(this Stream stream, long count)
		{
			int LF = UTF8.GetBytes("\n")[0];

			stream.Position = stream.Length;

			long lineCtr = 0;

			while (true)
			{
				if (stream.Position == 0)
					break;

				stream.Position--;

				var byteRead = stream.ReadByte();
				stream.Position--;

				if (byteRead == LF && stream.Position != stream.Length - 1)
					lineCtr++;

				if (lineCtr >= count)
				{
					stream.Position++;
					break;
				}
			}

			return stream.ReadAllLines();
		}

		public static void Write(this Stream stream, byte[] data) =>
			stream.Write(data, 0, data.Length);

		public static void Write(this Stream stream, string text)
		{
			using (var writer = new StreamWriter(stream, UTF8, 4096, true))
				writer.Write(text);
		}

		public static void WriteLine(this Stream stream, string text)
		{
			using (var writer = new StreamWriter(stream, UTF8, 4096, true))
				writer.WriteLine(text);
		}

		public static void WriteLines(this Stream stream, string[] lines)
		{
			using (var writer = new StreamWriter(stream, UTF8, 4096, true))
			{
				foreach (var line in lines)
					writer.WriteLine(line);
			}
		}

		public static byte[] ReadBodyData(this HttpListenerRequest request) =>
			request.InputStream.ReadAllBytes();

		public static string ReadBodyText(this HttpListenerRequest request) =>
			request.InputStream.ReadAllText();

		public static void WriteBodyData(this HttpListenerResponse response, byte[] data) =>
			response.OutputStream.Write(data);

		public static void WriteBodyText(this HttpListenerResponse response, string text) =>
			response.OutputStream.Write(text);

		public static void FlushBodyStream(this HttpListenerResponse response) =>
			response.OutputStream.Flush();

		public static Dictionary<string, string> ToDictionary(
			this NameValueCollection nvc, bool lowercaseKeys = false) =>
			nvc.AllKeys.ToDictionary(k => lowercaseKeys ? k.ToLowerInvariant() : k, k => nvc[k]);
	}
}
