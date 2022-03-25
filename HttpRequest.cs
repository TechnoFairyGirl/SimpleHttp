using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using Newtonsoft.Json;

namespace SimpleHttp
{
	public sealed class HttpRequest
	{
		static long requestIdCtr = 0;
		static readonly object requestIdCtrLock = new object();

		readonly HttpListenerRequest request;

		public string Method => request.HttpMethod;
		public string Url => request.Url.AbsolutePath;
		public string QueryString => request.Url.Query;
		public Dictionary<string, string> QueryParams => request.QueryString.ToDictionary();
		public Dictionary<string, string> Headers => request.Headers.ToDictionary(true);
		public string ContentType => request.ContentType;
		public long ContentLength => request.ContentLength64;
		public string UserAgent => request.UserAgent;
		public string ClientIP => request.RemoteEndPoint.Address.ToString();

		public object CustomData { get; set; }

		public long RequestId { get; }

		public Dictionary<string, string> Cookies => 
			Enumerable.Range(0, request.Cookies.Count)
				.ToDictionary(i => request.Cookies[i].Name, i => request.Cookies[i].Value);

		public static string UrlDecode(string str) => WebUtility.UrlDecode(str);
		public static byte[] Base64DecodeBytes(string str) => Convert.FromBase64String(str);
		public static string Base64Decode(string str) => HttpHelperExtensions.UTF8.GetString(Base64DecodeBytes(str));

		public HttpRequest(HttpListenerRequest request)
		{
			this.request = request;
			CustomData = null;

			lock (requestIdCtrLock)
			{
				requestIdCtr++;
				RequestId = requestIdCtr;
			}
		}

		public byte[] ReadBodyData() => request.ReadBodyData();
		public string ReadBodyText() => request.ReadBodyText();
		public Stream GetBodyStream() => request.InputStream;
		public T ReadBodyJson<T>() => JsonConvert.DeserializeObject<T>(ReadBodyText());
		public Dictionary<string, string> ReadBodyUrlEncoded() =>
			HttpUtility.ParseQueryString(ReadBodyText()).ToDictionary();
	}
}
