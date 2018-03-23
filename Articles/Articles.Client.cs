using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace ChickenSoup
{
	using Configuration;
	public static partial class Articles
	{
		private class CommentTree
		{
			public Comment comment;
			public int id;
			public List<CommentTree> branches = new List<CommentTree>();

			public CommentTree()
			{ }
			public CommentTree(Comment comment)
			{
				this.comment = comment;
			}

			public bool Add(Comment comment)
			{
				if (comment.replyTo == id)
				{
					Add(new CommentTree(comment));
					return true;
				}
				int count = branches.Count;
				for (int i = 0; i < count; i++)
				{
					if (branches[i].Add(comment))
						return true;
				}
				return false;
			}
			public void Add(CommentTree tree) => branches.Add(tree);
		}

		private static string articleTemplate;
		private static string summarySnippet;
		private static string commentSnippet;

		#pragma warning disable 0649
		[Config("ARTICLE_TEMPLATE")]private static readonly string ArticleTemplatePath;
		[Config("SUMMARY_SNIPPET")] private static readonly string SummarySnippetPath;
		[Config("COMMENT_SNIPPET")] private static readonly string CommentSnippetPath;
		[Config("SUMMARY_COUNT")]   private static int summaryCount;
		#pragma warning restore 0649

		private static string ArticleTemplate => articleTemplate ?? (articleTemplate = File.ReadAllText(ChickenSoup.RootFolder + ArticleTemplatePath));
		private static string SummarySnippet  => summarySnippet  ?? (summarySnippet  = File.ReadAllText(ChickenSoup.RootFolder + SummarySnippetPath));
		private static string CommentSnippet  => commentSnippet  ?? (commentSnippet  = File.ReadAllText(ChickenSoup.RootFolder + CommentSnippetPath));

		private static void GetArticle(this HttpListenerContext client, int categoryIndex)
		{
			var req = client.Request;
			var url = req.Url.Segments;
			if (url.Length < 3)
			{
				client.Error(HttpStatusCode.BadRequest);
				return;
			}
			if (url[url.Length - 1].Contains("."))
			{
				client.GetFile();
			}
			else
			{
				var key = url[2];
				client.GetArticleAsHtml(articles[categoryIndex][key]);
			}
		}


		private static void GetSummary(this HttpListenerContext client, int categoryIndex)
		{
			var req  = client.Request;
			var path = req.Url.AbsolutePath;
			Article art = lastArticles[categoryIndex];

			var sb = new StringBuilder();
			for (var i = 0; i < summaryCount && art != null; i++)
			{
				sb.Append(GetSummaryEntry(art));
				art = art.Previous;
			}

			client.WriteAndClose(sb.ToString(), "html", HttpStatusCode.OK);
		}


		private static void HandleArticleRequest(this HttpListenerContext client, int categoryIndex)
		{
			var r = client.Request;
			var segments = r.Url.Segments;
			
			if (r.HttpMethod == "GET")
			{
				if (segments.Length == 2)
					client.GetSummary(categoryIndex);
				else if (r.QueryString["comments"] != null)
					client.GetComments(categoryIndex);
				else
					client.GetArticle(categoryIndex);
			}
			else if (r.HttpMethod == "POST")
			{
				client.AddComment(categoryIndex);
			}
			else
			{
				client.Error(HttpStatusCode.BadRequest);
			}
		}


		private static string GetSummaryEntry(Article art)
		{
			var sum = GetSummaryText(art);
			return SummarySnippet.Replace("{href}", art.Category + '/' + art.Name)
				                 .Replace("{time}", art.Date.ToString())
				                 .Replace("{time(O)}", art.Date.ToString("O"))
			                     .Replace("{content}", sum);
		}


		private static string GetSummaryText(Article art)
		{
			var sum = File.ReadAllText(ChickenSoup.RootFolder + art.Path);
			var hb = sum.IndexOf("<header>", StringComparison.InvariantCultureIgnoreCase);
			var he = sum.IndexOf("</header>", StringComparison.InvariantCultureIgnoreCase);
			if (hb < 0 || he < hb)
				throw new FormatException("Invalid header tag in file " + art.Path);
			hb += "<header>".Length;
			return sum.Substring(hb, he - hb);
		}


		private static void GetArticleAsHtml(this HttpListenerContext client, Article article)
		{
			var path = ChickenSoup.RootFolder + article.Path;
			if (File.Exists(path))
			{
				var content = File.ReadAllText(path);
				var comments = article.GetComments();
				var tree = new CommentTree();
				tree.id = -1;
				foreach (var comment in comments)
					if (!tree.Add(comment))
						throw new FormatException($"Comment file of {article.Name} has an invalid comment: {comment}");
				client.WriteAndClose(ArticleTemplate.Replace("{title}", article.Title)
				                                    .Replace("{time}", article.Date.ToString())
				                                    .Replace("{time(O)}", article.Date.ToString("O"))
				                                    .Replace("{content}", File.ReadAllText(path))
				                                    .Replace("{comments}", GenerateCommentHtml(tree)),
				                     "html", HttpStatusCode.OK);
			}
			else
			{
				client.Error(HttpStatusCode.NotFound);
			}
		}


		private static string GenerateCommentHtml(CommentTree tree)
		{
			var sb = new StringBuilder();
			foreach (var branch in tree.branches)
			{
				sb.Append(CommentSnippet
				          .Replace("{time}", branch.comment.date.ToString())
				          .Replace("{time(O)}", branch.comment.date.ToString("O"))
				          .Replace("{user}", branch.comment.name)
				          .Replace("{comment}", branch.comment.comment)
				          .Replace("{reply}", GenerateCommentHtml(branch)));
			}
			return sb.ToString();
		}
	}
}
