using System.Globalization;

internal static class Loc
{
	public static bool En;

	public static void Init(string lang)
	{
		if (lang != null)
		{
			lang = lang.Trim().ToLowerInvariant();
			if (lang == "en")
			{
				En = true;
				return;
			}
			if (lang == "de")
			{
				En = false;
				return;
			}
		}
		En = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "de";
	}

	public static string T(string de, string en)
	{
		return En ? en : de;
	}
}
