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
			var plugs = new List<Plugin>();
			var attrs = new List<PluginAttribute>();
			foreach (var file in Directory.GetFiles(PluginsFolder, "*.dll", SearchOption.AllDirectories))
			{
				Console.WriteLine($"Scanning {file}");
				var assembly = Assembly.LoadFrom(file);
				foreach (var type in assembly.GetTypes())
				{
					var attr = type.GetCustomAttribute<PluginAttribute>();
					if (attr == null)
						continue;
					if (type.IsAssignableFrom(typeof(Plugin)))
						throw new TypeLoadException($"{attr.Name} must inherit from Plugin");
					var plugin = (Plugin)FormatterServices.GetUninitializedObject(type);
					plugin.Writer = new PluginTextWriter(attr.Name);
					plugs.Add(plugin);
					attrs.Add(attr);
					Console.WriteLine("  Added " + attr.Name);
				}
			}
			for (int i = 0; i < plugs.Count; i++)
			{
				Console.WriteLine("Constructing " + attrs[i].Name);
				var constructor = plugs[i].GetType().GetConstructor(new Type[0]);
					/*
					list[i].GetType().GetConstructor(
					BindingFlags.Public | BindingFlags.NonPublic,
					null,
					new Type[0],
					null
				);*/
				constructor.Invoke(plugs[i], new object[0]);
			}
			Configuration.Config.ReadConfigFile(false);
			for (int i = 0; i < plugs.Count; i++)
			{
				Console.WriteLine("Initializing " + attrs[i].Name);
				plugs[i].Init();
			}
		}
	}
}
