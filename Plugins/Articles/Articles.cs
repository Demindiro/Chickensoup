using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using ChickenSoup.Plugins;
using Commands;
using Configuration;

namespace ChickenSoup.Articles
{
	[Plugin("Articles")]
	public partial class Articles : Plugin
	{
		#pragma warning disable 0649
		[Config("ARTICLE_ROOT_FOLDER")] private static string articleRootFolder;
		[Config("ARTICLE_URLS")]        private static string[] categories;
		#pragma warning restore 0649
		private static Dictionary<string, Article>[] articles;
		private static Article[] lastArticles;
		private static HttpListener[] servers;
		private static ReadOnlyCollection<Article> ro_lastArticles;


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
		public static ReadOnlyCollection<Article> LastArticles => ro_lastArticles;
		public static string RootFolder => articleRootFolder;

		public Articles()
		{
			ChickenSoup.Functions.Add("summary", GenerateShortSummary);
		}

		protected override void Init()
		{
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
				Http.AddListener(categories[i], (context) => HandleArticleRequest(context, index));
			}
			ro_lastArticles = Array.AsReadOnly<Article>(lastArticles);
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
		static void AddArticle(string category, string name, string title, string path)
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
		static void RemoveArticle(string category, string name)
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

		private static string GenerateShortSummary(string args, object data)
		{
			var sb = new StringBuilder();
			for (int i = 0; i < Articles.LastArticles.Count; i++)
			{
				var article = Articles.LastArticles[i];
				// TODO move magic constant to settings
				for (int j = 0; j < 3 && article != null; j++)
				{
					sb.Append(args.Replace("{link}", article.Path).Replace("{title}", article.Title));
					article = article.Previous;	
				}
				sb.Append("<br>");
			}
			return sb.ToString();
		}
	}
}
