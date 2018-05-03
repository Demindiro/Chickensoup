using System;
using System.IO;
using System.Net;
using System.Text;
using Configuration;

namespace ChickenSoup.Articles
{
	public partial class Articles
	{
		#pragma warning disable 0649
		[Config("COMMENT_MIN_COMMENT_LENGTH")] private static int commentMinCommentLength;
		[Config("COMMENT_MIN_NAME_LENGTH")]    private static int commentMinNameLength;
		#pragma warning restore 0649

		private static void AddComment(HttpListenerContext client, int categoryIndex)
		{
			var r = client.Request;

			string name = null, email = null, text = null;
			int replyTo = -1;
			var buf = new byte[r.ContentLength64];
			var query = Encoding.UTF8.GetString(buf, 0, r.InputStream.Read(buf, 0, buf.Length));
			var param = query.Split('&');
			foreach(var p in param)
			{
				var segments = p.Split(new[] { '=' }, 2);
				if (segments.Length < 2)
				{
					client.Error(HttpStatusCode.BadRequest);
					return;
				}
				var value = Uri.UnescapeDataString(segments[1]).Replace('+', ' ');
				if (segments[0] == "name")
					name = value;
				else if (segments[0] == "email")
					email = value;
				else if (segments[0] == "comment")
					text = value;
				else if (segments[0] == "replyTo")
					replyTo = int.Parse(segments[1]);
			}
			if (name == null || text == null)
			{
				client.Error(HttpStatusCode.BadRequest);
				return;
			}
			
			var article = articles[categoryIndex][r.Url.Segments[2]];
			var comment = new Comment(name, text, article.CommentCount, replyTo);
			if (!Validate(comment, article))
			{
				client.Error(HttpStatusCode.BadRequest);
				return;
			}
			article.AddComment(comment);

			if (client.Request.Url.Query.Contains("returnSelf"))
			{
				client.WriteAndClose(GenerateCommentHtml(comment, ""), "html", HttpStatusCode.Created);
			}
			else
			{
				// This should prevent accidently submitting a form multiple times
				client.SetHeader("Location", client.Request.Url);
				client.Close(HttpStatusCode.SeeOther);
			}
		}

		private static bool Validate (Comment comment, Article article)
		{
			if (comment.name.Length < commentMinNameLength ||
			    comment.comment.Trim().Length < commentMinCommentLength ||
			    comment.replyTo >= article.CommentCount) // >= because a comment can't be a reply to itself
				return false;
			return true;
		}


		private static void GetComments (HttpListenerContext client, int categoryIndex)
		{
			var name = client.Request.Url.Segments[1];
			var path = articleRootFolder + categories[categoryIndex] + name + "-comments";
			client.WriteAndClose(File.ReadAllBytes(path), "json", HttpStatusCode.OK);
		}
	}
}
