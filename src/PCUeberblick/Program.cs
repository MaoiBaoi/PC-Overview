using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

internal static class Program
{
	[DllImport("user32.dll")]
	private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

	[DllImport("user32.dll")]
	private static extern bool SetProcessDPIAware();

	private static void EnableDpiAwareness()
	{
		try
		{
			if (!SetProcessDpiAwarenessContext(new IntPtr(-4)))
			{
				SetProcessDPIAware();
			}
		}
		catch
		{
			try
			{
				SetProcessDPIAware();
			}
			catch
			{
			}
		}
	}

	[STAThread]
	private static void Main()
	{
		EnableDpiAwareness();
		if (!IsAdmin())
		{
			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = Application.ExecutablePath,
					UseShellExecute = true,
					Verb = "runas"
				});
				return;
			}
			catch
			{
			}
		}
		LowerPriority();
		AppDomain.CurrentDomain.AssemblyResolve += Resolve;
		AppDomain.CurrentDomain.UnhandledException += OnFatal;
		AppDomain.CurrentDomain.ProcessExit += delegate
		{
			SafeRelease();
		};
		Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
		Application.ThreadException += OnThreadException;
		Start();
	}

	private static void LowerPriority()
	{
		try
		{
			using Process process = Process.GetCurrentProcess();
			process.PriorityClass = ProcessPriorityClass.BelowNormal;
		}
		catch
		{
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void SafeRelease()
	{
		try
		{
			Thread thread = new Thread((ThreadStart)delegate
			{
				try
				{
					MainForm.EmergencyRelease();
				}
				catch
				{
				}
			});
			thread.IsBackground = true;
			thread.Start();
			thread.Join(2500);
		}
		catch
		{
		}
	}

	private static void OnFatal(object sender, UnhandledExceptionEventArgs e)
	{
		try
		{
			Log.Write("ABSTURZ: " + e.ExceptionObject);
		}
		catch
		{
		}
		SafeRelease();
	}

	private static int threadErrors;

	private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
	{
		threadErrors++;
		if (threadErrors <= 20 || threadErrors % 100 == 0)
		{
			Log.Write("Fehler (" + threadErrors + "): " + e.Exception);
		}
	}

	public static bool IsAdmin()
	{
		try
		{
			using WindowsIdentity ntIdentity = WindowsIdentity.GetCurrent();
			return new WindowsPrincipal(ntIdentity).IsInRole(WindowsBuiltInRole.Administrator);
		}
		catch
		{
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void Start()
	{
		Application.EnableVisualStyles();
		Application.Run(new MainForm());
	}

	private static Assembly Resolve(object sender, ResolveEventArgs args)
	{
		string name = new AssemblyName(args.Name).Name + ".dll";
		using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
		if (stream == null)
		{
			return null;
		}
		using MemoryStream memoryStream = new MemoryStream();
		stream.CopyTo(memoryStream);
		return Assembly.Load(memoryStream.ToArray());
	}
}
