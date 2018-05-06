using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using ChickenSoup;
using ChickenSoup.Plugins;
using Commands;
using Configuration;

namespace ChickenSoup.Redirect
{
	[Plugin("Redirect")]
	public class Redirect : Plugin
	{
		#pragma warning disable 0649
		[Config("REDIRECT_URLS_FILE")] public static string urlFilePath;
		#pragma warning restore 0649

		public static readonly Dictionary<string, string> Urls = new Dictionary<string, string>();
		private static new TextWriter Writer;

		protected override void Init()
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
			Writer = base.Writer;
			Http.AddListener("rdr", RedirectClient);
		}

		private static bool RedirectClient(HttpListenerContext context)
		{
			ThreadPool.QueueUserWorkItem(delegate
			{
				var url = context.Request.Url.Segments;
				if (url.Length < 3)
				{
					context.Error(HttpStatusCode.BadRequest);
					return;
				}
				var key = context.Request.Url.Segments[2];
				if(!Urls.ContainsKey(key))
				{
					context.Error(HttpStatusCode.BadRequest);
					return;
				}
				context.SetHeader("Location", Urls[key]);
				context.Response.StatusCode = (int)HttpStatusCode.Found;
				context.Response.Close();
			});
			return true;
		}


		[Command("redirect add")]
		private static void Add(string uri, string location)
		{
			if(Urls.ContainsKey(uri))
			{
				Writer.WriteLine($"Redirect URI \"{uri}\" already exists. Overwrite? (Y/N) ");
				if(char.ToUpper(Console.ReadLine()[0]) != 'Y')
					return;
			}
			Urls[uri] = location;
			File.AppendAllText(urlFilePath, uri + ':' + location);
		}

		[Command("redirect remove")]
		private static void Remove(string uri)
		{
			if(!Urls.ContainsKey(uri))
				throw new System.ArgumentException($"URI \"{uri}\" is not defined", nameof(uri));
			Urls.Remove(uri);
			var urls = new string[Urls.Count];
			int i = 0;
			foreach (var item in Urls)
				urls[i++] = item.Key + ':' + item.Value;
			File.WriteAllLines(urlFilePath, urls);
		}
	}
}
