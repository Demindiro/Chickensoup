﻿using System;
using System.IO;
using static System.Text.Encoding;

namespace ChickenSoup
{
	public static partial class Articles
	{
		private struct Comment
		{
			public string name;
			public string comment;
			public DateTime date;
			public int replyTo;

			public Comment(string name, string comment, int replyTo, DateTime date)
			{
				this.name = name;
				this.date = date;
				this.comment = comment
					.Replace("<", "&lt;")
					.Replace(">", "&rt;");
				this.replyTo = replyTo;
			}

			public Comment(string name, string comment, int replyTo) :
			this(name, comment, replyTo, DateTime.Now)
			{ }

			public Comment(string name, string comment) :
			this(name, comment, -1)
			{ }

			internal Comment(byte[] data, ref int index)
			{
				long ticks = 0;
				for (int i = 0; i < 8; i++)
					ticks += (data[index++] << (i * 8));
				date = new DateTime(ticks);
				int len = data[index++];
				name = UTF8.GetString(data, index, len);
				index += len;
				replyTo = 0;
				for (int i = 0; i < 4; i++)
					replyTo += (data[index++] << (i * 8));
				len = 0;
				for (int i = 0; i < 2; i++)
					replyTo += (data[index++] << (i * 8));
				comment = UTF8.GetString(data, index, len);
			}

			public override string ToString()
			{
				return $"[{date}] <{name}> {comment}";
			}

			internal void Serialize(Stream stream)
			{
				long ticks = date.Ticks;
				for (int i = 0; i < 8; i++)
					stream.WriteByte ((byte)(ticks >> (i * 8)));
				var array = UTF8.GetBytes(name);
				stream.Write(array, 0, array.Length);
				replyTo = 0;
				for (int i = 0; i < 4; i++)
					stream.WriteByte ((byte)(replyTo >> (i * 8)));
				array = UTF8.GetBytes(comment);
				for (int i = 0; i < 2; i++)
					stream.WriteByte((byte)(array.Length >> (i * 8)));
				stream.Write(array, 0, array.Length);
			}
		}
	}
}