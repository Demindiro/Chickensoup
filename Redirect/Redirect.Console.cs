using System;
using System.Collections.Generic;
using System.IO;

namespace ChickenSoup
{
	public static partial class Redirect
	{
		internal static void ParseCommand(string input)
		{
			var segments = input.Split(' ');
			if(segments[1] == "add")
			{
				if(segments.Length < 4)
					throw new ArgumentException("Usage: redirect add <uri> <location>");
				Add(segments[2], segments[3]);
			}
			else if(segments[1] == "remove")
			{
				if(segments.Length < 4)
					throw new ArgumentException("Usage: redirect remove <uri>");
				Remove(segments[2]);
			}
			else
			{
				throw new ArgumentException("Usage: redirect <add|remove> <uri> [...]");
			}
		}

		private static void Add(string uri, string location)
		{
			if(Urls.ContainsKey(uri))
			{
				Console.WriteLine($"Redirect URI \"{uri}\" already exists. Overwrite? (Y/N) ");
				if(char.ToUpper(Console.ReadLine()[0]) != 'Y')
					return;
			}
			Urls[uri] = location;
			File.AppendAllText(urlFilePath, uri + ':' + location);
		}

		private static void Remove(string uri)
		{
			if(!Urls.ContainsKey(uri))
				throw new ArgumentException($"URI \"{uri}\" is not defined", nameof(uri));
			Urls.Remove(uri);
			var urls = new string[Urls.Count];
			int i = 0;
			foreach (var item in Urls)
				urls[i++] = item.Key + ':' + item.Value;
			File.WriteAllLines(urlFilePath, urls);
		}

		private static Dictionary<string, object> Convert(this Dictionary<string, string> dict)
		{
			var convDict = new Dictionary<string, object>(dict.Count);
			foreach(var item in dict)
				convDict[item.Key] = item.Value;
			return convDict;
		}
	}
}
