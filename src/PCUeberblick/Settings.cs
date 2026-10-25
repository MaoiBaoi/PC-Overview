using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

internal class Settings
{
	public bool Enabled = true;

	public bool HasBounds;

	public Rectangle Bounds;

	public bool Maximized;

	public bool Full;

	public bool TopMost;

	public int Design;

	public float? GpuFan;

	public float? CpuFan;

	public int PollMs = 1000;

	public bool UseGpu = true;

	public bool UseMobo = true;

	public int GameMatrixFps;

	public bool GameMatrixStop;

	public int[] TempLo = new int[3] { 40, 40, 40 };

	public int[] TempHi = new int[3] { 70, 70, 70 };

	public static string FilePath => Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PCUeberblick"), "settings.ini");

	public static Settings Load()
	{
		Settings settings = new Settings();
		Loc.Init(null);
		try
		{
			if (!File.Exists(FilePath))
			{
				return settings;
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			string[] array = File.ReadAllLines(FilePath);
			foreach (string text in array)
			{
				int num = text.IndexOf('=');
				if (num > 0)
				{
					dictionary[text.Substring(0, num).Trim()] = text.Substring(num + 1).Trim();
				}
			}
			string langValue;
			if (dictionary.TryGetValue("Lang", out langValue))
			{
				Loc.Init(langValue);
			}
			string[] tempKeys = new string[3] { "Gpu", "Cpu", "Hot" };
			for (int ti = 0; ti < 3; ti++)
			{
				int tlo = (int)Get(dictionary, tempKeys[ti] + "Lo", 40f);
				int thi = (int)Get(dictionary, tempKeys[ti] + "Hi", 70f);
				tlo = Math.Max(0, Math.Min(99, tlo));
				thi = Math.Max(tlo + 1, Math.Min(100, thi));
				settings.TempLo[ti] = tlo;
				settings.TempHi[ti] = thi;
			}
			settings.PollMs = Math.Max(500, Math.Min(10000, (int)Get(dictionary, "PollMs", 1000f)));
			settings.UseGpu = Get(dictionary, "Gpu", 1f) != 0f;
			settings.UseMobo = Get(dictionary, "Mobo", 1f) != 0f;
			settings.GameMatrixFps = Math.Max(0, Math.Min(25, (int)Get(dictionary, "GameMatrixFps", 0f)));
			settings.GameMatrixStop = Get(dictionary, "GameMatrixStop", 0f) != 0f;
			settings.Enabled = Get(dictionary, "Save", 1f) != 0f;
			if (!settings.Enabled)
			{
				return settings;
			}
			float num2 = Get(dictionary, "W", -1f);
			float num3 = Get(dictionary, "H", -1f);
			if (num2 >= 160f && num3 >= 120f)
			{
				settings.Bounds = new Rectangle((int)Get(dictionary, "X", 0f), (int)Get(dictionary, "Y", 0f), (int)num2, (int)num3);
				settings.HasBounds = true;
			}
			settings.Maximized = Get(dictionary, "Max", 0f) != 0f;
			settings.Full = Get(dictionary, "Full", 0f) != 0f;
			settings.TopMost = Get(dictionary, "Top", 0f) != 0f;
			settings.Design = (int)Get(dictionary, "Design", 0f);
			float num4 = Get(dictionary, "GpuFan", -1f);
			float num5 = Get(dictionary, "CpuFan", -1f);
			if (num4 >= 0f && num4 <= 100f)
			{
				settings.GpuFan = num4;
			}
			if (num5 >= 0f && num5 <= 100f)
			{
				settings.CpuFan = num5;
			}
		}
		catch
		{
		}
		return settings;
	}

	private static float Get(Dictionary<string, string> d, string key, float def)
	{
		if (d.TryGetValue(key, out var value) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		return def;
	}

	public bool IsOnScreen()
	{
		if (!HasBounds)
		{
			return false;
		}
		Rectangle rect = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, Math.Min(Bounds.Height, 60));
		return Screen.AllScreens.Any((Screen s) => s.WorkingArea.IntersectsWith(rect));
	}

	public void Save()
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
			List<string> list = new List<string>();
			list.Add("Save=" + (Enabled ? "1" : "0"));
			list.Add("PollMs=" + PollMs.ToString(CultureInfo.InvariantCulture));
			list.Add("Gpu=" + (UseGpu ? "1" : "0"));
			list.Add("Mobo=" + (UseMobo ? "1" : "0"));
			list.Add("GameMatrixFps=" + GameMatrixFps.ToString(CultureInfo.InvariantCulture));
			list.Add("GameMatrixStop=" + (GameMatrixStop ? "1" : "0"));
			list.Add("Lang=" + (Loc.En ? "en" : "de"));
			string[] tk = new string[3] { "Gpu", "Cpu", "Hot" };
			for (int ti = 0; ti < 3; ti++)
			{
				list.Add(tk[ti] + "Lo=" + TempLo[ti].ToString(CultureInfo.InvariantCulture));
				list.Add(tk[ti] + "Hi=" + TempHi[ti].ToString(CultureInfo.InvariantCulture));
			}
			if (Enabled)
			{
				if (HasBounds)
				{
					list.Add("X=" + Bounds.X.ToString(CultureInfo.InvariantCulture));
					list.Add("Y=" + Bounds.Y.ToString(CultureInfo.InvariantCulture));
					list.Add("W=" + Bounds.Width.ToString(CultureInfo.InvariantCulture));
					list.Add("H=" + Bounds.Height.ToString(CultureInfo.InvariantCulture));
				}
				list.Add("Max=" + (Maximized ? "1" : "0"));
				list.Add("Full=" + (Full ? "1" : "0"));
				list.Add("Top=" + (TopMost ? "1" : "0"));
				list.Add("Design=" + Design.ToString(CultureInfo.InvariantCulture));
				list.Add("GpuFan=" + (GpuFan.HasValue ? GpuFan.Value.ToString("0", CultureInfo.InvariantCulture) : "auto"));
				list.Add("CpuFan=" + (CpuFan.HasValue ? CpuFan.Value.ToString("0", CultureInfo.InvariantCulture) : "auto"));
			}
			File.WriteAllLines(FilePath, list);
		}
		catch
		{
		}
	}
}
