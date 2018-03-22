﻿using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using Configuration;

namespace ChickenSoup
{
	public static partial class ChickenSoup
	{
		// TODO figure out how to disable the warning for all fields with this attribute
		#pragma warning disable 0649
		[Config("PORT")]               private static ushort port;
		[Config("BASE_FILE")]          private static string baseFilePath;
		[Config("DEFAULT_FILE")]       private static string defaultFilePath;
		[Config("ERROR_FILE")]         private static string errorSnippetFilePath;
		#pragma warning restore 0649
		[Config("ROOT_FOLDER")]        private static string rootFolder;
		[Config("CACHE_MAX_AGE")]      public static readonly int CacheMaxAge;
		private static string errorSnippet;
		private static string baseFile;
		private static HttpListener server;

		public static string BaseFile => baseFile ?? (baseFile = File.ReadAllText(RootFolder + baseFilePath));
		public static string ErrorSnippet => errorSnippet ?? (errorSnippet = File.ReadAllText(RootFolder + errorSnippetFilePath));
		public static ushort Port => port;
		public static string RootFolder => rootFolder;

		public static int Main(string[] args)
		{
			int ret = ParseArguments(args);
			if (ret <= 0)
				return -ret;

			Configuration.Config.ReadConfigFile();
			if (!rootFolder.EndsWith("/", StringComparison.InvariantCulture))
				rootFolder += "/";

			Articles.Init();
			Redirect.Init();

			server = new HttpListener();
			server.Prefixes.Add($"http://*:{port}/");
			server.Start();
			server.BeginGetContext(HandleNewRequest, null);

			while (true)
			{
				Console.Write(">> ");
				string input = Console.ReadLine();
				try
				{
					ParseCommand(input);
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

		private static void HandleNewRequest(IAsyncResult ar)
		{
			var client = server.EndGetContext(ar);
			ThreadPool.QueueUserWorkItem(HandleRequest, client);
			server.BeginGetContext(HandleNewRequest, null);
		}

		private static void HandleRequest(object state)
		{
			var client = (HttpListenerContext)state;
			Logger.Log(client.Stringify(), "Base");
			try
			{
				GetFile(client);
			}
			catch(Exception ex)
			{
				Logger.Log(ex.Message, "Base", LogType.Error);
				client.Close(HttpStatusCode.InternalServerError);
			}
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
				path = RootFolder + path;
				if (client.ErrorOnNonExists(path))
					return;
				format = path.Substring(dotIndex + 1);
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