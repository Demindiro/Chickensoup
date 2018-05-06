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
		private static readonly string PluginsFolder = 
		 #if DEBUG
		 "../../Plugins/bin";
		 #else
		 "plugins";
		 #endif

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
			for (int i = 0; i < list.Count; i++)
			{
				var constructor = list[i].GetType().GetConstructor(new Type[0]);
					/*
					list[i].GetType().GetConstructor(
					BindingFlags.Public | BindingFlags.NonPublic,
					null,
					new Type[0],
					null
				);*/
				constructor.Invoke(list[i], new object[0]);
			}
			Configuration.Config.ReadConfigFile(false);
			for (int i = 0; i < list.Count; i++)
				list[i].Init();
		}
	}
}
