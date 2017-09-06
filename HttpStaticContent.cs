using System;
using System.IO;
using System.Security;

namespace SimpleHttp
{
	static class HttpStaticContent
	{
		public static void AddStaticFile(this HttpServer server, string url, string filePath)
		{
			server.AddExactRoute("GET", url, (request, response) =>
			{
				using (var file = new FileStream(filePath, FileMode.Open))
					file.CopyTo(response.GetBodyStream());
			});
		}

		public static void AddStaticDirectory(this HttpServer server, string url, string directoryPath, string defaultFile = null)
		{
			server.AddRoute("GET", $"{url.TrimEnd('/')}(/.*)?", (captures, request, response) =>
			{
				var fullDirectoryPath = Path.GetFullPath(directoryPath);
				if (!fullDirectoryPath.EndsWith($"{Path.DirectorySeparatorChar}"))
					fullDirectoryPath += Path.DirectorySeparatorChar;

				var fullFilePath = Path.GetFullPath(
					fullDirectoryPath + captures[0].Replace('/', Path.DirectorySeparatorChar));
				if (!fullFilePath.StartsWith(fullDirectoryPath))
					throw new SecurityException();

				if (defaultFile != null && Directory.Exists(fullFilePath))
					fullFilePath += Path.DirectorySeparatorChar + defaultFile;

				using (var file = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
					file.CopyTo(response.GetBodyStream());
			});
		}
	}
}
