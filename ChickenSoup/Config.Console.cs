using System;

namespace ChickenSoup
{
	internal static class Config
	{
		internal static void ParseCommand(string input)
		{
			if (input.EndsWith(" reload"))
				Configuration.Config.ReadConfigFile();
			/*
			else if (input.EndsWith(" show"))
				PrintSettings();*/
			else
				throw new ArgumentException("Usage: config <reload|show>");
		}
	}
}
