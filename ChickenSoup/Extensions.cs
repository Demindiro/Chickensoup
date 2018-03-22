using System.IO;
namespace ChickenSoup
{
	public static class Extensions
	{
		public static void Write(this Stream stream, byte[] data)
		{
			if (data != null)
				stream.Write(data, 0, data.Length);
		}
	}
}
