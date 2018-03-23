using System;
using System.IO;
using System.Net;
using System.Text;
namespace ChickenSoup
{
	using Configuration;
	public static class Http
	{
		public static void Close(this HttpListenerContext client, HttpStatusCode code)
		{
			client.Response.StatusCode = (int)code;
			client.Response.Close();
		}

		internal static void Error(this HttpListenerContext client, HttpStatusCode code)
		{
			var error = ChickenSoup.ErrorSnippet;
			int i = error.IndexOf("<error>", StringComparison.InvariantCultureIgnoreCase);
			if (i < 0)
				throw new FormatException("<error> tag missing");
			client.Response.StatusCode = (int)code;
			error = error.Insert(i + "<error>".Length, $"Error {(int)code}: {client.Response.StatusDescription}");
			error = error.WrapContent();
			client.WriteAndClose(Encoding.UTF8.GetBytes(error), "html", code);
		}

		public static void SetHeader(this HttpListenerContext client, string header, object value) => client.SetHeader(header, value.ToString());
		public static void SetHeader(this HttpListenerContext client, string header, string value)
		{
			client.Response.Headers[header] = value;
		}

		public static string Stringify(this HttpListenerContext client)
		{
			var r = client.Request;
			return $"<{r.RemoteEndPoint}> {r.HttpMethod} {r.Url}";
		}

		public static void Write(this HttpListenerContext client, string str, string fileExt)
		{
			if (!client.Request.Url.Query.Contains("raw"))
				str = str.WrapContent();
			client.Write(Encoding.UTF8.GetBytes(str), fileExt);
		}
		public static void Write(this HttpListenerContext client, byte[] data, string fileExt)
		{
			client.Response.Headers["Server"] = "ChickenSoup/" + Configuration.Config.Version;
			client.Response.ContentType = GetType(fileExt);
			client.Write(data);
		}
		public static void Write(this HttpListenerContext client, byte[] data) => client.Response.OutputStream.Write(data);

		public static void WriteAndClose(this HttpListenerContext client, string str, string fileExt, HttpStatusCode code)
		{
			if (!client.Request.Url.Query.Contains("raw"))
				str = str.WrapContent();
			client.WriteAndClose(Encoding.UTF8.GetBytes(str), fileExt, code);
		}
		public static void WriteAndClose(this HttpListenerContext client, byte[] data, string fileExt, HttpStatusCode code)
		{
			client.Response.StatusCode = (int)code;
			client.Response.Headers["Server"] = "ChickenSoup/" + Configuration.Config.Version;
			client.Write(data, fileExt);
			client.Response.Close();
		}

		private static string GetType(string path)
		{
			string ext = path.Substring(path.LastIndexOf('.') + 1);
			switch (ext[0])
			{
				case 'c':
					if (ext == "css") return "text/css";
					break;
				case 'h':
					if (ext == "html") return "text/html";
					break;
				case 'i':
					if (ext == "ico") return "image/x-icon";
					break;
				case 'j':
					if (ext == "js") return "text/javascript";
					if (ext == "jpg" || ext == "jpeg") return "image/jpeg";
					if (ext == "json") return "application/json";
					break;
				case 'p':
					if (ext == "png") return "image/png";
					break;
				case 'm':
					if (ext == "mp4") return "video/mp4";
					if (ext == "mpeg") return "audio/mpeg";
					break;
				case 'x':
					if (ext == "xml") return "video/xml";
					break;
			}
			return "text/plain";
		}

		private static bool ShouldCache(int code)
		{
			if (code == 200)
				return true;
			return false;
		}
	}
}
