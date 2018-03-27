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
		[Config("REDIRECT_URLS_FILE")] internal static string urlFilePath;
		#pragma warning restore 0649

		public static readonly Dictionary<string, string> Urls = new Dictionary<string, string>();

		public static void Init()
		{
			if(File.Exists(urlFilePath))
			{
				var lines = File.ReadAllLines(urlFilePath);	
				foreach (var line in lines)
				{
					int colonIndex = line.IndexOf(':');
					if (colonIndex >= 0)
						Urls.Add(line.Substring(0, colonIndex), line.Substring(colonIndex + 1));
				}
			}
			else
			{
				File.Create(urlFilePath).Close();
			}
			Http.AddListener("rdr", RedirectClient, true);
		}

		private static bool RedirectClient(HttpListenerContext context)
		{
			var url = context.Request.Url.Segments;
			if (url.Length < 3)
			{
				context.Error(HttpStatusCode.BadRequest);
				return true;
			}
			var key = context.Request.Url.Segments[2];
			if(!Urls.ContainsKey(key))
			{
				context.Error(HttpStatusCode.BadRequest);
				return true;
			}
			context.SetHeader("Location", Urls[key]);
			context.Response.StatusCode = (int)HttpStatusCode.Found;
			context.Response.Close();
			return true;
		}
	}
}
