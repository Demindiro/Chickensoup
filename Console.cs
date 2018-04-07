using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ChickenSoup
{
	/*
	 * Similar to the default, but it support autocomplete (for files) 
	 */
	public static class Console
	{
		public static TextWriter Error => System.Console.Error;
		public static TextWriter Out   => System.Console.Out;

		public static string ReadLine()
		{
			var sb = new StringBuilder();
			while (true)
			{
				var keyInfo = System.Console.ReadKey(true);
				var c = keyInfo.KeyChar;
				switch (keyInfo.Key)
				{
				case ConsoleKey.Tab:
					AutoCompleteInput(sb);
					break;
				case ConsoleKey.Backspace: // Backspace
					if (sb.Length > 0)
					{
						sb.Remove(sb.Length - 1, 1);
						Write("\b ");
						System.Console.CursorLeft--;
					}
					break;
				case ConsoleKey.Enter:
					WriteLine();
					goto finish;
				default:
					sb.Append(c);
					Write(c);
					break;
				}
			}
			finish:
			return sb.ToString();
		}

		public static void Write(object obj) => System.Console.Write(obj);
		public static void WriteLine(object obj) => System.Console.WriteLine(obj);
		public static void WriteLine() => System.Console.WriteLine();

		private static void AutoCompleteInput(StringBuilder sb)
		{
			var str = sb.ToString();
			str = str.Substring(str.LastIndexOf(' ') + 1);
			var folder = str.Substring(0, str.LastIndexOf('/') + 1);
			if (folder == "")
				folder = ".";
			var fileNameStart = str.Substring(folder.Length > 0 ? folder.Length - 1 : 0);
			var matches = new List<string>();
			if (Directory.Exists(folder))
			{
				foreach (var item in Directory.EnumerateFileSystemEntries(folder))
				{
					var file = item.Substring(item.LastIndexOf('/') + 1);
					if (file.StartsWith(fileNameStart, StringComparison.InvariantCulture))
						matches.Add(file);
				}
			}
			if (matches.Count == 0)
			{
				System.Console.Beep();
				return;
			}
			var match = matches[0];
			for (int i = 1; i < matches.Count; i++)
			{
				int j;
				for (j = 0; j < match.Length && j < matches[i].Length; j++)
					if (match[j] != matches[i][j])
						break;
				if (match.Length != j)
					match = match.Substring(0, j);
				if (match.Length == 0)
				{
					System.Console.Beep();
					return;
				}
			}
			var append = match.Substring(fileNameStart.Length) + (matches.Count == 1 ? " " : "");
			sb.Append(append);
			System.Console.Write(append);
		}
	}
}
