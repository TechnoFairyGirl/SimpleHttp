using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace SimpleHttp
{
	public class HttpRequest
	{
		static long requestIdCtr = 0;
		static object requestIdCtrLock = new object();

		HttpListenerRequest request;

		public string Method { get => request.HttpMethod; }
		public string Url { get => request.Url.AbsolutePath; }
		public string QueryString { get => request.Url.Query; }
		public Dictionary<string, string> QueryParams { get => request.QueryString.ToDictionary(); }
		public Dictionary<string, string> Headers { get => request.Headers.ToDictionary(true); }
		public string ContentType { get => request.ContentType; }
		public long ContentLength { get => request.ContentLength64; }
		public string UserAgent { get => request.UserAgent; }
		public string ClientIP { get => request.RemoteEndPoint.Address.ToString(); }

		public object CustomData { get; set; }

		public long RequestId { get; private set; }

		public Dictionary<string, string> Cookies
		{
			get => Enumerable.Range(0, request.Cookies.Count)
				.ToDictionary(i => request.Cookies[i].Name, i => request.Cookies[i].Value);
		}

		public static string UrlDecode(string str) => WebUtility.UrlDecode(str);
		public static byte[] Base64DecodeBytes(string str) => Convert.FromBase64String(str);
		public static string Base64Decode(string str) => Encoding.UTF8.GetString(Base64DecodeBytes(str));

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
	}
}
