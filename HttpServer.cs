using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security;
using Newtonsoft.Json;

namespace SimpleHttp
{
	public class HttpServer
	{
		readonly HttpListener listener;
		public int Port { get; }

		Action<long?, string> logCallback;

		readonly List<HttpRoute> routes;
		HttpRoute defaultRoute;
		HttpRoute errorRoute;

		public bool IsRunning { get => listener.IsListening; }

		public HttpServer(int port)
		{
			Port = port;
			listener = new HttpListener();
			listener.Prefixes.Add($"http://+:{Port}/");

			SetLogCallback((requestId, message) =>
				Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] " + 
					(requestId != null ? $"<{requestId}> " : "") + message));

			routes = new List<HttpRoute>();

			SetDefaultRoute((request, response) =>
				throw new FileNotFoundException($"No route matched."));

			SetErrorRoute((e, request, response) =>
			{
				Log(request.RequestId, $"{e.GetType()} : {e.Message}");

				if (response.IsDataSent)
					return;

				response.Reset();

				if (e is FileNotFoundException || e is DirectoryNotFoundException || e is PathTooLongException)
				{
					response.StatusCode = 404;
					response.WriteBodyText("Not found.");
					return;
				}

				if (e is SecurityException || e is UnauthorizedAccessException)
				{
					response.StatusCode = 403;
					response.WriteBodyText("Forbidden.");
					return;
				}

				if (e is JsonReaderException || e is KeyNotFoundException)
				{
					response.StatusCode = 400;
					response.WriteBodyText("Invalid request.");
					return;
				}

				response.StatusCode = 500;
				response.WriteBodyText("Unknown error.");
			});
		}

		void ClientHandler(IAsyncResult result)
		{
			try
			{
				HttpListenerContext context = listener.EndGetContext(result);
				listener.BeginGetContext(ClientHandler, null);

				var request = new HttpRequest(context.Request);
				var response = new HttpResponse(context.Response);

				try
				{
					Log(request.RequestId, $"{request.Method} request for '{request.Url}' from '{request.ClientIP}'.");

					if (!HttpRoute.InvokeMatchingRoutes(routes, request, response))
						defaultRoute.Invoke(request, response);
				}
				catch (HttpListenerException) { throw; }
				catch (Exception e) { errorRoute.Invoke(e, request, response); }

				if (response.IsOpen)
					response.Close();
			}
			catch (HttpListenerException) { }
		}

		public void Start()
		{
			if (IsRunning)
				throw new InvalidOperationException("Server is already running.");

			listener.Start();
			listener.BeginGetContext(ClientHandler, null);

			Log($"Service started on port {Port}.");
		}

		public void Stop()
		{
			if (!IsRunning)
				throw new InvalidOperationException("Server is not running.");

			listener.Stop();

			Log("Service stopped.");
		}

		protected void ThrowIfRunning()
		{
			if (IsRunning)
				throw new InvalidOperationException("This operation is not allowed while the server is running.");
		}

		public void SetLogCallback(Action<long?, string> callback)
		{
			ThrowIfRunning();
			logCallback = callback;
		}

		public void Log(long? requestId, string message) => logCallback(requestId, message);
		public void Log(string message) => logCallback(null, message);

		public void AddRoute(HttpRoute route)
		{
			ThrowIfRunning();
			routes.Add(route);
		}

		public void AddRoute(
			string method, string url, Action<string[], HttpRequest, HttpResponse> callback, bool matchFullUrl = false) =>
			AddRoute(new HttpRoute(method, url, callback, true, matchFullUrl));

		public void AddRoute(
			string url, Action<string[], HttpRequest, HttpResponse> callback, bool matchFullUrl = false) =>
			AddRoute(new HttpRoute(null, url, callback, true, matchFullUrl));

		public void AddRoute(
			string method, string url, Action<HttpRequest, HttpResponse> callback, bool matchFullUrl = false) =>
			AddRoute(new HttpRoute(method, url, callback, true, matchFullUrl));

		public void AddRoute(
			string url, Action<HttpRequest, HttpResponse> callback, bool matchFullUrl = false) =>
			AddRoute(new HttpRoute(null, url, callback, true, matchFullUrl));

		public void AddExactRoute(
			string method, string url, Action<HttpRequest, HttpResponse> callback, bool matchFullUrl = false) =>
			AddRoute(new HttpRoute(method, url, callback, false, matchFullUrl));

		public void AddExactRoute(
			string url, Action<HttpRequest, HttpResponse> callback, bool matchFullUrl = false) =>
			AddRoute(new HttpRoute(null, url, callback, false, matchFullUrl));

		public void PrependRoute(HttpRoute route)
		{
			ThrowIfRunning();
			routes.Insert(0, route);
		}

		public void ClearRoutes()
		{
			ThrowIfRunning();
			routes.Clear();
		}

		public void SetDefaultRoute(Action<HttpRequest, HttpResponse> callback)
		{
			ThrowIfRunning();
			defaultRoute = new HttpRoute(null, null, callback);
		}

		public void SetErrorRoute(Action<Exception, HttpRequest, HttpResponse> errorCallback)
		{
			ThrowIfRunning();
			errorRoute = new HttpRoute(errorCallback);
		}
	}
}
