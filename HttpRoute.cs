using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SimpleHttp
{
	public class HttpRoute
	{
		Action<string[], HttpRequest, HttpResponse> callback;
		Action<Exception, HttpRequest, HttpResponse> errorCallback;

		public string MethodPattern { get; private set; }
		public string UrlPattern { get; private set; }
		public bool IsRegex { get; private set; }
		public bool MatchFullUrl { get; private set; }

		public bool IsStandard { get => callback != null; }
		public bool IsError { get => errorCallback != null; }

		public static bool InvokeMatchingRoutes(
			List<HttpRoute> routes, HttpRequest request, HttpResponse response)
		{
			var routeMatched = false;

			foreach (var route in routes)
			{
				routeMatched |= route.InvokeOnMatch(request, response);
				if (!response.IsOpen)
					break;
			}

			return routeMatched;
		}

		public HttpRoute(
			string methodPattern, string urlPattern, 
			Action<string[], HttpRequest, HttpResponse> callback,
			bool isRegex = true, bool matchFullUrl = false)
		{
			this.callback = callback;
			this.errorCallback = null;

			MethodPattern = methodPattern;
			UrlPattern = urlPattern;
			IsRegex = isRegex;
			MatchFullUrl = matchFullUrl;
		}

		public HttpRoute(
			string methodPattern, string urlPattern,
			Action<HttpRequest, HttpResponse> callback,
			bool isRegex = true, bool matchFullUrl = false)
			: this(methodPattern, urlPattern, (cap, req, res) => callback(req, res), isRegex, matchFullUrl)
		{ }

		public HttpRoute(Action<Exception, HttpRequest, HttpResponse> errorCallback)
		{
			this.callback = null;
			this.errorCallback = errorCallback;

			MethodPattern = null;
			UrlPattern = null;
			IsRegex = false;
			MatchFullUrl = false;
		}

		public string[] Match(HttpRequest request)
		{
			var result = new string[0];

			if (MethodPattern != null)
			{
				if (IsRegex)
				{
					if (!Regex.IsMatch(request.Method, $"\\A(?:{MethodPattern})\\z", RegexOptions.Singleline))
						return null;
				}
				else
				{
					if (request.Method != MethodPattern)
						return null;
				}
			}

			if (UrlPattern != null)
			{
				string url = MatchFullUrl ? request.Url + request.QueryString : request.Url;

				if (IsRegex)
				{
					var match = Regex.Match(url, $"\\A(?:{UrlPattern})\\z", RegexOptions.Singleline);
					if (!match.Success)
						return null;
					result = match.Groups.Cast<Capture>().Skip(1)
						.Select(c => HttpRequest.UrlDecode(c.Value)).ToArray();
				}
				else
				{
					if (url != UrlPattern)
						return null;
				}
			}

			return result;
		}

		public bool Invoke(string[] captures, HttpRequest request, HttpResponse response)
		{
			if (callback == null || captures == null)
				return false;

			callback(captures, request, response);
			return true;
		}

		public bool Invoke(HttpRequest request, HttpResponse response) =>
			Invoke(new string[0], request, response);

		public bool Invoke(Exception e, HttpRequest request, HttpResponse response)
		{
			if (errorCallback == null || e == null)
				return false;

			errorCallback(e, request, response);
			return true;
		}

		public bool InvokeOnMatch(HttpRequest request, HttpResponse response) =>
			Invoke(Match(request), request, response);
	}
}
