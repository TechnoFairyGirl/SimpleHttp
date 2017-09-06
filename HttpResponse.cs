using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace SimpleHttp
{
	class HttpResponse
	{
		HttpListenerResponse response;
		bool headersSet;

		public int? StatusCode { get; set; }
		public string StatusMessage { get; set; }
		public Dictionary<string, string> Headers { get; set; }
		public Dictionary<string, string> Cookies { get; set; }
		public Dictionary<string, string> CookiePath { get; set; }
		public Dictionary<string, long> CookieExpire { get; set; }
		public string ContentType { get; set; }
		public string RedirectLocation { get; set; }

		public static string UrlEncode(string str) { return WebUtility.UrlEncode(str); }

		public HttpResponse(HttpListenerResponse response)
		{
			this.response = response;
			headersSet = false;

			StatusCode = null;
			StatusMessage = null;
			Headers = new Dictionary<string, string>();
			Cookies = new Dictionary<string, string>();
			CookiePath = new Dictionary<string, string>();
			CookieExpire = new Dictionary<string, long>();
			ContentType = null;
			RedirectLocation = null;
		}

		void SetHeaders()
		{
			if (headersSet)
				return;

			if (StatusCode != null)
				response.StatusCode = (int)StatusCode;

			if (StatusMessage != null)
				response.StatusDescription = StatusMessage;

			if (Headers != null)
			{
				foreach (var header in Headers)
					response.AppendHeader(header.Key, header.Value);
			}

			if (Cookies != null)
			{
				foreach (var cookie in Cookies)
				{
					var cookieObj = new Cookie(cookie.Key, cookie.Value);
					if (CookiePath != null && CookiePath.ContainsKey(cookie.Key))
						cookieObj.Path = CookiePath[cookie.Key];
					if (CookieExpire != null && CookieExpire.ContainsKey(cookie.Key))
						cookieObj.Expires = DateTime.Now.AddSeconds(CookieExpire[cookie.Key]);
					response.AppendCookie(cookieObj);
				}
			}

			if (ContentType != null)
				response.ContentType = ContentType;
			
			if (RedirectLocation != null)
			{
				if (StatusCode == null)
					response.StatusCode = 302;
				response.RedirectLocation = RedirectLocation;
			}

			headersSet = true;
		}

		public void WriteBodyData(byte[] data)
		{
			if (ContentType == null)
				ContentType = "application/octet-stream";

			SetHeaders();

			response.WriteBodyData(data);
		}

		public void WriteBodyText(string text)
		{
			if (ContentType == null)
				ContentType = "text/plain";

			SetHeaders();

			response.WriteBodyText(text);
		}

		public void WriteBodyJson<T>(T obj)
		{
			if (ContentType == null)
				ContentType = "application/json";

			SetHeaders();

			var text = "";
			try { text = JsonConvert.SerializeObject(obj); }
			catch (JsonWriterException) { }

			response.WriteBodyText(text);
		}

		public Stream GetBodyStream()
		{
			SetHeaders();
			return response.OutputStream;
		}

		public void Close()
		{
			SetHeaders();
			response.Close();
		}
	}
}
