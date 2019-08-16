using System;
using System.Globalization;
using System.IO;

namespace SimpleHttp
{
	public sealed class LogWriter
	{
		public const string DefaultTimestampFormat = "yyyy-MM-dd HH:mm:ss";

		public static string GetTimestamp(string format) =>
			DateTime.Now.ToString(format, CultureInfo.InvariantCulture);
		public static string GetTimestamp() => GetTimestamp(DefaultTimestampFormat);

		readonly TextWriter writer;

		public string TimestampFormat { get; set; }

		public LogWriter(TextWriter writer)
		{
			this.writer = writer;
			TimestampFormat = DefaultTimestampFormat;
		}

		public LogWriter(Stream stream) :
			this(new StreamWriter(stream, HttpHelperExtensions.UTF8))
		{ }

		public LogWriter(string file) :
			this(new StreamWriter(file, true, HttpHelperExtensions.UTF8))
		{ }

		public LogWriter() :
			this(Console.Out)
		{ }

		public void Log(string text)
		{
			if (TimestampFormat != null)
				writer.WriteLine($"[{GetTimestamp(TimestampFormat)}] {text}");
			else
				writer.WriteLine(text);
			writer.Flush();
		}
	}
}
