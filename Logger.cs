using System;
using System.IO;
using System.Text;

namespace ChickenSoup
{
	public static class Logger
	{
		private static string logPath =
			#if DEBUG
			"/tmp/";
			#else
			"/var/log/chickensoup/";
			#endif
		private static TextWriter writer;
		#if !DEBUG
		private static int logFileDay;
		#endif

		public static string LogPath
		{
			get => logPath;
			set => logPath = value[value.Length - 1] == '/' ? value : value + '/';
		}


		public static void Log(string msg, LogType type = LogType.Info)
		{
			lock(logPath)
			{
				if (writer == null
				    #if !DEBUG
				    || DateTime.Now.Day != logFileDay
				    #endif
				   )
					OpenLog();
				writer.Write($"[{type.Parse()}][{DateTime.Now.ToString("HH:mm:ss")}] {msg}\n");
			}
		}

		public static void Close()
		{
			writer.Close();
			writer = null;
		}


		private static void OpenLog()
		{
#if !DEBUG
			if (!Directory.Exists(logPath))
				Directory.CreateDirectory(logPath);
			writer?.Close();
			var stream = File.Open(logPath + DateTime.Now.ToString("yy-MM-dd") + ".log",
			                       FileMode.OpenOrCreate,
			                       FileAccess.Write,
			                       FileShare.Read);
			stream.Seek(0, SeekOrigin.End);
			writer = new StreamWriter(stream, Encoding.UTF8, 1024, false);
			//writer.AutoFlush = true; // Suddenly gone... odd
			logFileDay = DateTime.Now.Day;
#else
			writer = Console.Out;
			#endif
		}

		private static string Parse(this LogType type)
		{
			switch (type)
			{
			case LogType.Info:
				return "INFO";
			case LogType.Error:
				return "ERROR";
			case LogType.Warning:
				return "WARNING";
			case LogType.Debug:
				return "DEBUG";
			default:
				throw new ArgumentException("Invalid LogType", nameof(type));
			}
		}
	}

	public enum LogType {
		Info,
		Warning,
		Error,
		Debug,
	}
}
