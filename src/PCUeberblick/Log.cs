using System;
using System.IO;
using System.Linq;
using System.Text;

internal static class Log
{
	private static readonly object gate = new object();

	private const long MaxBytes = 262144L;

	public static string FilePath => Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PCUeberblick"), "log.txt");

	public static void Write(string msg)
	{
		try
		{
			lock (gate)
			{
				string filePath = FilePath;
				Directory.CreateDirectory(Path.GetDirectoryName(filePath));
				FileInfo fileInfo = new FileInfo(filePath);
				if (fileInfo.Exists && fileInfo.Length > 262144)
				{
					string[] source = File.ReadAllLines(filePath);
					File.WriteAllLines(filePath, source.Skip(source.Length / 2).ToArray(), Encoding.UTF8);
				}
				File.AppendAllText(filePath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + msg + "\r\n", Encoding.UTF8);
			}
		}
		catch
		{
		}
	}
}
