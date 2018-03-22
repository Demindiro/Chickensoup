using System;
namespace ChickenSoup
{
	public static partial class ChickenSoup
	{

		/// <summary>
		/// Parses a command given as a string.
		/// </summary>
		/// <param name="input">A command represented as a string</param>
		private static void ParseCommand(string input)
		{
			if (input.StartsWith("article "))
			{
				Articles.ParseCommand(input);
			}
			else if (input.StartsWith("config "))
			{
				Config.ParseCommand(input);
			}
			else if (input.StartsWith("redirect "))
			{
				Redirect.ParseCommand(input);
			}
			else
			{
				throw new ArgumentException("Usage: article|config|redirect <...>");
			}
		}
	}
}
