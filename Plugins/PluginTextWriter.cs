using System;
using System.IO;
using System.Text;

namespace ChickenSoup.Plugins
{
	internal class PluginTextWriter : TextWriter
	{
		private string name;
		private string prefix;
		private bool startOfNewLine;

		public override Encoding Encoding => Encoding.UTF8;

		internal PluginTextWriter(string name)
		{
			this.name   = name;
			this.prefix = $"[{name}] ";
			this.startOfNewLine = true;
		}

		public override void Write(char value)
		{
			if (startOfNewLine)
			{
				Console.Write(prefix);
				startOfNewLine = false;
			}
			if (value == '\n')
				startOfNewLine = true;
			Console.Write(value);
		}
	}
}
