using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ChickenSoup.Templates
{
	public delegate string TemplateFunction(string args, object data);

	public class Template
	{
		private TemplateFunction[] functions;
		private string[]           arguments;
		private int[]              indices;
		private string             text;

		public Template(string text, Dictionary<string, TemplateFunction> functions)
		{
			int i = 0;
			var funcs = new List<TemplateFunction>();
			var args  = new List<string>();
			var indic = new List<int>();
			var sb = new StringBuilder(text);
			while (true)
			{
				int length = GetNextFunction(sb, ref i);
				if (length < 0)
					break;
				var f = sb.ToString(i + 1, length - 2).Split(new[] {' '}, 2);
				funcs.Add(functions[f[0]]);
				args .Add(f.Length == 2 ? f[1] : null);
				indic.Add(i);
				sb.Remove(i, length);
			}
			this.functions = funcs.ToArray();
			this.arguments = args .ToArray();
			this.indices   = indic.ToArray();
			this.text      = sb.ToString();
		}

		public string Process(object data = null)
		{
			var sb = new StringBuilder(text);
			int offset = 0;
			for (int i = 0; i < functions.Length; i++)
			{
				string val = functions[i](arguments[i], data);
				sb.Insert(indices[i] + offset, val);
				offset += val.Length;
			}
			return sb.ToString();
		}

		private static int GetNextFunction(StringBuilder sb, ref int i)
		{
			while (sb[i] != '{')
			{
				if (sb.Length == ++i)
					return -1;
			}
			int j = i;
			int counter = 1;
			while (counter > 0)
			{
				j++;
				if (sb[j] == '}')
					counter--;
				else if (sb[j] == '{')
					counter++;
			}
			return j - i + 1;
		}
	}
}
