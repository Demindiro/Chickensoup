﻿using System;
using System.IO;
using System.Net;
using System.Text;
namespace ChickenSoup
{
	using System.Threading;
	using Configuration;
	public static class Http
	{
		public delegate bool Callback(HttpListenerContext context);

		[Config("CACHE_MAX_AGE")]             public static readonly int CacheMaxAge;
		[Config("PORT")]                      public static readonly ushort Port;
		[Config("TLS_PORT", Optional = true)] public static readonly ushort TlsPort;

		public static void Close(this HttpListenerContext client, HttpStatusCode code)
		{
			client.Response.StatusCode = (int)code;
			client.Response.Close();
		}

		public static void Error(this HttpListenerContext client, HttpStatusCode code)
		{
			var error = ChickenSoup.ErrorSnippet;
			int i = error.IndexOf("<error>", StringComparison.InvariantCultureIgnoreCase);
			if (i < 0)
				throw new FormatException("<error> tag missing");
			try
			{
				client.Response.StatusCode = (int)code;
				client.SetHeader("Cache-Control", "no-cache, no-store, must-revalidate");
				error = error.Insert(i + "<error>".Length, $"Error {(int)code}: {client.Response.StatusDescription}");
				error = error.WrapContent();
				client.WriteAndClose(Encoding.UTF8.GetBytes(error), "html", code);
			}
			catch (Exception ex)
			{
				Logger.Log(ex);
				client.Response.OutputStream.Close();
			}
		}


		public static void SetHeader(this HttpListenerContext client, string header, object value) => client.SetHeader(header, value.ToString());
		public static void SetHeader(this HttpListenerContext client, string header, string value)
		{
			client.Response.Headers[header] = value;
		}


		private static string Stringify(this HttpListenerContext context)
		{
			var req = context.Request;
			var res = context.Response;
			// TODO figure out why RemoteEndPoint is sometimes null
			return $"<{req.RemoteEndPoint?.Address}> {req.HttpMethod} {res.StatusCode} {req.Url}";
		}


		public static void Write(this HttpListenerContext client, string str, string fileExt)
		{
			if (!client.Request.Url.Query.Contains("raw"))
				str = str.WrapContent();
			client.Write(Encoding.UTF8.GetBytes(str), fileExt);
		}
		public static void Write(this HttpListenerContext client, byte[] data, string fileExt)
		{
			client.SetHeader("Cache-Control", "public, max-age=" + CacheMaxAge);
			client.Response.ContentType = MimeType.GetMimeType(fileExt);
			client.Write(data);
		}
		public static void Write(this HttpListenerContext client, byte[] data) => client.Response.OutputStream.Write(data);


		public static void WriteAndClose(this HttpListenerContext client, string str, string fileExt, HttpStatusCode code)
		{
			if (!client.Request.Url.Query.Contains("raw"))
				str = str.WrapContent();
			client.WriteAndClose(Encoding.UTF8.GetBytes(str), fileExt, code);
		}
		public static void WriteAndClose(this HttpListenerContext context, byte[] data, string fileExt, HttpStatusCode code)
		{
			context.Response.StatusCode = (int)code;
			context.Response.ContentLength64 = data.Length;
			context.Write(data, fileExt);
			context.Response.Close();
			Logger.Log(context.Stringify());
		}


		public static void AddListener(string url, Callback callback)
		{
			var server = new HttpListener();
			server.Prefixes.Add($"http://*:{Port}/{url}/");
			if (TlsPort != 0)
				server.Prefixes.Add($"https://*:{TlsPort}/{url}/");
			server.Start();
			server.BeginGetContext(HandleNewContext, new object[] {server, callback});
		}

		private static void HandleNewContext(IAsyncResult ar)
		{
			try
			{
				if (ExtractAsyncResult(ar, out var server, out var context, out var callback))
				{
					context.SetHeader("Server", "ChickenSoup/" + Configuration.Config.Version);
					if (callback(context))
						server.BeginGetContext(HandleNewContext, ar.AsyncState);
					else
						server.Close();
				}
				else
				{
					server.BeginGetContext(HandleNewContext, ar.AsyncState);
				}
			}
			catch (Exception ex)
			{
				Console.Write(
					$"=== An exception occured ===\n" +
					$"{ex}\n" +
					$"============================\n"
				);
			}
		}

		private static bool ExtractAsyncResult(IAsyncResult ar, out HttpListener server, out HttpListenerContext context, out Callback callback)
		{
			var array = (object[])ar.AsyncState;
			server = (HttpListener)array[0];
			callback = (Callback)array[1];
			try
			{
				context = server.EndGetContext(ar);
				return true;
			}
			catch (Exception ex)
			{
				Logger.Log(ex);
				context = null;
				return false;
			}
		}

		private static bool ShouldCache(int code)
		{
			if (code == 200)
				return true;
			return false;
		}

		private static void Write(this Stream stream, byte[] data)
		{
			if (data != null)
				stream.Write(data, 0, data.Length);
		}
	}
}
