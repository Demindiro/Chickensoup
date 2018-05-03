using System;
using System.Collections.Generic;
using System.IO;

namespace ChickenSoup.Articles
{
	public class Article
	{
		private int commentCount = -1;
		public readonly string Name;
		public readonly string Title;
		public readonly string Category;
		public readonly string Path;
		public readonly DateTime Date;
		public Article Next;
		public Article Previous;
		public int CommentCount
		{
			get
			{
				if (commentCount < 0)
					return commentCount = GetComments().Length;
				return commentCount;
			}
		}
		public string Url => Category + '/' + Name;

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
			Name = f[0];
			Title = f[1];
			Path = f[2];
			Date = DateTime.Parse(f[3]);
			Category = category;
		}

		public Comment[] GetComments()
		{
			var path = Articles.RootFolder + Category + '/' + Name + "-comments";
			if (!File.Exists(path))
				return new Comment[0];
			byte[] data = File.ReadAllBytes(path);
			var comments = new List<Comment>();
			int index = 0;
			for (int i = 0; index != data.Length; i++)
				comments.Add(new Comment(data, ref index, i));
			return comments.ToArray();
		}

		public bool AddComment(Comment comment)
		{
			var path = Articles.RootFolder + Category + '/' + Name + "-comments";
			using (var file = File.Open(path, FileMode.Append))
				comment.Serialize(file);
			commentCount++;
			return false;
		}
	}
}
