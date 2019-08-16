using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security;
using Newtonsoft.Json;

namespace SimpleHttp
{
	public sealed class HttpServer
	{
		readonly HttpListener listener;

		public int Port { get; }
		public LogWriter LogWriter { get; set; }
		public bool ResetResponseOnError { get; set; }

		public List<HttpRoute> Routes { get; }
		public List<HttpErrorRoute> ErrorRoutes { get; }
		public HttpRoute DefaultRoute { get; set; }
		public HttpErrorRoute DefaultErrorRoute { get; set; }

		public bool IsRunning => listener.IsListening;

		public HttpServer(int port)
		{
			Port = port;
			listener = new HttpListener();
			listener.Prefixes.Add($"http://+:{Port}/");

			LogWriter = new LogWriter();

			ResetResponseOnError = true;

			Routes = new List<HttpRoute>();
			ErrorRoutes = new List<HttpErrorRoute>();

			SetDefaultRoute((request, response) =>
				throw new FileNotFoundException($"No route matched."));

			SetDefaultErrorRoute((e, request, response) =>
			{
				if (response.IsDataSent)
					return;

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

					if (HttpRoute.InvokeMatched(Routes, request, response))
						DefaultRoute.Invoke(request, response);
				}
				catch (HttpListenerException) { throw; }
				catch (Exception e)
				{
					Log(request.RequestId, $"{e.GetType()} : {e.Message}");

					if (ResetResponseOnError)
					{
						if (response.IsDataSent)
							response.Close();
						else
							response.Reset();
					}

					if (HttpErrorRoute.InvokeAll(ErrorRoutes, e, request, response))
						DefaultErrorRoute.Invoke(e, request, response);
				}

				if (response.IsOpen)
					response.Close();
			}
			catch (HttpListenerException) { }
		}

		public void Start()
		{
			if (IsRunning)
				return;

			listener.Start();
			listener.BeginGetContext(ClientHandler, null);

			Log($"Service started on port {Port}.");
		}

		public void Stop()
		{
			if (!IsRunning)
				return;

			listener.Stop();

			Log("Service stopped.");
		}

		public void Log(long? requestId, string message) =>
			LogWriter.Log((requestId == null ? "" : $"<{requestId}> ") + message);
		public void Log(string message) => LogWriter.Log(message);

		public void AddRoute(HttpRoute route) => Routes.Add(route);
		public void AddRoute(string method, string url, Func<string[], HttpRequest, HttpResponse, bool> callback) =>
			AddRoute(new HttpRoute(method, url, true, false, callback));
		public void AddRoute(string method, string url, Action<string[], HttpRequest, HttpResponse> callback) =>
			AddRoute(new HttpRoute(method, url, true, false, callback));
		public void AddExactRoute(string method, string url, Func<HttpRequest, HttpResponse, bool> callback) =>
			AddRoute(new HttpRoute(method, url, false, false, callback));
		public void AddExactRoute(string method, string url, Action<HttpRequest, HttpResponse> callback) =>
			AddRoute(new HttpRoute(method, url, false, false, callback));
		public void PrependRoute(HttpRoute route) => Routes.Insert(0, route);
		public void ClearRoutes() => Routes.Clear();
		public void SetDefaultRoute(HttpRoute route) => DefaultRoute = route;
		public void SetDefaultRoute(Action<HttpRequest, HttpResponse> callback) =>
			SetDefaultRoute(new HttpRoute(null, null, false, false, callback));

		public void AddErrorRoute(HttpErrorRoute errorRoute) => ErrorRoutes.Add(errorRoute);
		public void AddErrorRoute(Func<Exception, HttpRequest, HttpResponse, bool> errorCallback) =>
			AddErrorRoute(new HttpErrorRoute(errorCallback));
		public void PrependErrorRoute(HttpErrorRoute errorRoute) => ErrorRoutes.Insert(0, errorRoute);
		public void ClearErrorRoutes() => ErrorRoutes.Clear();
		public void SetDefaultErrorRoute(HttpErrorRoute errorRoute) => DefaultErrorRoute = errorRoute;
		public void SetDefaultErrorRoute(Action<Exception, HttpRequest, HttpResponse> errorCallback) =>
			SetDefaultErrorRoute(new HttpErrorRoute(errorCallback));
	}
}
