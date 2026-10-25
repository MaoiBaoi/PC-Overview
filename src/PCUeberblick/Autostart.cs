using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

internal static class Autostart
{
	private const string TaskName = "PCUeberblick";

	private static int Run(string args, out string output)
	{
		using Process process = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "schtasks.exe"), args)
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		});
		string text = process.StandardOutput.ReadToEnd();
		string text2 = process.StandardError.ReadToEnd();
		process.WaitForExit(15000);
		output = (text2 + " " + text).Trim();
		return process.ExitCode;
	}

	public static bool IsEnabled()
	{
		try
		{
			string output;
			return Run("/Query /TN \"PCUeberblick\"", out output) == 0;
		}
		catch
		{
			return false;
		}
	}

	public static string Set(bool on)
	{
		string text = null;
		try
		{
			string output;
			if (!on)
			{
				if (!IsEnabled())
				{
					return null;
				}
				return (Run("/Delete /TN \"PCUeberblick\" /F", out output) == 0) ? null : output;
			}
			string text2 = SecurityElement.Escape(WindowsIdentity.GetCurrent().Name);
			string text3 = SecurityElement.Escape(Application.ExecutablePath);
			string text4 = SecurityElement.Escape(Path.GetDirectoryName(Application.ExecutablePath));
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-16\"?>");
			stringBuilder.AppendLine("<Task version=\"1.2\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">");
			stringBuilder.AppendLine("  <RegistrationInfo><Description>PC Überblick – Start mit Windows</Description></RegistrationInfo>");
			stringBuilder.AppendLine("  <Triggers><LogonTrigger><Enabled>true</Enabled><UserId>" + text2 + "</UserId><Delay>PT10S</Delay></LogonTrigger></Triggers>");
			stringBuilder.AppendLine("  <Principals><Principal id=\"Author\"><UserId>" + text2 + "</UserId><LogonType>InteractiveToken</LogonType><RunLevel>HighestAvailable</RunLevel></Principal></Principals>");
			stringBuilder.AppendLine("  <Settings>");
			stringBuilder.AppendLine("    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>");
			stringBuilder.AppendLine("    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>");
			stringBuilder.AppendLine("    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>");
			stringBuilder.AppendLine("    <AllowHardTerminate>true</AllowHardTerminate>");
			stringBuilder.AppendLine("    <StartWhenAvailable>false</StartWhenAvailable>");
			stringBuilder.AppendLine("    <AllowStartOnDemand>true</AllowStartOnDemand>");
			stringBuilder.AppendLine("    <Enabled>true</Enabled>");
			stringBuilder.AppendLine("    <Hidden>false</Hidden>");
			stringBuilder.AppendLine("    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>");
			stringBuilder.AppendLine("    <Priority>7</Priority>");
			stringBuilder.AppendLine("  </Settings>");
			stringBuilder.AppendLine("  <Actions Context=\"Author\"><Exec><Command>" + text3 + "</Command><WorkingDirectory>" + text4 + "</WorkingDirectory></Exec></Actions>");
			stringBuilder.AppendLine("</Task>");
			text = Path.Combine(Path.GetTempPath(), "PCUeberblick_task_" + Guid.NewGuid().ToString("N") + ".xml");
			File.WriteAllText(text, stringBuilder.ToString(), Encoding.Unicode);
			return (Run("/Create /TN \"PCUeberblick\" /XML \"" + text + "\" /F", out output) == 0) ? null : output;
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
		finally
		{
			try
			{
				if (text != null && File.Exists(text))
				{
					File.Delete(text);
				}
			}
			catch
			{
			}
		}
	}
}
