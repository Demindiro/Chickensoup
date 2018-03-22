using System;
using System.Collections.Generic;
using System.IO;

namespace ChickenSoup
{
	using Configuration;
	public static partial class Articles
	{
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
			if (!File.Exists(fpath + "-comments.json"))
			{
				File.WriteAllText(path + "-comments.json", "[]");
				File.WriteAllText(path + "-comments-details.json", "[]");
			}

			articles[i][name] = new Article(name, title, category, path, DateTime.Now);
			Logger.Log($"{name} added in {category}", "Base");
			Console.WriteLine($"\x1b[32m{title} added on {DateTime.Now}\x1b[0m");
		}


		public static void ParseCommand(string input)
		{
			string[] v = input.Split(' ');
			if (v[1] == "add")
			{
				if (v.Length < 6)
					throw new ArgumentException("Usage: article add <category> <name> <title> <path>");
				AddArticle(v[2], v[3], v[4], v[5]);
				return;
			}
			if (v[1] == "remove")
			{
				if (v.Length < 4)
					throw new ArgumentException("Usage: article remove <category> <name>");
				RemoveArticle(v[2], v[3]);
				return;
			}
			throw new ArgumentException("Usage: article add|remove [...]");
		}


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
			articles[i].Remove(name);
			Console.WriteLine($"\x1b[32mArticle \"{name}\" in category " +
			                  $"\"{category}\" has been removed\x1b[0m");
		}

		/*
		private static string[] SplitIntoArguments(this string str)
		{
			var argv = new List<string>();
			int i;
			for(i = 0; i < str.Length; i++)
			{
				if(str[i] == '"')
				{
					i++;
					var j = str.IndexOf('"', i);
					argv.Add(str.Substring(i, j - i));
					i = j + 1;
				}
				else
				{
					var j = str.IndexOf(' ', i);
					if(j < 0)
					{
						argv.Add(str.Substring(i));
						break;
					}
					argv.Add(str.Substring(i, j - i));
					i = j;
				}
			}
			return argv.ToArray();
		}
		*/
	}
}
