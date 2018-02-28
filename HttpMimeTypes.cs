using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SimpleHttp
{
	public static class HttpMimeTypes
	{
		static readonly Dictionary<string, string> mimeTypes;

		static HttpMimeTypes()
		{
			mimeTypes = new Dictionary<string, string>();

			using (var reader = new StreamReader(
				new MemoryStream(Properties.Resources.MimeTypes, false),
				Encoding.UTF8, true, 4096, false))
			{
				while (!reader.EndOfStream)
				{
					var line = reader.ReadLine();
					if (String.IsNullOrEmpty(line) || line.StartsWith("#"))
						continue;

					var lineParts = line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
					var mimeType = lineParts[0];
					var extensions = lineParts[1].Split(' ');

					foreach (var extension in extensions)
					{
						if (!mimeTypes.ContainsKey(extension))
							mimeTypes.Add(extension, mimeType);
					}
				}
			}
		}

		public static string GetByExtension(string extension)
		{
			extension = extension.TrimStart('.');
			return mimeTypes.ContainsKey(extension) ? mimeTypes[extension] : "application/octet-stream";
		}

		public static string GetByPath(string path) => GetByExtension(Path.GetExtension(path));
	}
}
