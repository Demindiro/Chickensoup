using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace ChickenSoup
{
	using System.Threading;
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

		#pragma warning disable 0649
		[Config("ARTICLE_TEMPLATE", LoadFileContents = true)] private static readonly string ArticleTemplate;
		[Config("SUMMARY_SNIPPET" , LoadFileContents = true)] private static readonly string SummarySnippet;
		[Config("COMMENT_SNIPPET" , LoadFileContents = true)] private static readonly string CommentSnippet;
		[Config("SUMMARY_COUNT")] private static int summaryCount;
		#pragma warning restore 0649

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
				return;
			}
			var article = articles[categoryIndex][url[2]];
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
				var response = ArticleTemplate.Replace("{title}", article.Title)
				                              .Replace("{time}", article.Date.ToString())
				                              .Replace("{time(O)}", article.Date.ToString("O"));
				Action<Article, string, string> replaceLinks = (otherArticle, replace, conditional) =>
				{
					if (otherArticle != null)
					{
						response = response.Replace(replace, otherArticle.Url);
						response.Replace(replace, "");
						response.Replace(conditional, "");
					}
					else
					{
						var s = response.IndexOf(conditional, StringComparison.InvariantCulture);
						if (s < 0)
							return;
						var e = response.IndexOf(")}", s + conditional.Length, StringComparison.InvariantCulture) + 2;
				        response = response.Remove(s, e - s);
					}	
				};
				replaceLinks(article.Next, "{next}", "{next??(");
				replaceLinks(article.Previous, "{previous}", "{previous??(");
				response = response.Replace("{content}", File.ReadAllText(path))
				                   .Replace("{comments}", GenerateCommentHtml(tree));
				client.WriteAndClose(response, "html", HttpStatusCode.OK);
			}
			else
			{
				client.Error(HttpStatusCode.NotFound);
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


		private static bool HandleArticleRequest(this HttpListenerContext client, int categoryIndex)
		{
			ThreadPool.QueueUserWorkItem(delegate
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
			});
			return true;
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


		private static string GenerateCommentHtml(CommentTree tree)
		{
			var sb = new StringBuilder();
			for (int i = tree.branches.Count - 1; i >= 0; i--)
				sb.Append(GenerateCommentHtml(tree.branches[i].comment, GenerateCommentHtml(tree.branches[i])));
			return sb.ToString();
		}

		private static string GenerateCommentHtml(Comment comment, string reply)
		{
			return CommentSnippet.Replace("{time}", comment.date.ToString())
				                 .Replace("{time(O)}", comment.date.ToString("O"))
				                 .Replace("{user}", comment.name)
				                 .Replace("{comment}", comment.comment)
				                 .Replace("{id}", comment.id.ToString())
				                 .Replace("{reply}", reply);
		}
	}
}
