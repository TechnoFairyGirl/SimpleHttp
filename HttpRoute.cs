using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SimpleHttp
{
	public class HttpRoute
	{
		protected readonly Func<string[], HttpRequest, HttpResponse, bool> callback;

		public string MethodPattern { get; }
		public string UrlPattern { get; }
		public bool IsRegex { get; }
		public bool MatchFullUrl { get; }

		public static bool InvokeMatched(List<HttpRoute> routes, HttpRequest request, HttpResponse response)
		{
			foreach (var route in routes)
			{
				var captures = route.Match(request);
				if (captures == null)
					continue;

				if (!route.Invoke(captures, request, response))
					return false;
			}

			return true;
		}

		public HttpRoute(
			string methodPattern, string urlPattern, bool isRegex, bool matchFullUrl,
			Func<string[], HttpRequest, HttpResponse, bool> callback)
		{
			this.callback = callback;

			MethodPattern = methodPattern;
			UrlPattern = urlPattern;
			IsRegex = isRegex;
			MatchFullUrl = matchFullUrl;
		}

		public HttpRoute(
			string methodPattern, string urlPattern, bool isRegex, bool matchFullUrl,
			Func<HttpRequest, HttpResponse, bool> callback)
			: this(methodPattern, urlPattern, isRegex, matchFullUrl, 
				(cap, req, res) => callback(req, res))
		{ }

		public HttpRoute(
			string methodPattern, string urlPattern, bool isRegex, bool matchFullUrl,
			Action<string[], HttpRequest, HttpResponse> callback)
			: this(methodPattern, urlPattern, isRegex, matchFullUrl, 
				(cap, req, res) => { callback(cap, req, res); return false; })
		{ }

		public HttpRoute(
			string methodPattern, string urlPattern, bool isRegex, bool matchFullUrl,
			Action<HttpRequest, HttpResponse> callback)
			: this(methodPattern, urlPattern, isRegex, matchFullUrl,
				(cap, req, res) => { callback(req, res); return false; })
		{ }

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
						.Select(cap => HttpRequest.UrlDecode(cap.Value)).ToArray();
				}
				else
				{
					if (url != UrlPattern)
						return null;
				}
			}

			return result;
		}

		public bool Invoke(string[] captures, HttpRequest request, HttpResponse response) =>
			callback(captures, request, response);
		public bool Invoke(HttpRequest request, HttpResponse response) =>
			Invoke(new string[0], request, response);
	}
}
