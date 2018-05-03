using System;
using System.IO;
using static System.Text.Encoding;

namespace ChickenSoup
{
	public struct Comment
	{
		public string name;
		public string comment;
		public DateTime date;
		public int replyTo;
		public int id;

		public Comment(string name, string comment, int replyTo, DateTime date, int id)
		{
			this.name = name;
			this.date = date;
			this.comment = comment
				.Replace("<", "&lt;")
				.Replace(">", "&rt;");
			this.replyTo = replyTo;
			this.id = id;
		}

		public Comment(string name, string comment, int id, int replyTo) :
		this(name, comment, replyTo, DateTime.Now, id)
		{ }

		public Comment(string name, string comment, int id) :
		this(name, comment, -1, id)
		{ }

		internal Comment(byte[] data, ref int index, int id)
		{
			long ticks = 0;
			for (int i = 0; i < 8; i++)
				ticks += ((long)data[index++] << (i * 8));
			date = new DateTime(ticks);
			int len = data[index++];
			name = UTF8.GetString(data, index, len);
			index += len;
			replyTo = 0;
			for (int i = 0; i < 4; i++)
				replyTo += (data[index++] << (i * 8));
			len = 0;
			for (int i = 0; i < 2; i++)
				len += (data[index++] << (i * 8));
			comment = UTF8.GetString(data, index, len);
			index += len;
			this.id = id;
		}

		public override string ToString()
		{
			return $"[{date}] <{name}> {comment}";
		}

		internal void Serialize(Stream stream)
		{
			long ticks = date.Ticks;
			for (int i = 0; i < 8; i++)
				stream.WriteByte((byte)(ticks >> (i * 8)));
			var array = UTF8.GetBytes(name);
			stream.WriteByte((byte)array.Length);
			stream.Write(array, 0, array.Length);
			for (int i = 0; i < 4; i++)
				stream.WriteByte((byte)((uint)replyTo >> (i * 8)));
			array = UTF8.GetBytes(comment);
			for (int i = 0; i < 2; i++)
				stream.WriteByte((byte)(array.Length >> (i * 8)));
			stream.Write(array, 0, array.Length);
		}
	}
}
