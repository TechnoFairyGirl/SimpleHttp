using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SimpleHttp
{
	public class HttpRoute
	{
		Action<string[], HttpRequest, HttpResponse> Callback { get; }
		Action<Exception, HttpRequest, HttpResponse> ErrorCallback { get; }

		public string MethodPattern { get; }
		public string UrlPattern { get; }
		public bool IsRegex { get; }
		public bool MatchFullUrl { get; }

		public bool IsStandard { get => Callback != null; }
		public bool IsError { get => ErrorCallback != null; }

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
			Callback = callback;
			ErrorCallback = null;

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
			Callback = null;
			ErrorCallback = errorCallback;

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
			if (Callback == null || captures == null)
				return false;

			Callback(captures, request, response);
			return true;
		}

		public bool Invoke(HttpRequest request, HttpResponse response) =>
			Invoke(new string[0], request, response);

		public bool Invoke(Exception e, HttpRequest request, HttpResponse response)
		{
			if (ErrorCallback == null || e == null)
				return false;

			ErrorCallback(e, request, response);
			return true;
		}

		public bool InvokeOnMatch(HttpRequest request, HttpResponse response) =>
			Invoke(Match(request), request, response);
	}
}
