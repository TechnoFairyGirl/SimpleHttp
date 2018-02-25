using System;
using System.IO;
using System.Security;
using System.Text.RegularExpressions;

namespace SimpleHttp
{
	public static class HttpStaticContent
	{
		static bool ParseRangeRequestHeader(HttpRequest request, long totalLength, out long offset, out long length)
		{
			offset = 0;
			length = totalLength;

			if (!request.Headers.ContainsKey("Range"))
				return false;

			var range = Regex.Match(request.Headers["Range"], "^bytes=(\\d+)-(\\d*)");
			if (!range.Success)
				return false;

			var rangeStart = long.Parse(range.Groups[1].Value);
			offset = Math.Max(0, Math.Min(rangeStart, totalLength - 1));
			length -= offset;

			if (!String.IsNullOrEmpty(range.Groups[2].Value))
			{
				var rangeLength = long.Parse(range.Groups[2].Value) - rangeStart + 1;
				length = Math.Max(0, Math.Min(rangeLength, length));
			}

			return true;
		}

		static void SetRangeResponseHeader(HttpResponse response, long totalLength, long offset, long length)
		{
			if (length < 1)
				return;

			response.StatusCode = 206;
			response.Headers.Add("Content-Range", $"bytes {offset}-{offset + length - 1}/{totalLength}");
		}

		public static void AddStaticFile(this HttpServer server, string url, string filePath)
		{
			server.AddExactRoute("GET", url, (request, response) =>
			{
				using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					long offset, length;
					if (ParseRangeRequestHeader(request, file.Length, out offset, out length))
						SetRangeResponseHeader(response, file.Length, offset, length);

					response.Headers.Add("Accept-Ranges", "bytes");
					file.CopyBlockTo(response.GetBodyStream(), offset, length);
				}
			});
		}

		public static void AddStaticDirectory(
			this HttpServer server, string url, string directoryPath, string defaultFile = null)
		{
			server.AddRoute("GET", $"{Regex.Escape(url.TrimEnd('/'))}(/.*)?", (captures, request, response) =>
			{
				var fullDirectoryPath = Path.GetFullPath(directoryPath);
				if (!fullDirectoryPath.EndsWith($"{Path.DirectorySeparatorChar}"))
					fullDirectoryPath += Path.DirectorySeparatorChar;

				var fullFilePath = Path.GetFullPath(
					fullDirectoryPath + captures[0].Replace('/', Path.DirectorySeparatorChar));
				if (!fullFilePath.StartsWith(fullDirectoryPath))
					throw new SecurityException($"Access to '{fullFilePath}' not allowed.");

				if (Directory.Exists(fullFilePath))
				{
					if (!fullFilePath.EndsWith($"{Path.DirectorySeparatorChar}"))
						fullFilePath += Path.DirectorySeparatorChar;
					if (defaultFile != null)
						fullFilePath += defaultFile;
				}

				using (var file = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					long offset, length;
					if (ParseRangeRequestHeader(request, file.Length, out offset, out length))
						SetRangeResponseHeader(response, file.Length, offset, length);

					response.Headers.Add("Accept-Ranges", "bytes");
					file.CopyBlockTo(response.GetBodyStream(), offset, length);
				}
			});
		}
	}
}
