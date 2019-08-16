using System;
using System.Collections.Generic;

namespace SimpleHttp
{
	public class HttpErrorRoute
	{
		protected readonly Func<Exception, HttpRequest, HttpResponse, bool> callback;

		public static bool InvokeAll(List<HttpErrorRoute> routes, Exception e, HttpRequest request, HttpResponse response)
		{
			foreach (var route in routes)
			{
				if (!route.Invoke(e, request, response))
					return false;
			}

			return true;
		}

		public HttpErrorRoute(Func<Exception, HttpRequest, HttpResponse, bool> callback) =>
			this.callback = callback;

		public HttpErrorRoute(Action<Exception, HttpRequest, HttpResponse> callback)
			: this((e, req, res) => { callback(e, req, res); return false; })
		{ }

		public bool Invoke(Exception e, HttpRequest request, HttpResponse response) =>
			callback(e, request, response);
	}
}
