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

		private static string summarySnippet;

		#pragma warning disable 0649
		//[Config("SUMMARY_SNIPPET")] private static readonly string SummarySnippetPath;
		[Config("SUMMARY_COUNT")] private static int summaryCount;
		#pragma warning restore 0649

		//private static string SummarySnippet = summarySnippet ?? (summarySnippet = File.ReadAllText(SummarySnippetPath));

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


		private static string GetSummaryEntryAsJson(Article art)
		{
			var sum = GetSummaryText(art);
			return "{" +
			       $"\"link\":\"{art.Category}/{art.Name}\"," + 
			       $"\"date\":\"{art.Date.ToString("O")}\"," +
			       $"\"title\":\"{art.Title}\"," +
			       $"\"summary\":\"{sum.Replace("\n","\\n").Replace("\t","\\t")}\"" +
			       "},";
		}


		private static string GetSummaryEntryAsHtml(Article art)
		{
			var sum = GetSummaryText(art);
			return $"<a href=\"/{art.Category}/{art.Name}?format=html\">" +
			       $"<time datetime=\"{art.Date.ToString("O")}\">{art.Date.ToString("yy-MM-dd hh:mm:ss")}</time>" +
			       $"<article>{sum}</article>" +
			       $"</a>";
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


		private static void GetArticleAsJson(this HttpListenerContext client, Article art)
		{
			client.Write($"\"link\":\"{art.Path}\"" +
			             $",\"title\":\"{art.Title}\"" +
			             $",\"date\":\"{art.Date.ToString("O")}\"" +
			             (art.Previous == null ? "" : $",\"previous\":\"{art.Previous.Name}\"") +
			             (art.Next     == null ? "" : $",\"next\":\"{    art.Next.Name}\"") +
						 "}", "json");
		}


		private static void GetArticleAsHtml(this HttpListenerContext client, Article art)
		{
			var path = ChickenSoup.RootFolder + art.Path;
			if (File.Exists(path))
			{
				client.WriteAndClose($"<title>{art.Title}</title>" +
				             (art.Previous == null ? "" : $"<a href=\"{art.Previous.Name}?format=html\">Previous</a>") +
				             (art.Next     == null ? "" : $"<a href=\"{art.Next    .Name}?format=html\">Next</a>") +
				             $"<time datetime=\"{art.Date.ToString("O")}\">{art.Date.ToString("yy-MM-dd hh:mm:ss")}</time>" +
				             $"<article>{File.ReadAllText(path)}</article>", "html", HttpStatusCode.OK);
			}
			else
			{
				client.Error(HttpStatusCode.NotFound);
			}
		}

	}
}
