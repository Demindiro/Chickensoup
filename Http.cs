using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
namespace ChickenSoup
{
	using System.Threading;
	using Configuration;
	public static class Http
	{
		public delegate void Callback(HttpListenerContext context, HttpListener server);

		[Config("CACHE_MAX_AGE")]             public static readonly int CacheMaxAge;
		[Config("PORT")]                      public static readonly ushort Port;
		[Config("TLS_PORT", Optional = true)] public static readonly ushort TlsPort;
		public static readonly List<Callback> GlobalListeners = new List<Callback>();

		public static void Close(this HttpListenerContext context, HttpStatusCode code)
		{
			context.Response.StatusCode = (int)code;
			context.Response.Close();
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
				client.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
				error = error.Insert(i + "<error>".Length, $"Error {(int)code}: {client.Response.StatusDescription}");
				error = error.WrapContent();
				client.Write(Encoding.UTF8.GetBytes(error), "html", code);
			}
			catch (Exception ex)
			{
				Logger.Log(ex);
				client.Response.OutputStream.Close();
			}
		}


		private static string Stringify(this HttpListenerContext context)
		{
			var req = context.Request;
			var res = context.Response;
			// TODO figure out why RemoteEndPoint is sometimes null
			return $"<{req.RemoteEndPoint?.Address}> {req.HttpMethod} {res.StatusCode} {req.Url} ({req.UserAgent})";
		}

		public static void Write(this HttpListenerContext client, string str, string fileExt, HttpStatusCode code)
		{
			if (!client.Request.Url.Query.Contains("raw"))
				str = str.WrapContent();
			client.Write(Encoding.UTF8.GetBytes(str), fileExt, code);
		}
		public static void Write(this HttpListenerContext context, byte[] data, string fileExt, HttpStatusCode code)
		{
			context.Response.StatusCode = (int)code;
			context.Response.Headers["Cache-Control"] = "public, max-age=" + CacheMaxAge;
			context.Response.ContentType = MimeType.GetMimeType(fileExt);
			// PNG, JPG and ICO are already compressed. Compressing again doesn't yield much better results (or even worse results)
			// TODO it works but it's a bit lazy.
			if (!"png,jpg,ico".Contains(fileExt) && context.Request.Headers["Accept-Encoding"].Contains("gzip")) {
				using (var ms = new MemoryStream())
				{
					using (var gzip = new GZipStream(ms, CompressionLevel.Fastest, true))
						gzip.Write(data, 0, data.Length);
					context.Response.ContentLength64 = ms.Length;
					Console.WriteLine($"GZIP: {ms.Length} / {data.Length} ({(1f - (float)ms.Length / data.Length) * 100f}%)");
					context.Response.Headers["Content-Encoding"] = "gzip";
					ms.Position = 0;
					ms.CopyTo(context.Response.OutputStream);
				}
			}
			else
			{
				Console.WriteLine($"None: {data.Length} / {data.Length} (0%)");
				context.Response.ContentLength64 = data.Length;
				context.Response.OutputStream.Write(data, 0, data.Length);
			}
			context.Response.OutputStream.Close();
			Logger.Log(context.Stringify());
		}


		public static HttpListener AddListener(string url, Callback callback)
		{
			var listener = new HttpListener();
			listener.Prefixes.Add($"http://*:{Port}/{url}/");
			if (TlsPort != 0)
				listener.Prefixes.Add($"https://*:{TlsPort}/{url}/");
			listener.Start();
			listener.BeginGetContext(HandleNewContext, new object[] {listener, callback});
			return listener;
		}


		private static void HandleNewContext(IAsyncResult ar)
		{
			try
			{
				if (ExtractAsyncResult(ar, out var server, out var context, out var callback))
				{
					context.Response.Headers["Server"] = "ChickenSoup/" + Configuration.Config.Version;
					for (int i = 0; i < GlobalListeners.Count; i++)
						GlobalListeners[i](context, server);
					ThreadPool.QueueUserWorkItem((state) => callback(context, server));
					if (server.IsListening)
						server.BeginGetContext(HandleNewContext, ar.AsyncState);
					else
						server.Close(); // Make sure the listener has actually been disposed.
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
