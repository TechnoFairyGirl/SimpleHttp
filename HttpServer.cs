﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security;

namespace SimpleHttp
{
	class HttpServer
	{
		HttpListener listener;

		public int Port { get; private set; }

		public List<HttpRoute> Routes { get; private set; }
		public HttpRoute DefaultRoute { get; private set; }
		public HttpRoute ErrorRoute { get; private set; }

		public bool IsRunning { get { return listener.IsListening; } }

		public HttpServer(int port)
		{
			Port = port;
			listener = new HttpListener();
			listener.Prefixes.Add($"http://+:{Port}/");

			Routes = new List<HttpRoute>();

			SetDefaultRoute((request, response) =>
				throw new FileNotFoundException($"No route matched for '{request.Url}'."));

			SetErrorRoute((e, request, response) =>
			{
				Console.WriteLine($"{e.GetType()} : {e.Message}");

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
					if (!HttpRoute.InvokeMatchingRoutes(Routes, request, response))
						DefaultRoute.Invoke(request, response);
				}
				catch (HttpListenerException) { throw; }
				catch (Exception e) { ErrorRoute.Invoke(e, request, response); }

				if (!response.IsOpen)
					return;

				response.Close();
			}
			catch (HttpListenerException) { }
		}

		public void Start()
		{
			if (listener.IsListening)
				return;

			listener.Start();
			listener.BeginGetContext(ClientHandler, null);
		}

		public void Stop()
		{
			if (!listener.IsListening)
				return;

			listener.Stop();
		}

		public void AddRoute(
			string method, string url, Action<string[], HttpRequest, HttpResponse> callback, bool matchFullUrl = false)
		{
			Routes.Add(new HttpRoute(method, url, callback, true, matchFullUrl));
		}

		public void AddRoute(
			string url, Action<string[], HttpRequest, HttpResponse> callback, bool matchFullUrl = false)
		{
			Routes.Add(new HttpRoute(null, url, callback, true, matchFullUrl));
		}

		public void AddRoute(
			string method, string url, Action<HttpRequest, HttpResponse> callback, bool matchFullUrl = false)
		{
			Routes.Add(new HttpRoute(method, url, callback, true, matchFullUrl));
		}

		public void AddRoute(
			string url, Action<HttpRequest, HttpResponse> callback, bool matchFullUrl = false)
		{
			Routes.Add(new HttpRoute(null, url, callback, true, matchFullUrl));
		}

		public void AddExactRoute(
			string method, string url, Action<HttpRequest, HttpResponse> callback, bool matchFullUrl = false)
		{
			Routes.Add(new HttpRoute(method, url, callback, false, matchFullUrl));
		}

		public void AddExactRoute(
			string url, Action<HttpRequest, HttpResponse> callback, bool matchFullUrl = false)
		{
			Routes.Add(new HttpRoute(null, url, callback, false, matchFullUrl));
		}

		public void SetDefaultRoute(Action<HttpRequest, HttpResponse> callback)
		{
			DefaultRoute = new HttpRoute(null, null, callback);
		}

		public void SetErrorRoute(Action<Exception, HttpRequest, HttpResponse> errorCallback)
		{
			ErrorRoute = new HttpRoute(errorCallback);
		}
	}
}
