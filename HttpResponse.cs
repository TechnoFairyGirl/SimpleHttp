﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace SimpleHttp
{
	public sealed class HttpResponse
	{
		HttpListenerResponse response;

		public bool IsOpen { get; private set; }
		public bool IsDataSent { get; private set; }

		public int? StatusCode { get; set; }
		public string StatusMessage { get; set; }
		public bool? Chunked { get; set; }
		public Dictionary<string, string> Headers { get; set; }
		public Dictionary<string, string> Cookies { get; set; }
		public Dictionary<string, string> CookiePath { get; set; }
		public Dictionary<string, long> CookieExpire { get; set; }
		public string ContentType { get; set; }
		public string RedirectLocation { get; set; }
		public bool AllowCors { get; set; }

		public static string UrlEncode(string str) => WebUtility.UrlEncode(str);
		public static string Base64Encode(byte[] data) => Convert.ToBase64String(data);
		public static string Base64Encode(string str) => Base64Encode(HttpHelperExtensions.UTF8.GetBytes(str));

		public HttpResponse(HttpListenerResponse response)
		{
			this.response = response;
			this.response.KeepAlive = false;

			IsOpen = true;
			IsDataSent = false;

			Reset();
		}

		public void Reset()
		{
			if (IsDataSent)
				return;

			StatusCode = null;
			StatusMessage = null;
			Chunked = null;
			Headers = new Dictionary<string, string>();
			Cookies = new Dictionary<string, string>();
			CookiePath = new Dictionary<string, string>();
			CookieExpire = new Dictionary<string, long>();
			ContentType = null;
			RedirectLocation = null;
		}

		void SetHeaders()
		{
			if (IsDataSent)
				return;

			if (StatusCode != null)
				response.StatusCode = (int)StatusCode;

			if (StatusMessage != null)
				response.StatusDescription = StatusMessage;

			if (Chunked != null)
				response.SendChunked = (bool)Chunked;

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

			if (AllowCors)
			{
				response.AppendHeader("Access-Control-Expose-Headers", String.Join(", ", response.Headers.AllKeys));
				response.AppendHeader("Access-Control-Allow-Origin", "*");
			}

			IsDataSent = true;
		}

		public void WriteBodyData(byte[] data)
		{
			if (ContentType == null)
				ContentType = "application/octet-stream";

			SetHeaders();
			response.WriteBodyData(data);
			response.FlushBodyStream();
		}

		public void WriteBodyText(string text)
		{
			if (ContentType == null)
				ContentType = "text/plain";

			SetHeaders();
			response.WriteBodyText(text);
			response.FlushBodyStream();
		}

		public void WriteBodyJson<T>(T obj)
		{
			if (ContentType == null)
				ContentType = "application/json";

			SetHeaders();
			response.WriteBodyText(JsonConvert.SerializeObject(obj));
			response.FlushBodyStream();
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
			IsOpen = false;
		}
	}
}
