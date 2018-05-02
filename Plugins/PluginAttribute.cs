using System;
namespace ChickenSoup.Plugins
{
	public class PluginAttribute : Attribute
	{
		private string name;
		public string Name => name;

		public PluginAttribute(string name)
		{
			this.name = name;
		}
	}
}
