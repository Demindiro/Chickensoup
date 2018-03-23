using System;
using System.Collections.Generic;
using System.IO;

namespace ChickenSoup
{
	public static partial class Articles
	{
		private class Article
		{
			public readonly string Name;
			public readonly string Title;
			public readonly string Category;
			public readonly string Path;
			public readonly DateTime Date;
			public readonly Article Next;
			public readonly Article Previous;

			public Article(string name, string title, string category, string path,
							 DateTime date, Article next = null, Article previous = null)
			{
				Name = name;
				Title = title;
				Category = category;
				Path = path;
				Date = date;
				Next = next;
				Previous = previous;
			}

			public Article(string data, string category)
			{
				var f = data.Split(';');
				if (f.Length != 4)
					throw new ArgumentException("Data string is malformed", nameof(data));
				Name  = f[0];
				Title = f[1];
				Path  = f[2];
				Date  = DateTime.Parse(f[3]);
				Category = category;
			}

			public Comment[] GetComments()
			{
				var path = articleRootFolder + Name + "-comments";
				if (!File.Exists(path))
					return new Comment[0];
				byte[] data = File.ReadAllBytes(path);
				int count = 0;
				int index = 0;
				for (int i = 0; i < 4; i++)
					count += (data[index++] << (i * 8));
				var comments = new Comment[count];
				for (int i = 0; i < count; i++)
					comments[i] = new Comment(data, ref index);
				return comments;
			}

			public bool AddComment()
			{
				return false;
			}
		}
	}
}
