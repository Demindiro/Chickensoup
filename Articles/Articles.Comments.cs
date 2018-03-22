using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using JSON;

namespace ChickenSoup
{
	using Configuration;
	public static partial class Articles
	{
		private struct Comment
		{
			public string name;
			public string comment;
			public DateTime date;
			public int replyTo;

			public Comment (string name, string comment, int replyTo, DateTime date)
			{
				this.name = name;
				this.date = date;
				this.comment = comment
					.Replace("<", "&lt;")
					.Replace(">", "&rt;")
					.Replace("\n","\\n"); // TODO need something a little more efficient
				this.replyTo = replyTo;
			}

			public Comment (string name, string comment, int replyTo) :
			this (name, comment, replyTo, DateTime.Now)
			{ }

			public Comment (string name, string comment) :
			this (name, comment, -1)
			{ }

			public override string ToString ()
			{
				return
					"{" +
					$"\"name\":\"{name}\"," + 
					$"\"date\":\"{date.ToString ("O")}\"," +
					$"\"comment\":\"{comment}\"" +
					(replyTo < 0 ? "" : $",\"replyTo\":{replyTo}") +
					"}";
			}
		}


		private struct CommentDetails
		{
			public string email;
			public IPAddress ip;

			public CommentDetails (string email, IPAddress ip)
			{
				this.email = email;
				this.ip = ip;
			}

			public override string ToString ()
			{
				return 
					"{" +
					(email == "" ? "" : $"\"email\":\"{email}\"") +
					$"\"ip\":\"{ip}\"" +
					"}";
			}
		}

		#pragma warning disable 0649
		[Config("COMMENT_MIN_COMMENT_LENGTH")] private static int commentMinCommentLength;
		[Config("COMMENT_MIN_NAME_LENGTH")] private static int commentMinNameLength;
		#pragma warning restore 0649

		private static void AddComment(this HttpListenerContext client, int categoryIndex)
		{
			var r = client.Request;
			var pathComments       = articleRootFolder + r.Url.AbsolutePath + "-comments.json";
			var pathCommentDetails = articleRootFolder + r.Url.AbsolutePath + "comment-details.json";

			var buf = new byte[r.ContentLength64];
			var str = Encoding.UTF8.GetString(buf, 0, r.InputStream.Read(buf, 0, buf.Length));
			if(Json.TryParse(str, out Dictionary<string, object> dict) < 0)
				goto error;
			if (!(dict.TryGetValue("name"   , out string name) &&
			      dict.TryGetValue("comment", out string message) &&
				  dict.TryGetValue("email"  , out string email)))
				goto error;
			if (!dict.TryGetValue("replyTo", out double replyTo))
				replyTo = -1;
			
			var comment = new Comment(name, message, (int)replyTo);
			var commentDetails = new CommentDetails(email, r.RemoteEndPoint.Address);
			var num = CheckComment(comment, commentDetails);
			if (num > 0)
				goto error;

			var commentStr = comment.ToString();
			WriteToFile(pathComments      , commentStr);
			WriteToFile(pathCommentDetails, commentDetails.ToString());
			client.Close(HttpStatusCode.Created);

			return;
			error:
			client.Error(HttpStatusCode.BadRequest);
		}

		private static int CheckComment (Comment comment, CommentDetails commentDetails)
		{
			if (comment.name.Length < commentMinNameLength ||
			    comment.comment.Trim().Length < commentMinCommentLength)
				return 112;
			return 0;
		}


		private static void GetComments (this HttpListenerContext client, int categoryIndex)
		{
			var name = client.Request.Url.Segments[1];
			var path = articleRootFolder + categories[categoryIndex] + name + "-comments.json";
			client.WriteAndClose(File.ReadAllBytes(path), "json", HttpStatusCode.OK);
		}


		private static void WriteToFile (string path, string jsonStr)
		{
			using (FileStream fileStream = File.Open (path, FileMode.OpenOrCreate))
			{
				string text = jsonStr + "]";
				if (fileStream.Length > 0)
				{
					fileStream.Seek (-2, SeekOrigin.End);
					int num = fileStream.ReadByte ();
					if (num != ',' && num != '[')
						text = "," + text;
				}
				byte[] bytes = Encoding.UTF8.GetBytes (text);
				fileStream.Write (bytes, 0, bytes.Length);
			}
		}


	    private static bool TryGetValue<T>(this Dictionary<string, object> dict, string key, out T value)
		{
			object obj;
			if(dict.TryGetValue(key, out obj) && obj is T)
			{
				value = (T)obj;
				return true;
			}
			value = default(T);
			Console.WriteLine(obj.GetType());
			Console.WriteLine(dict.TryGetValue(key, out obj));
			Console.WriteLine(obj is T);
			return false;
		}
	}
}
