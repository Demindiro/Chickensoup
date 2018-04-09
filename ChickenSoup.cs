using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using Configuration;
using Commands;

namespace ChickenSoup
{
	public static partial class ChickenSoup
	{
		// TODO figure out how to disable the warning for all fields with this attribute
		#pragma warning disable 0649
		[Config("BASE_FILE"   , LoadFileContents = true)] public static readonly string BaseFile;
		[Config("DEFAULT_FILE", LoadFileContents = true)] public static readonly string DefaultFile;
		[Config("ERROR_FILE"  , LoadFileContents = true)] public static readonly string ErrorSnippet;
		#pragma warning restore 0649
		[Config("ROOT_FOLDER")]   private static string rootFolder;

		public static string RootFolder => rootFolder;

		public static int Main(string[] args)
		{
			int ret = ParseArguments(args);
			if (ret <= 0)
				return -ret;

			Command.RegisterCommands();
			Command.AddCommand("config reload", Config.ReadConfigFile);

			try
			{
				Configuration.Config.ReadConfigFile();
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex.Message);
				return 1;
			}
			if (!rootFolder.EndsWith("/", StringComparison.InvariantCulture))
				rootFolder += "/";

			Articles.Init();
			Redirect.Init();

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

		private static bool HandleRequest(HttpListenerContext context)
		{
			ThreadPool.QueueUserWorkItem(delegate
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
			});
			return true;
		}


		internal static void GetFile(this HttpListenerContext client)
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
				path = RootFolder + ((path == "/") ? "index.html" : path + ".html");
				if (client.ErrorOnNonExists(path))
					return;
				client.WriteAndClose(File.ReadAllText(path), format, HttpStatusCode.OK);
			}
			else
			{
				format = path.Substring(dotIndex + 1);
				path = RootFolder + path;
				if (client.ErrorOnNonExists(path))
					return;
				client.WriteAndClose(File.ReadAllBytes(path), format, HttpStatusCode.OK);
			}
		}


		public static string WrapContent(this string content)
		{
			int i = BaseFile.IndexOf("<main>", StringComparison.InvariantCultureIgnoreCase);
			if (i < 0)
				throw new FormatException("<main> tag missing");
			return BaseFile.Insert(i + "<main>".Length, content);
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
