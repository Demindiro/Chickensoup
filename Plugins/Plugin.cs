﻿using System.IO;

namespace ChickenSoup.Plugins
{
	public abstract class Plugin
	{
		protected internal TextWriter Writer;

		protected internal abstract void Init();
	}
}
