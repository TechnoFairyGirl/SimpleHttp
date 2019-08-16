using System;
using System.Linq;

namespace SimpleHttp
{
	public static class HttpCors
	{
		public static void AllowCors(this HttpServer server, params string[] endpoints)
		{
			var regex = endpoints == null || endpoints.Length == 0 ? null :
				String.Join("|", endpoints.Select(ep => $"(?:{ep})"));

			server.PrependRoute(new HttpRoute(null, regex, true, false, (request, response) =>
			{
				response.AllowCors = true;
				return true;
			}));

			server.PrependErrorRoute(new HttpErrorRoute((e, request, response) =>
			{
				if (!response.IsDataSent)
					response.AllowCors = true;
				return true;
			}));

			server.PrependRoute(new HttpRoute("OPTIONS", regex, true, false, (request, response) =>
			{
				response.Headers.Add("Access-Control-Allow-Origin", "*");
				if (request.Headers.ContainsKey("access-control-request-method"))
					response.Headers.Add("Access-Control-Allow-Methods", request.Headers["access-control-request-method"]);
				if (request.Headers.ContainsKey("access-control-request-headers"))
					response.Headers.Add("Access-Control-Allow-Headers", request.Headers["access-control-request-headers"]);
				response.Headers.Add("Access-Control-Max-Age", "-1");
			}));
		}
	}
}
