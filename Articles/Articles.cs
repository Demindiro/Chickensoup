using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Commands;

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
				var index = i;
				Http.AddListener(categories[i], (context) => context.HandleArticleRequest(index));
			}
		}

		private static Article AddArticles(int category, string[] list)
		{
			Article art = null, prevArt = null;
			foreach(var entry in list)
			{
				if(entry != "")
				{
					art = new Article(entry, categories[category]);
					articles[category][art.Name] = art;
					if (prevArt != null)
						prevArt.Next = art;
					art.Previous = prevArt;
					prevArt = art;
				}
			}
			return art;
		}


		[Command("article add")]
		public static void AddArticle(string category, string name, string title, string path)
		{
			var i = GetCategoryIndex(category);
			if (i < 0)
				throw new ArgumentException($"Category \"{category}\" does not exist", nameof(category));
			if (articles[i].ContainsKey(name))
				throw new ArgumentException($"Article \"{name}\" in \"{category}\" does already exist", nameof(name));

			var fpath = articleRootFolder + category + "/";
			File.AppendAllText(fpath + "list.txt", $"{name};{title};{path};{DateTime.Now.ToString("O")}\n");
			fpath += name;

			var article = articles[i][name] = new Article(name, title, category, path, DateTime.Now);
			article.Previous = lastArticles[i];
			lastArticles[i] = article;
			Logger.Log($"{name} added in {category}");
			Console.WriteLine($"\x1b[32m{title} added on {DateTime.Now}\x1b[0m");
		}

		[Command("article remove")]
		public static void RemoveArticle(string category, string name)
		{
			var i = GetCategoryIndex(category);
			if (i < 0)
				throw new ArgumentException($"URL \"{category}\" is not an article URL");
			if (!articles[i].ContainsKey(name))
				throw new ArgumentException($"Article {name} in {category} doesn't exist", nameof(name));

			Console.Write($"Are you sure you want to remove article \"{name}\"" +
			              $"from category \"{category}\"? (Y/N) ");
			if (char.ToUpper(Console.ReadLine()[0]) != 'Y')
				return;

			var path = articleRootFolder + category + "/list.txt";
			var list = File.ReadAllText(path);
			var startIndex = list.IndexOf('\n' + name, StringComparison.InvariantCulture) + 1;
			var endIndex   = list.IndexOf('\n', startIndex) + 1;
			File.WriteAllText(path, list.Remove(startIndex, endIndex - startIndex));

			var article = articles[i][name];
			if (article.Next != null)
				article.Next.Previous = article.Previous;
			if (article.Previous != null)
				article.Previous.Next = article.Next;
			articles[i].Remove(name);
			Console.WriteLine($"\x1b[32mArticle \"{name}\" in category " +
			                  $"\"{category}\" has been removed\x1b[0m");
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
