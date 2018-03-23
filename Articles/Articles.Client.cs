using System;
using System.IO;
using System.Net;
using System.Text;

namespace ChickenSoup
{
	using Configuration;
	public static partial class Articles
	{
		private delegate string GetEntry(Article article);

		private static string articleTemplate;
		private static string summarySnippet;

		#pragma warning disable 0649
		[Config("ARTICLE_TEMPLATE")]private static readonly string ArticleTemplatePath;
		[Config("SUMMARY_SNIPPET")] private static readonly string SummarySnippetPath;
		[Config("SUMMARY_COUNT")]   private static int summaryCount;
		#pragma warning restore 0649

		private static string ArticleTemplate => articleTemplate ?? (articleTemplate = File.ReadAllText(ChickenSoup.RootFolder + ArticleTemplatePath));
		private static string SummarySnippet  => summarySnippet  ?? (summarySnippet  = File.ReadAllText(ChickenSoup.RootFolder + SummarySnippetPath));

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

			GetEntry getEntry;
			getEntry = GetSummaryEntryAsHtml;
			
			var sb = new StringBuilder();
			for (var i = 0; i < summaryCount && art != null; i++)
			{
				sb.Append(getEntry(art));
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
				if (segments.Length <= 2)
					client.AddComment(categoryIndex);
			}
			else
			{
				client.Error(HttpStatusCode.BadRequest);
			}
		}


		private static string GetSummaryEntryAsHtml(Article art)
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
				var commentsHtml = "";
				client.WriteAndClose(ArticleTemplate.Replace("{title}", article.Title)
				                                    .Replace("{time}", article.Date.ToString())
				                                    .Replace("{time(O)}", article.Date.ToString("O"))
				                                    .Replace("{content}", File.ReadAllText(path))
				                                    .Replace("{comments}", commentsHtml),
				                     "html", HttpStatusCode.OK);
				return;
			}
			else
			{
				client.Error(HttpStatusCode.NotFound);
			}
		}

	}
}
