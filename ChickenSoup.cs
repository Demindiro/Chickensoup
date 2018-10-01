using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using Configuration;
using Commands;
using ChickenSoup.Templates;

namespace ChickenSoup
{
	public static partial class ChickenSoup
	{
		// TODO figure out how to disable the warning for all fields with this attribute
		#pragma warning disable 0649
		[Config("BASE_FILE"   , LoadFileContents = true)] private static string BaseFile { set => baseTemplate = new Template(value, Functions); }
		[Config("DEFAULT_FILE", LoadFileContents = true)] private static readonly string DefaultFile;
		[Config("ERROR_FILE"  , LoadFileContents = true)] public static readonly string ErrorSnippet;
		[Config("ROOT_FOLDER")] private static string rootFolder;
		#pragma warning restore 0649
		private static Template baseTemplate;

		public static string RootFolder => rootFolder;
		public static readonly Dictionary<string, TemplateFunction> Functions = new Dictionary<string, TemplateFunction>
		{
			{"main", (a, o) => (string)o},
		};

		public static int Main(string[] args)
		{
			int ret = ParseArguments(args);
			if (ret <= 0)
				return -ret;

			try
			{
				Plugins.PluginLoader.LoadPlugins();
			}
			catch (FormatException fex)
			{
				Exception ex = fex;
				var prefix = "";
				while(ex != null)
				{
					Console.Error.WriteLine(prefix + ex.Message);
					ex = ex.InnerException;
					prefix += "  ";
				}
				return 1;
			}

			Command.RegisterCommands();
			Command.AddCommand("config reload", ReloadConfig);

			Http.AddListener("", HandleRequest);

			while (true)
			{
				Console.Write(">> ");
				try
				{
					Command.Parse(Console.ReadLine());
				}
				catch(Exception ex)
				{
					Console.WriteLine("\x1b[31m" + ex.Message + "\x1b[0m");
				}
			}
		}

		// All lambdas and anonymous methods are now instance-bound so fuck me
		static void ReloadConfig() => Config.ReadConfigFile(allowUnassignedFields:false);


		private static int ParseArguments(string[] args)
		{
			int ret = 1;
			for (int i = 0; i < args.Length; i++)
			{
				var arg = args[i];
				if (arg == "-v" || arg == "--version")
				{
					Console.WriteLine($"ChickenSoup v{Assembly.GetExecutingAssembly().GetName().Version}");
					ret = 0;
				}
				else
				{
					Console.WriteLine($"Invalid argument: {arg}");
					return -1;
				}
			}
			return ret;
		}

		private static void HandleRequest(HttpListenerContext context, HttpListener server)
		{
				try
				{
					GetFile(context);
				}
				catch (Exception ex)
				{
					Logger.Log(ex);
					context.Error(HttpStatusCode.InternalServerError);
				}
		}

		public static string WrapContent(this string content) => baseTemplate.Process(content);

		public static void GetFile(this HttpListenerContext client)
		{
			if (client.Request.HttpMethod != "HEAD" && client.Request.HttpMethod != "GET")
			{
				client.Error(HttpStatusCode.BadRequest);
				return;
			}
			var path = client.Request.Url.AbsolutePath;
			if (path.Contains("../"))
			{
				client.Error(HttpStatusCode.BadRequest);
				return;
			}

			var dotIndex = path.LastIndexOf('.');
			var format = "html";
			if (dotIndex < 0)
			{
				path = RootFolder + "/" + ((path == "/") ? "index.html" : path + ".html");
				if (client.ErrorOnNonExists(path))
					return;
				client.Write(File.ReadAllText(path), format, HttpStatusCode.OK);
			}
			else
			{
				format = path.Substring(dotIndex + 1);
				path = RootFolder + path;
				if (client.ErrorOnNonExists(path))
					return;
				client.Write(File.ReadAllBytes(path), format, HttpStatusCode.OK);
			}
		}


		private static bool ErrorOnNonExists(this HttpListenerContext client, string path)
		{
			if (!File.Exists(path))
			{
				client.Error(HttpStatusCode.NotFound);
				return true;
			}
			return false;
		}
	}
}
