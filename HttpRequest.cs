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

		public string Method { get { return request.HttpMethod; } }
		public string Url { get { return request.Url.AbsolutePath; } }
		public string QueryString { get { return request.Url.Query; } }
		public Dictionary<string, string> QueryParams { get { return request.QueryString.ToDictionary(); } }
		public Dictionary<string, string> Headers { get { return request.Headers.ToDictionary(true); } }
		public string ContentType { get { return request.ContentType; } }
		public long ContentLength { get { return request.ContentLength64; } }
		public string UserAgent { get { return request.UserAgent; } }
		public string ClientIP { get { return request.RemoteEndPoint.Address.ToString(); } }

		public object CustomData { get; set; }

		public long RequestId { get; private set; }

		public Dictionary<string, string> Cookies
		{
			get
			{
				return Enumerable.Range(0, request.Cookies.Count)
					.ToDictionary(i => request.Cookies[i].Name, i => request.Cookies[i].Value);
			}
		}

		public static string UrlDecode(string str) { return WebUtility.UrlDecode(str); }
		public static byte[] Base64DecodeBytes(string str) { return Convert.FromBase64String(str); }
		public static string Base64Decode(string str) { return Encoding.UTF8.GetString(Base64DecodeBytes(str)); }

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

		public byte[] ReadBodyData() { return request.ReadBodyData(); }
		public string ReadBodyText() { return request.ReadBodyText(); }
		public Stream GetBodyStream() { return request.InputStream; }

		public T ReadBodyJson<T>()
		{
			return JsonConvert.DeserializeObject<T>(ReadBodyText());
		}
	}
}
