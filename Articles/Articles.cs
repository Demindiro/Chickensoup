using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace ChickenSoup
{
	using Configuration;
	public static partial class Articles
	{
		#pragma warning disable 0649
		[Config("ARTICLE_ROOT_FOLDER")] private static string articleRootFolder;
		[Config("ARTICLE_URLS")] private static string[] categories;
		#pragma warning restore 0649
		private static Dictionary<string, Article>[] articles;
		private static Article[] lastArticles;
		private static HttpListener[] servers;


		public static string[] Categories
		{
			get => categories;
			set
			{
				if (categories != null)
					throw new InvalidOperationException("Categories list has already been set");
				categories = value;
			}
		}


		public static void Init()
		{
			if (articles != null)
				throw new InvalidOperationException("Articles has already been initialized");
			articles     = new Dictionary<string, Article>[categories.Length];
			lastArticles = new Article[categories.Length];
			if (!articleRootFolder.EndsWith("/", StringComparison.InvariantCulture))
				articleRootFolder += "/";
			for(var i = 0; i < articles.Length; i++)
			{
				articles[i] = new Dictionary<string, Article>();
				var path = articleRootFolder + categories[i];
				if(!Directory.Exists(path))
					Directory.CreateDirectory(path);
				path += "/list.txt";
				if(!File.Exists(path))
				{
					File.Create(path);
					break;
				}
				lastArticles[i] = AddArticles(i, File.ReadAllLines(path));
			}
			servers = new HttpListener[categories.Length];
			for (int i = 0; i < categories.Length; i++)
			{
				servers[i] = new HttpListener();
				servers[i].Prefixes.Add($"http://*:{ChickenSoup.Port}/{categories[i]}/");
				servers[i].Start();
				servers[i].BeginGetContext(HandleNewArticleRequest, i);
			}
		}

		private static void HandleNewArticleRequest(IAsyncResult ar)
		{
			var i = (int)ar.AsyncState;
			var client = servers[i].EndGetContext(ar);
			ThreadPool.QueueUserWorkItem(ThreadPool_HandleArticleRequest, new object[] {client, i});
			servers[i].BeginGetContext(HandleNewArticleRequest, ar.AsyncState);
		}

		private static void ThreadPool_HandleArticleRequest(object state)
		{
			var obj = (object[])state;
			var client = (HttpListenerContext)obj[0];
			client.HandleArticleRequest((int)obj[1]);
		}

		private static Article AddArticles(int index, string[] list)
		{
			Article art = null;
			foreach(var entry in list)
			{
				if(entry != "")
				{
					art = new Article(entry, categories[index]);
					articles[index][art.Name] = art;
				}
			}
			return art;
		}

		private static int GetCategoryIndex(string category)
		{
			for(int i = 0; i < categories.Length; i++)
				if(categories[i] == category)
					return i;
			return -100;
		}
	}
}
