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
		#pragma warning disable 0649
		[Config("COMMENT_MIN_COMMENT_LENGTH")] private static int commentMinCommentLength;
		[Config("COMMENT_MIN_NAME_LENGTH")]    private static int commentMinNameLength;
		#pragma warning restore 0649

		private static void AddComment(this HttpListenerContext client, int categoryIndex)
		{
			var r = client.Request;
			var pathComments       = articleRootFolder + r.Url.AbsolutePath + "-comments";

			string name = null, email = null, text = null;
			int replyTo = -1;
			var buf = new byte[r.ContentLength64];
			var query = Encoding.UTF8.GetString(buf, 0, r.InputStream.Read(buf, 0, buf.Length));
			var param = query.Split('&');
			foreach(var p in param)
			{
				var segments = p.Split('=');
				if (segments[0] == "name")
					name = Uri.UnescapeDataString(segments[1]);
				else if (segments[0] == "email")
					email = Uri.UnescapeDataString(segments[1]);
				else if (segments[0] == "comment")
					text = Uri.UnescapeDataString(segments[1]);
				else if (segments[0] == "replyTo")
					replyTo = int.Parse(segments[1]);
			}
			if (name == null || text == null)
			{
				client.Error(HttpStatusCode.BadRequest);
				return;
			}
			
			var comment = new Comment(name, text, replyTo);
			if (!ValidateComment(comment))
				goto error;

			using (var file = File.Open(pathComments, FileMode.Append))
				comment.Serialize(file);
			client.Close(HttpStatusCode.Created);

			return;
			error:
			client.Error(HttpStatusCode.BadRequest);
		}

		private static bool ValidateComment (Comment comment)
		{
			if (comment.name.Length < commentMinNameLength ||
			    comment.comment.Trim().Length < commentMinCommentLength)
				return false;
			return true;
		}


		private static void GetComments (this HttpListenerContext client, int categoryIndex)
		{
			var name = client.Request.Url.Segments[1];
			var path = articleRootFolder + categories[categoryIndex] + name + "-comments";
			client.WriteAndClose(File.ReadAllBytes(path), "json", HttpStatusCode.OK);
		}
	}
}
