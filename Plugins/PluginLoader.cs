using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using Configuration;

namespace ChickenSoup.Plugins
{
	internal static class PluginLoader
	{
		[Config("PLUGINS_FOLDER")] private static readonly string PluginsFolder;

		internal static void LoadPlugins()
		{
			var list = new List<Plugin>();
			foreach (var file in Directory.GetFiles(PluginsFolder, "*.dll", SearchOption.AllDirectories))
			{
				var assembly = Assembly.LoadFrom(file);
				foreach (var type in assembly.GetTypes())
				{
					var attribute = type.GetCustomAttribute<PluginAttribute>();
					if (attribute == null)
						continue;
					if (type.IsAssignableFrom(typeof(Plugin)))
						throw new TypeLoadException($"{attribute.Name} must inherit from Plugin");
					var plugin = (Plugin)FormatterServices.GetUninitializedObject(type);
					plugin.Writer = new PluginTextWriter(attribute.Name);
					list.Add(plugin);
				}
			}
			Configuration.Config.ReadConfigFile(false);
			for (int i = 0; i < list.Count; i++)
			{
				var constructor = list[i].GetType().GetConstructor(new Type[0]);
				constructor.Invoke(list[i], new object[0]);
			}
		}
	}
}
