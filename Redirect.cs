using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Configuration;

namespace ChickenSoup
{
	public static partial class Redirect
	{
		#pragma warning disable 0649
		[Config("REDIRECT_URLS_FILE")] internal static string urlFile;
		#pragma warning restore 0649
		private static HttpListener server;

		public static Dictionary<string, string> Urls { get; private set; }


		public static void Init()
		{
			if(File.Exists(urlFile))
				Urls = JSON.Json.Parse(File.ReadAllText(urlFile)).Convert();
			else
				File.WriteAllText(urlFile, "{}");
			server = new HttpListener();
			server.Prefixes.Add($"http://*:{ChickenSoup.Port}/rdr/");
			server.Start();
			server.BeginGetContext(RedirectClient, null);
		}

		private static void RedirectClient(IAsyncResult ar)
		{
			var client = server.EndGetContext(ar);
			server.BeginGetContext(RedirectClient, null);
			var url    = client.Request.Url.Segments;
			if (url.Length < 3)
			{
				client.Error(HttpStatusCode.BadRequest);
				return;
			}
			var key = client.Request.Url.Segments[2];
			if(!Urls.ContainsKey(key))
			{
				client.Error(HttpStatusCode.BadRequest);
				return;
			}
			client.SetHeader("Location", Urls[key]);
			client.Response.StatusCode = (int)HttpStatusCode.Found;
			client.Response.Close();
		}

		private static Dictionary<string, string> Convert(this Dictionary<string, object> dict)
		{
			var convDict = new Dictionary<string, string>(dict.Count);
			foreach(var item in dict)
				convDict[item.Key] = (string)item.Value;
			return convDict;
		}
	}
}
