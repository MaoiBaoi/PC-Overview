using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;
using Microsoft.Win32;

internal class MainForm : Form
{
	private static readonly Color BG = Color.FromArgb(16, 17, 20);

	private static readonly Color FG = Color.FromArgb(232, 232, 232);

	private static readonly Color DIM = Color.FromArgb(122, 125, 133);

	private static readonly Color GREEN = Color.FromArgb(74, 222, 128);

	private static readonly Color YELLOW = Color.FromArgb(251, 191, 36);

	private static readonly Color RED = Color.FromArgb(248, 113, 113);

	private static readonly Color BLUE = Color.FromArgb(96, 165, 250);

	private static readonly Color TRACK = Color.FromArgb(52, 55, 62);

	private static readonly Color C_RED = Color.FromArgb(255, 36, 0);

	private static readonly Color C_BLUE = Color.FromArgb(80, 215, 255);

	private const float SKEW = 0.2f;

	private static readonly Color MX_GREEN = Color.FromArgb(0, 235, 90);

	private static readonly Color MX_FILL = Color.FromArgb(0, 58, 26);

	private static readonly Color MX_FILL_DARK = Color.FromArgb(0, 28, 13);

	private const string AppVersion = "1.0";

	private const string AppAuthor = "MaoiBaoi";

	private RectangleF infoBtn;

	private bool hoverInfo;

	private PointF infoMouse;

	private Fan hoverKnob;

	private volatile bool pollBusy;

	private volatile bool pollRunning;

	private volatile bool closing;

	private string pollError;

	private Timer parTimer = new Timer();

	private float parX;

	private float parY;

	private float parTx;

	private float parTy;

	private RectangleF footArea;

	private const float INFO_FADE_STRENGTH = 0.5f;

	private const float AUTO_GAP = 0.69f;

	private const float TRACK_FADE_STRENGTH = 0.81f;

	private const float MX_KNOB_DARK = 0.675f;

	private const float TRACK_LEN_SCALE = 0.75f;

	private const float TRACK_GAP_LEFT = 54f;

	private const float TRACK_GAP_RIGHT = 66f;

	private Bitmap infoFade;

	private string infoFadeKey;

	private PointF knobMouse;

	private Computer computer;

	private Readings r = new Readings();

	private string status = "";

	private string alertMsg = "";

	private DateTime alertUntil = DateTime.MinValue;

	private const float BASE_EXTRA = 30f;

	private const float EXTRA_PCT = 0.14f;

	private bool pawnOk;

	private DateTime started = DateTime.Now;

	private static string cfgDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PCUeberblick");

	private string noAskPath = Path.Combine(cfgDir, "nopawnio.txt");

	private Timer timer = new Timer();

	private Timer animTimer = new Timer();

	private Timer saveTimer = new Timer();

	private Timer fanAnimTimer = new Timer();

	private DateTime fanAnimLast = DateTime.Now;

	private Settings cfg = Settings.Load();

	[DllImport("shell32.dll")]
	private static extern int SHQueryUserNotificationState(out int state);

	private static MainForm current;

	private static bool closedNormally;

	private bool gameMode;

	private FormWindowState lastWs;

	private DateTime lastSlowLog = DateTime.MinValue;

	private int readErrors;

	private string lastAlertLog = "";

	private DateTime lastAlertLogAt = DateTime.MinValue;

	private static readonly Dictionary<char, string> glyphCache = new Dictionary<char, string>();

	private Bitmap bgCache;

	private int bgLineKey;

	private int design;

	private RectangleF[] designBtn = new RectangleF[2];

	private Bitmap infoImg;

	private Timer matrixTimer = new Timer();

	private int mCols;

	private int mRows;

	private float mDiv;

	private const float MATRIX_BLUR = 0.25f;

	private const float MATRIX_BLUR_SCALE = 0.9f;

	private float mCw;

	private float mCh;

	private float[] mHead;

	private float[] mSpeed;

	private int[] mLen;

	private char[][] mChars;

	private Random mRnd = new Random();

	private DateTime mLast = DateTime.Now;

	private Bitmap mBmp;

	private Bitmap mSmall;

	private Graphics mG;

	private Graphics mGs;

	private Font mFont;

	private SolidBrush[] mBr;

	private SolidBrush mHeadBr = new SolidBrush(Color.FromArgb(215, 255, 225));

	private static readonly Color ICE = Color.FromArgb(110, 195, 255);

	private static readonly Color HOTC = Color.FromArgb(255, 60, 25);

	private Fan gpuFan = new Fan
	{
		Label = "GPU Lüfter"
	};

	private Fan cpuFan = new Fan
	{
		Label = "CPU Lüfter"
	};

	private Fan dragging;

	private Fan hoverFan;

	private Bitmap[] frames;

	private SoundPlayer snd;

	private SoundPlayer eggSnd;

	private Timer eggTimer = new Timer();

	private Random eggRnd = new Random();

	private bool videoPlaying;

	private DateTime videoStart;

	private const double FPS = 14.985;

	private Timer vidTimer = new Timer();

	private DateTime maxStart = DateTime.MinValue;

	private const double MAXDUR = 2200.0;

	public MainForm()
	{
		try
		{
			System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;
		}
		catch
		{
		}
		try
		{
			using (Stream iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico"))
			{
				Icon = new Icon(iconStream);
			}
		}
		catch
		{
		}
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Expected Obj, but got Unknown
		current = this;
		Text = "PC Überblick";
		ApplyLanguage();
		BackColor = BG;
		AutoScaleMode = AutoScaleMode.None;
		float num = ((DeviceDpi <= 0) ? 1f : ((float)DeviceDpi / 96f));
		ClientSize = new Size((int)(760f * num), (int)(520f * num));
		MinimumSize = new Size(160, 120);
		DoubleBuffered = true;
		ResizeRedraw = true;
		KeyPreview = true;
		if (cfg.Enabled)
		{
			if (cfg.IsOnScreen())
			{
				StartPosition = FormStartPosition.Manual;
				Bounds = cfg.Bounds;
			}
			TopMost = cfg.TopMost;
			design = ((cfg.Design == 1) ? 1 : 0);
			if (cfg.Maximized && !cfg.Full)
			{
				WindowState = FormWindowState.Maximized;
			}
		}
		pawnOk = PawnIoInstalled();
		try
		{
			computer = new Computer
			{
				IsCpuEnabled = true,
				IsGpuEnabled = cfg.UseGpu,
				IsMotherboardEnabled = cfg.UseMobo
			};
			Stopwatch openWatch = Stopwatch.StartNew();
			computer.Open();
			Log.Write("Start v" + AppVersion + ", Admin=" + Program.IsAdmin() + ", PawnIO=" + pawnOk + ", Windows " + Environment.OSVersion.Version + ", Sensoren geoeffnet in " + openWatch.ElapsedMilliseconds + " ms, Abfrage alle " + cfg.PollMs + " ms, GPU=" + cfg.UseGpu + ", Mainboard=" + cfg.UseMobo);
			foreach (IHardware hw in computer.Hardware)
			{
				Log.Write("Hardware: " + hw.HardwareType + " " + hw.Name);
			}
			DiscoverFans();
			RestoreFans();
			Log.Write("Luefter: GPU " + (gpuFan.Available ? "steuerbar" : "n/a") + (gpuFan.Pct.HasValue ? " fest " + gpuFan.Pct.Value.ToString("0") + " %" : " Auto") + ", CPU " + (cpuFan.Available ? "steuerbar" : "n/a") + (cpuFan.Pct.HasValue ? " fest " + cpuFan.Pct.Value.ToString("0") + " %" : " Auto"));
		}
		catch (Exception ex)
		{
			status = Loc.T("Sensoren konnten nicht gestartet werden: ", "Could not start sensors: ") + ex.Message;
		}
		gpuFan.Anim = ((gpuFan.Available && !gpuFan.Pct.HasValue) ? 1f : 0f);
		cpuFan.Anim = ((cpuFan.Available && !cpuFan.Pct.HasValue) ? 1f : 0f);
		fanAnimTimer.Interval = 15;
		fanAnimTimer.Tick += delegate
		{
			FanAnimTick();
		};
		timer.Interval = cfg.PollMs;
		timer.Tick += delegate
		{
			if (pollBusy)
			{
				return;
			}
			pollBusy = true;
			System.Threading.ThreadPool.QueueUserWorkItem(delegate
			{
				pollRunning = true;
				try
				{
					PollHardware();
				}
				catch
				{
				}
				finally
				{
					pollRunning = false;
				}
				try
				{
					if (closing || !IsHandleCreated)
					{
						pollBusy = false;
					}
					else
					{
						BeginInvoke(new Action(FinishPoll));
					}
				}
				catch
				{
					pollBusy = false;
				}
			});
		};
		timer.Start();
		matrixTimer.Interval = 40;
		matrixTimer.Tick += delegate
		{
			UpdateMatrix();
			Invalidate();
		};
		parTimer.Interval = 16;
		parTimer.Tick += delegate
		{
			parX += (parTx - parX) * 0.18f;
			parY += (parTy - parY) * 0.18f;
			if (Math.Abs(parTx - parX) < 0.003f && Math.Abs(parTy - parY) < 0.003f)
			{
				parX = parTx;
				parY = parTy;
				parTimer.Stop();
			}
			if (!(design == 1 && matrixTimer.Enabled))
			{
				Invalidate(Rectangle.Ceiling(footArea));
			}
		};
		ApplyMatrixRate();
		saveTimer.Interval = 1200;
		saveTimer.Tick += delegate
		{
			saveTimer.Stop();
			SaveNow();
		};
		animTimer.Interval = 30;
		animTimer.Tick += delegate
		{
			if ((DateTime.Now - maxStart).TotalMilliseconds > 2200.0)
			{
				animTimer.Stop();
			}
			Invalidate();
		};
		PollHardware();
		UpdateReadings();
		KeyDown += OnKey;
		DoubleClick += delegate
		{
			Point point = PointToClient(Cursor.Position);
			if (!gpuFan.Hit.Contains(point) && !cpuFan.Hit.Contains(point) && !gpuFan.Btn.Contains(point) && !cpuFan.Btn.Contains(point) && !infoBtn.Contains(point) && !designBtn[0].Contains(point) && !designBtn[1].Contains(point))
			{
				ToggleFull();
			}
		};
		MouseDown += OnMouseDown;
		MouseMove += OnMouseMove;
		MouseUp += delegate
		{
			if (dragging != null)
			{
				dragging = null;
				Invalidate();
			}
		};
		MouseLeave += delegate
		{
			parTx = 0f;
			parTy = 0f;
			if (!parTimer.Enabled)
			{
				parTimer.Start();
			}
			if (hoverFan != null || hoverInfo || hoverKnob != null)
			{
				hoverFan = null;
				hoverInfo = false;
				hoverKnob = null;
				Invalidate();
			}
		};
		vidTimer.Interval = 33;
		vidTimer.Tick += delegate
		{
			VideoTick();
		};
		FormClosing += delegate
		{
			closing = true;
			SaveNow();
			ReleaseAll();
			try
			{
				StopVideo();
				eggTimer.Stop();
				if (eggSnd != null)
				{
					eggSnd.Stop();
				}
				if (snd != null)
				{
					snd.Stop();
				}
			}
			catch
			{
			}
		};
		FormClosed += delegate
		{
			closedNormally = true;
			Log.Write("Beenden");
			saveTimer.Stop();
			fanAnimTimer.Stop();
			matrixTimer.Stop();
			parTimer.Stop();
			timer.Stop();
			for (int waitI = 0; waitI < 40 && pollRunning; waitI++)
			{
				System.Threading.Thread.Sleep(50);
			}
			if (bgCache != null)
			{
				bgCache.Dispose();
			}
			try
			{
				if (computer != null)
				{
					computer.Close();
				}
			}
			catch
			{
			}
		};
		SizeChanged += delegate
		{
			QueueSave();
			if (WindowState != lastWs)
			{
				lastWs = WindowState;
				ApplyMatrixRate();
			}
		};
		LocationChanged += delegate
		{
			QueueSave();
		};
		Shown += delegate
		{
			if (cfg.Enabled && cfg.Full)
			{
				ToggleFull();
			}
			try
			{
				LoadMedia();
			}
			catch
			{
			}
			StartEgg();
			AskPawnIo();
		};
	}

	private void ApplyMatrixRate()
	{
		bool slowMatrix = gameMode && cfg.GameMatrixStop;
		if (design != 1 || WindowState == FormWindowState.Minimized || (slowMatrix && cfg.GameMatrixFps <= 0))
		{
			matrixTimer.Stop();
			return;
		}
		int num = ((!slowMatrix) ? 40 : Math.Max(40, 1000 / cfg.GameMatrixFps));
		if (matrixTimer.Interval != num)
		{
			matrixTimer.Interval = num;
		}
		if (!matrixTimer.Enabled)
		{
			mLast = DateTime.Now;
			matrixTimer.Start();
		}
	}

	private void UpdateGameMode()
	{
		bool flag = false;
		try
		{
			if (Form.ActiveForm == null)
			{
				int state;
				if (SHQueryUserNotificationState(out state) == 0)
				{
					flag = state == 2 || state == 3 || state == 4;
				}
			}
		}
		catch
		{
		}
		if (flag != gameMode)
		{
			gameMode = flag;
			Log.Write(gameMode ? (cfg.GameMatrixStop ? "Vollbildanwendung erkannt: Matrix wird gedrosselt" : "Vollbildanwendung erkannt: Matrix laeuft weiter") : "Vollbildanwendung beendet: Matrix laeuft normal");
			ApplyMatrixRate();
		}
	}

	public static void EmergencyRelease()
	{
		MainForm mainForm = current;
		if (mainForm != null && !closedNormally)
		{
			mainForm.HardRelease(mainForm.gpuFan);
			mainForm.HardRelease(mainForm.cpuFan);
			Log.Write("Luefter im Notfall auf Auto gestellt");
		}
	}

	private void HardRelease(Fan f)
	{
		try
		{
			foreach (ISensor sensor in f.Sensors)
			{
				try
				{
					sensor.Control.SetDefault();
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	private void SetDesign(int d)
	{
		if (d != design)
		{
			design = d;
			ApplyMatrixRate();
			QueueSave();
			Invalidate();
		}
	}

	private char RandChar()
	{
		int num = mRnd.Next(100);
		if (num < 55)
		{
			return (char)mRnd.Next(65382, 65438);
		}
		if (num < 75)
		{
			return (char)(48 + mRnd.Next(10));
		}
		if (num < 87)
		{
			return (char)(65 + mRnd.Next(26));
		}
		if (num < 92)
		{
			return ":.=*+-<>|".ToCharArray()[mRnd.Next(9)];
		}
		if (num < 96)
		{
			return (char)(945 + mRnd.Next(24));
		}
		return (char)(1040 + mRnd.Next(32));
	}

	private void ResetCol(int c, bool initial)
	{
		mSpeed[c] = 7f + (float)mRnd.NextDouble() * 16f;
		mLen[c] = 8 + mRnd.Next(18);
		mHead[c] = (initial ? ((float)mRnd.NextDouble() * (float)(mRows + mLen[c]) - (float)mLen[c]) : (0f - (float)mRnd.NextDouble() * (float)mRows * 0.5f));
	}

	private void UpdateMatrix()
	{
		int num = ClientSize.Width;
		int num2 = ClientSize.Height;
		if (num < 8 || num2 < 8)
		{
			return;
		}
		float num3 = ((DeviceDpi <= 0) ? 1f : ((float)DeviceDpi / 96f));
		float num4 = Math.Max(8f, 14f * num3);
		float num5 = Math.Max(9f, 17f * num3);
		int num6 = (int)Math.Ceiling((float)num / num4);
		int num7 = (int)Math.Ceiling((float)num2 / num5) + 1;
		if (mHead == null || num6 != mCols || num7 != mRows)
		{
			mCols = num6;
			mRows = num7;
			mCw = num4;
			mCh = num5;
			mHead = new float[mCols];
			mSpeed = new float[mCols];
			mLen = new int[mCols];
			mChars = new char[mCols][];
			for (int i = 0; i < mCols; i++)
			{
				mChars[i] = new char[mRows];
				for (int j = 0; j < mRows; j++)
				{
					mChars[i][j] = RandChar();
				}
				ResetCol(i, initial: true);
			}
			if (mFont != null)
			{
				mFont.Dispose();
				mFont = null;
			}
		}
		DateTime now = DateTime.Now;
		float num8 = Math.Min(0.2f, (float)(now - mLast).TotalSeconds);
		mLast = now;
		for (int k = 0; k < mCols; k++)
		{
			mHead[k] += mSpeed[k] * num8;
			if (mHead[k] - (float)mLen[k] > (float)mRows)
			{
				ResetCol(k, initial: false);
			}
			if (mRnd.NextDouble() < 0.35)
			{
				mChars[k][mRnd.Next(mRows)] = RandChar();
			}
		}
	}

	private static string GlyphStr(char c)
	{
		string value;
		if (!glyphCache.TryGetValue(c, out value))
		{
			value = c.ToString();
			glyphCache[c] = value;
		}
		return value;
	}

	private float MatrixDiv()
	{
		return Math.Max(2f, (float)Math.Round(mCh * MATRIX_BLUR) * MATRIX_BLUR_SCALE);
	}

	private void DrawMatrix(Graphics g)
	{
		int num = ClientSize.Width;
		int num2 = ClientSize.Height;
		if (num < 8 || num2 < 8)
		{
			return;
		}
		if (mHead == null)
		{
			UpdateMatrix();
		}
		if (mHead == null)
		{
			return;
		}
		if (mBmp == null || mBmp.Width != num || mBmp.Height != num2 || mDiv != MatrixDiv())
		{
			if (mG != null)
			{
				mG.Dispose();
			}
			if (mGs != null)
			{
				mGs.Dispose();
			}
			if (mBmp != null)
			{
				mBmp.Dispose();
			}
			if (mSmall != null)
			{
				mSmall.Dispose();
			}
			mBmp = new Bitmap(num, num2, PixelFormat.Format32bppPArgb);
			mG = Graphics.FromImage(mBmp);
			mG.TextRenderingHint = TextRenderingHint.AntiAlias;
			mDiv = MatrixDiv();
			mSmall = new Bitmap(Math.Max(1, (int)((float)num / mDiv)), Math.Max(1, (int)((float)num2 / mDiv)), PixelFormat.Format32bppPArgb);
			mGs = Graphics.FromImage(mSmall);
			mGs.InterpolationMode = InterpolationMode.HighQualityBilinear;
		}
		if (mFont == null)
		{
			mFont = new Font("MS Gothic", mCh * 0.95f, FontStyle.Regular, GraphicsUnit.Pixel);
		}
		if (mBr == null)
		{
			mBr = new SolidBrush[12];
			for (int i = 0; i < mBr.Length; i++)
			{
				mBr[i] = new SolidBrush(Color.FromArgb(40 + i * 19, 0, 230, 70));
			}
		}
		mG.Clear(Color.Black);
		using (StringFormat format = (StringFormat)StringFormat.GenericTypographic.Clone())
		{
			for (int j = 0; j < mCols; j++)
			{
				int num3 = (int)Math.Floor(mHead[j]);
				float headFrac = mHead[j] - (float)num3;
				for (int k = 0; k < mLen[j]; k++)
				{
					int num4 = num3 - k;
					if (num4 >= 0 && num4 < mRows)
					{
						SolidBrush brush;
						if (k == 0)
						{
							brush = mHeadBr;
						}
						else
						{
							double num5 = Math.Pow(1.0 - (double)k / (double)mLen[j], 1.6);
							brush = mBr[Math.Max(0, Math.Min(11, (int)(num5 * 11.99)))];
						}
						mG.DrawString(GlyphStr(mChars[j][num4]), mFont, brush, (float)j * mCw + mCw * 0.12f, ((float)num4 + headFrac) * mCh, format);
					}
				}
			}
		}
		mGs.DrawImage(mBmp, new Rectangle(0, 0, mSmall.Width, mSmall.Height));
		InterpolationMode interpolationMode = g.InterpolationMode;
		g.InterpolationMode = InterpolationMode.HighQualityBicubic;
		using (ImageAttributes imageAttributes = new ImageAttributes())
		{
			ColorMatrix colorMatrix = new ColorMatrix(new float[5][]
			{
				new float[5] { 1.5f, 0f, 0f, 0f, 0f },
				new float[5] { 0f, 1.5f, 0f, 0f, 0f },
				new float[5] { 0f, 0f, 1.5f, 0f, 0f },
				new float[5] { 0f, 0f, 0f, 1f, 0f },
				new float[5] { 0f, 0f, 0f, 0f, 1f }
			});
			imageAttributes.SetColorMatrix(colorMatrix);
			imageAttributes.SetWrapMode(WrapMode.TileFlipXY);
			g.DrawImage(mSmall, new Rectangle(0, 0, num, num2), 0, 0, mSmall.Width, mSmall.Height, GraphicsUnit.Pixel, imageAttributes);
		}
		g.InterpolationMode = interpolationMode;
		using Brush brush2 = new SolidBrush(Color.FromArgb(80, 0, 0, 0));
		g.FillRectangle(brush2, 0, 0, num, num2);
	}

	private void DrawInfoIcon(Graphics g, RectangleF circle, Color color)
	{
		if (infoImg == null)
		{
			try
			{
				using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("info.png");
				using Image image = Image.FromStream(stream);
				Bitmap image2 = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppPArgb);
				using (Graphics graphics = Graphics.FromImage(image2))
				{
					graphics.CompositingMode = CompositingMode.SourceCopy;
					graphics.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height));
				}
				infoImg = image2;
			}
			catch
			{
				return;
			}
		}
		float num = circle.Height * 0.58f;
		float num2 = num * (float)infoImg.Width / (float)infoImg.Height;
		Rectangle destRect = new Rectangle((int)Math.Round(circle.Left + (circle.Width - num2) / 2f), (int)Math.Round(circle.Top + (circle.Height - num) / 2f), (int)Math.Round(num2), (int)Math.Round(num));
		using ImageAttributes imageAttributes = new ImageAttributes();
		ColorMatrix colorMatrix = new ColorMatrix(new float[5][]
		{
			new float[5]
			{
				(float)(int)color.R / 255f,
				0f,
				0f,
				0f,
				0f
			},
			new float[5]
			{
				0f,
				(float)(int)color.G / 255f,
				0f,
				0f,
				0f
			},
			new float[5]
			{
				0f,
				0f,
				(float)(int)color.B / 255f,
				0f,
				0f
			},
			new float[5] { 0f, 0f, 0f, 1f, 0f },
			new float[5] { 0f, 0f, 0f, 0f, 1f }
		});
		imageAttributes.SetColorMatrix(colorMatrix);
		InterpolationMode interpolationMode = g.InterpolationMode;
		g.InterpolationMode = InterpolationMode.HighQualityBicubic;
		g.DrawImage(infoImg, destRect, 0, 0, infoImg.Width, infoImg.Height, GraphicsUnit.Pixel, imageAttributes);
		g.InterpolationMode = interpolationMode;
	}

	private static float InfoRamp(float t)
	{
		float s = 1f - Math.Max(0f, Math.Min(1f, t));
		if (s <= 0.4f)
		{
			return 1f;
		}
		if (s <= 0.78f)
		{
			return 1f + (0.2157f - 1f) * ((s - 0.4f) / 0.38f);
		}
		return 0.2157f * (1f - (s - 0.78f) / 0.22f);
	}

	private void DrawInfoFaded(Graphics g, RectangleF btn, Color ring, float ringW, Color icon)
	{
		int pad = (int)Math.Ceiling(ringW) + 2;
		int ix = (int)Math.Floor(btn.Left) - pad;
		int iy = (int)Math.Floor(btn.Top) - pad;
		int w = (int)Math.Ceiling(btn.Width) + pad * 2 + 1;
		int h = (int)Math.Ceiling(btn.Height) + pad * 2 + 1;
		RectangleF local = new RectangleF(btn.Left - (float)ix, btn.Top - (float)iy, btn.Width, btn.Height);
		string key = w + "x" + h + "|" + ring.ToArgb() + "|" + icon.ToArgb() + "|" + ringW.ToString("0.00") + "|" + local.Left.ToString("0.00") + "|" + local.Top.ToString("0.00") + "|" + local.Width.ToString("0.00");
		if (infoFade == null || infoFadeKey != key)
		{
			if (infoFade != null)
			{
				infoFade.Dispose();
			}
			Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
			using (Graphics gb = Graphics.FromImage(bmp))
			{
				gb.SmoothingMode = SmoothingMode.AntiAlias;
				using (Pen ringPen = new Pen(ring, ringW))
				{
					gb.DrawEllipse(ringPen, local);
				}
				DrawInfoIcon(gb, local, icon);
			}
			BitmapData bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);
			try
			{
				byte[] px = new byte[bd.Stride * h];
				Marshal.Copy(bd.Scan0, px, 0, px.Length);
				for (int y = 0; y < h; y++)
				{
					float f = 1f - (1f - InfoRamp((((float)y + 0.5f) - local.Top) / Math.Max(1f, local.Height))) * INFO_FADE_STRENGTH;
					for (int x = 0; x < w; x++)
					{
						int p = y * bd.Stride + x * 4;
						px[p] = (byte)((float)(int)px[p] * f);
						px[p + 1] = (byte)((float)(int)px[p + 1] * f);
						px[p + 2] = (byte)((float)(int)px[p + 2] * f);
						px[p + 3] = (byte)((float)(int)px[p + 3] * f);
					}
				}
				Marshal.Copy(px, 0, bd.Scan0, px.Length);
			}
			finally
			{
				bmp.UnlockBits(bd);
			}
			infoFade = bmp;
			infoFadeKey = key;
		}
		g.DrawImage(infoFade, new Rectangle(ix, iy, w, h));
	}

	private void DrawInfoShine(Graphics g, RectangleF btn, PointF mouse)
	{
		float num = btn.Width / 2f;
		float num2 = btn.Left + num;
		float num3 = btn.Top + btn.Height / 2f;
		float num4 = mouse.X - num2;
		float num5 = mouse.Y - num3;
		float num6 = (float)Math.Sqrt(num4 * num4 + num5 * num5);
		if (num6 > num)
		{
			num4 *= num / num6;
			num5 *= num / num6;
		}
		float num7 = num2 + num4;
		float num8 = num3 + num5;
		float num9 = btn.Width * 0.95f;
		GraphicsState gstate = g.Save();
		try
		{
			using GraphicsPath graphicsPath = new GraphicsPath();
			graphicsPath.AddEllipse(num7 - num9, num8 - num9, num9 * 2f, num9 * 2f);
			using PathGradientBrush pathGradientBrush = new PathGradientBrush(graphicsPath);
			pathGradientBrush.CenterPoint = new PointF(num7, num8);
			pathGradientBrush.SurroundColors = new Color[1] { Color.FromArgb(0, 255, 255, 255) };
			ColorBlend colorBlend = new ColorBlend(4);
			colorBlend.Colors = new Color[4]
			{
				Color.FromArgb(0, 255, 255, 255),
				Color.FromArgb(14, 255, 255, 255),
				Color.FromArgb(60, 255, 255, 255),
				Color.FromArgb(133, 255, 255, 255)
			};
			colorBlend.Positions = new float[4] { 0f, 0.4f, 0.75f, 1f };
			pathGradientBrush.InterpolationColors = colorBlend;
			using (Pen pen = new Pen(pathGradientBrush, Math.Max(1.5f, btn.Width * 0.07f)))
			{
				g.DrawEllipse(pen, btn);
			}
			using GraphicsPath graphicsPath2 = new GraphicsPath();
			graphicsPath2.AddEllipse(btn);
			g.SetClip(graphicsPath2, CombineMode.Intersect);
			g.FillRectangle(pathGradientBrush, btn);
		}
		finally
		{
			g.Restore(gstate);
		}
	}

	private static float Ease(float a)
	{
		a = Math.Max(0f, Math.Min(1f, a));
		return a * a * (3f - 2f * a);
	}

	private void StartFanAnim()
	{
		fanAnimLast = DateTime.Now;
		if (!fanAnimTimer.Enabled)
		{
			fanAnimTimer.Start();
		}
	}

	private void FanAnimTick()
	{
		DateTime now = DateTime.Now;
		float num = (float)(now - fanAnimLast).TotalSeconds;
		fanAnimLast = now;
		bool flag = false;
		Fan[] array = new Fan[2] { gpuFan, cpuFan };
		foreach (Fan fan in array)
		{
			float num2 = ((fan.Available && !fan.Pct.HasValue) ? 1f : 0f);
			if (fan.Anim != num2)
			{
				float num3 = num / 0.3f;
				fan.Anim = ((num2 > fan.Anim) ? Math.Min(num2, fan.Anim + num3) : Math.Max(num2, fan.Anim - num3));
				flag = true;
			}
		}
		if (!flag)
		{
			fanAnimTimer.Stop();
		}
		Invalidate();
	}

	private void QueueSave()
	{
		saveTimer.Stop();
		saveTimer.Start();
	}

	private void SaveNow()
	{
		try
		{
			if (cfg.Enabled)
			{
				bool flag = FormBorderStyle == FormBorderStyle.None;
				cfg.Bounds = ((WindowState == FormWindowState.Normal) ? Bounds : RestoreBounds);
				cfg.HasBounds = cfg.Bounds.Width >= 160 && cfg.Bounds.Height >= 120;
				cfg.Full = flag;
				cfg.Maximized = WindowState == FormWindowState.Maximized && !flag;
				cfg.TopMost = TopMost;
				cfg.Design = design;
				cfg.GpuFan = gpuFan.Pct;
				cfg.CpuFan = cpuFan.Pct;
			}
			cfg.Save();
		}
		catch
		{
		}
	}

	private void RestoreFans()
	{
		if (cfg.Enabled)
		{
			if (gpuFan.Available && cfg.GpuFan.HasValue)
			{
				gpuFan.Pct = cfg.GpuFan;
				ApplyFan(gpuFan);
			}
			if (cpuFan.Available && cfg.CpuFan.HasValue)
			{
				cpuFan.Pct = cfg.CpuFan;
				ApplyFan(cpuFan);
			}
		}
	}

	private void StartEgg()
	{
		try
		{
			using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ghstlyecft.wav");
			if (stream == null)
			{
				return;
			}
			MemoryStream memoryStream = new MemoryStream();
			stream.CopyTo(memoryStream);
			memoryStream.Position = 0L;
			eggSnd = new SoundPlayer(memoryStream);
			eggSnd.Load();
			eggTimer.Tick += delegate
			{
				eggTimer.Stop();
				EggTick();
			};
			ScheduleEgg();
		}
		catch
		{
			eggSnd = null;
		}
	}

	private void ScheduleEgg()
	{
		eggTimer.Interval = eggRnd.Next(60 * 60 * 1000, 90 * 60 * 1000 + 1);
		eggTimer.Start();
	}

	private void EggTick()
	{
		try
		{
			if (!closing && eggSnd != null && !videoPlaying)
			{
				eggSnd.Play();
			}
		}
		catch
		{
		}
		ScheduleEgg();
	}

	private void LoadMedia()
	{
		if (frames != null)
		{
			return;
		}
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		List<Bitmap> list = new List<Bitmap>();
		int num = 0;
		while (true)
		{
			using (Stream stream = executingAssembly.GetManifestResourceStream("f" + num.ToString("00") + ".png"))
			{
				if (stream == null)
				{
					break;
				}
				using (Image image = Image.FromStream(stream))
				{
					Bitmap bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppPArgb);
					bitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
					using (Graphics graphics = Graphics.FromImage(bitmap))
					{
						graphics.CompositingMode = CompositingMode.SourceCopy;
						graphics.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
					}
					list.Add(bitmap);
				}
				goto IL_00e6;
			}
			IL_00e6:
			num++;
		}
		if (list.Count == 0)
		{
			throw new Exception(Loc.T("Video-Bilder sind nicht eingebettet", "Video frames are not embedded"));
		}
		frames = list.ToArray();
		try
		{
			using Stream stream2 = executingAssembly.GetManifestResourceStream("max.wav");
			if (stream2 != null)
			{
				MemoryStream memoryStream = new MemoryStream();
				stream2.CopyTo(memoryStream);
				memoryStream.Position = 0L;
				snd = new SoundPlayer(memoryStream);
				snd.Load();
			}
		}
		catch
		{
			snd = null;
		}
	}

	private int CurrentFrame()
	{
		return (int)((DateTime.Now - videoStart).TotalSeconds * 14.985);
	}

	private void PlayVideo()
	{
		try
		{
			if (videoPlaying)
			{
				return;
			}
			LoadMedia();
			videoPlaying = true;
			videoStart = DateTime.Now;
			try
			{
				if (snd != null)
				{
					snd.Stop();
					snd.Play();
				}
			}
			catch
			{
			}
			vidTimer.Start();
			Invalidate();
		}
		catch (Exception ex)
		{
			videoPlaying = false;
			Alert(Loc.T("Video nicht abspielbar: ", "Video cannot be played: ") + ex.Message);
		}
	}

	private void VideoTick()
	{
		if (!videoPlaying || frames == null)
		{
			vidTimer.Stop();
		}
		else if (CurrentFrame() >= frames.Length)
		{
			StopVideo();
		}
		else
		{
			Invalidate();
		}
	}

	private void StopVideo()
	{
		videoPlaying = false;
		vidTimer.Stop();
		Invalidate();
	}

	private void DrawVideo(Graphics g)
	{
		if (videoPlaying && frames != null)
		{
			int num = Math.Min(CurrentFrame(), frames.Length - 1);
			Bitmap bitmap = frames[num];
			float num2 = ClientSize.Width;
			float num3 = ClientSize.Height;
			float num4 = num3 * 0.5f;
			float num5 = num4 * (float)bitmap.Width / (float)bitmap.Height;
			if (num5 > num2 * 0.85f)
			{
				num5 = num2 * 0.85f;
				num4 = num5 * (float)bitmap.Height / (float)bitmap.Width;
			}
			InterpolationMode interpolationMode = g.InterpolationMode;
			PixelOffsetMode pixelOffsetMode = g.PixelOffsetMode;
			g.InterpolationMode = InterpolationMode.HighQualityBilinear;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;
			g.DrawImage(bitmap, new RectangleF((num2 - num5) / 2f, num3 - num4, num5, num4));
			g.InterpolationMode = interpolationMode;
			g.PixelOffsetMode = pixelOffsetMode;
		}
	}

	private static bool PawnIoInstalled()
	{
		RegistryView[] array = new RegistryView[2]
		{
			RegistryView.Registry64,
			RegistryView.Registry32
		};
		foreach (RegistryView view in array)
		{
			try
			{
				using RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
				using RegistryKey registryKey2 = registryKey.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\PawnIO");
				if (registryKey2 != null)
				{
					return true;
				}
			}
			catch
			{
			}
		}
		return false;
	}

	private void InstallPawnIo()
	{
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = "/c \"winget install --id namazso.PawnIO -e --accept-source-agreements --accept-package-agreements & echo. & echo " + Loc.T("Fertig. Bitte PC-Ueberblick jetzt neu starten.", "Done. Please restart PC Ueberblick now.") + " & pause\"",
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			Alert(Loc.T("winget nicht gefunden: ", "winget not found: ") + ex.Message);
		}
	}

	private void AskPawnIo()
	{
		if (pawnOk || File.Exists(noAskPath))
		{
			return;
		}
		if (MessageBox.Show(this, Loc.T("Für CPU-Temperatur, CPU-Watt und CPU-Lüfter fehlt der kostenlose Treiber \"PawnIO\" (wird auch von LibreHardwareMonitor genutzt).\n\nJetzt per winget installieren?\nDanach das Programm bitte einmal neu starten.", "The free driver \"PawnIO\" is missing for CPU temperature, CPU power and the CPU fan (it is also used by LibreHardwareMonitor).\n\nInstall it now with winget?\nAfterwards please restart the program once."), Loc.T("PC Überblick – Treiber fehlt", "PC Überblick: driver missing"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
		{
			InstallPawnIo();
			return;
		}
		try
		{
			Directory.CreateDirectory(cfgDir);
			File.WriteAllText(noAskPath, "1");
		}
		catch
		{
		}
	}

	private void DiscoverFans()
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Invalid comparison between Unknown and I4
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Invalid comparison between Unknown and I4
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		foreach (IHardware item in computer.Hardware)
		{
			List<ISensor> list = AllSensors(item).Where((ISensor s) =>
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_0008: Invalid comparison between Unknown and I4
				return (int)s.SensorType == 9 && s.Control != null;
			}).ToList();
			if (list.Count == 0)
			{
				continue;
			}
			if ((int)item.HardwareType == 4 || (int)item.HardwareType == 5)
			{
				if (!gpuFan.Available)
				{
					gpuFan.Sensors.AddRange(list);
					AddRpmSensors(gpuFan, item, list);
				}
			}
			else
			{
				if ((int)item.HardwareType != 0)
				{
					continue;
				}
				foreach (ISensor item2 in list)
				{
					if (!cpuFan.Sensors.Contains(item2))
					{
						cpuFan.Sensors.Add(item2);
					}
				}
				AddRpmSensors(cpuFan, item, list);
			}
		}
	}

	private static int TrailingNumber(string name)
	{
		Match match = Regex.Match(name ?? "", "(\\d+)\\s*$");
		if (!match.Success)
		{
			return -1;
		}
		return int.Parse(match.Groups[1].Value);
	}

	private static void AddRpmSensors(Fan f, IHardware hw, List<ISensor> controls)
	{
		List<ISensor> list = AllSensors(hw).Where((ISensor x) =>
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Invalid comparison between Unknown and I4
			return (int)x.SensorType == 7;
		}).ToList();
		List<ISensor> list2 = new List<ISensor>();
		foreach (ISensor control in controls)
		{
			ISensor val = list.FirstOrDefault((ISensor x) => string.Equals(x.Name, control.Name, StringComparison.OrdinalIgnoreCase));
			if (val == null)
			{
				int num = TrailingNumber(control.Name);
				if (num >= 0)
				{
					val = list.FirstOrDefault((ISensor x) => TrailingNumber(x.Name) == num);
				}
			}
			if (val != null && !list2.Contains(val))
			{
				list2.Add(val);
			}
		}
		if (list2.Count == 0)
		{
			list2 = list;
		}
		foreach (ISensor item in list)
		{
			List<ISensor> list3 = (list2.Contains(item) ? f.RpmSensors : f.RpmFallback);
			if (!f.RpmSensors.Contains(item) && !f.RpmFallback.Contains(item))
			{
				list3.Add(item);
			}
		}
	}

	private void ApplyFan(Fan f)
	{
		if (!f.Pct.HasValue)
		{
			return;
		}
		float num = (float)Math.Round(f.Pct.Value);
		if (num == f.Applied)
		{
			return;
		}
		f.Applied = num;
		foreach (ISensor sensor in f.Sensors)
		{
			try
			{
				float minSoftwareValue = sensor.Control.MinSoftwareValue;
				float maxSoftwareValue = sensor.Control.MaxSoftwareValue;
				sensor.Control.SetSoftware(Math.Max(minSoftwareValue, Math.Min(maxSoftwareValue, num)));
			}
			catch (Exception ex)
			{
				Alert(Loc.T("Lüfter nicht steuerbar: ", "Fan cannot be controlled: ") + ex.Message);
			}
		}
	}

	private void Release(Fan f)
	{
		f.Pct = null;
		f.Applied = -1f;
		QueueSave();
		StartFanAnim();
		foreach (ISensor sensor in f.Sensors)
		{
			try
			{
				sensor.Control.SetDefault();
			}
			catch
			{
			}
		}
	}

	private void ReleaseAll()
	{
		Release(gpuFan);
		Release(cpuFan);
	}

	private void Alert(string msg)
	{
		alertMsg = msg;
		alertUntil = DateTime.Now.AddSeconds(8.0);
		if (msg != lastAlertLog || (DateTime.Now - lastAlertLogAt).TotalSeconds > 5.0)
		{
			lastAlertLog = msg;
			lastAlertLogAt = DateTime.Now;
			Log.Write("Meldung: " + msg);
		}
	}

	private void Protect(Fan f, bool tooHot, string name)
	{
		if ((f.Pct.HasValue && f.Pct.Value < 100f) & tooHot)
		{
			Release(f);
			Alert(name + Loc.T(" zu heiß – Lüfter wieder auf Auto gestellt", " too hot, fan set back to Auto"));
		}
	}

	private void ProtectNoTemp(Fan f, bool tempOk, string name)
	{
		if (tempOk || !f.Pct.HasValue || f.Pct.Value >= 100f)
		{
			f.NoTemp = 0;
			return;
		}
		f.NoTemp++;
		if (f.NoTemp >= 5)
		{
			f.NoTemp = 0;
			Release(f);
			Alert(name + Loc.T(" Temperatur nicht lesbar – Lüfter wieder auf Auto gestellt", " temperature not readable, fan set back to Auto"));
		}
	}

	private void SetFromMouse(Fan f, int x)
	{
		float val = ((float)x - f.Track.Left) / Math.Max(1f, f.Track.Width);
		val = Math.Max(0f, Math.Min(1f, val)) * 100f;
		float? pct = f.Pct;
		float num = ((!pct.HasValue) ? (-1f) : pct.Value);
		f.Pct = (float)Math.Round(val);
		StartFanAnim();
		ApplyFan(f);
		QueueSave();
		if (f.Pct.Value >= 100f && num < 100f)
		{
			maxStart = DateTime.Now;
			animTimer.Start();
			PlayVideo();
		}
		Invalidate();
	}

	private void OnMouseDown(object s, MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left && infoBtn.Contains(e.Location))
		{
			ShowInfo();
			return;
		}
		if (e.Button == MouseButtons.Left)
		{
			for (int i = 0; i < 2; i++)
			{
				if (designBtn[i].Contains(e.Location))
				{
					SetDesign(i);
					return;
				}
			}
		}
		Fan[] array = new Fan[2] { gpuFan, cpuFan };
		foreach (Fan fan in array)
		{
			if (fan.Btn.Contains(e.Location))
			{
				if (e.Button == MouseButtons.Left)
				{
					if (fan.Available)
					{
						Release(fan);
					}
					Invalidate();
				}
				break;
			}
			if (fan.Hit.Contains(e.Location))
			{
				if (!fan.Available)
				{
					break;
				}
				if (e.Button == MouseButtons.Right)
				{
					Release(fan);
					Invalidate();
					break;
				}
				if (e.Button == MouseButtons.Left)
				{
					dragging = fan;
					SetFromMouse(fan, e.X);
					break;
				}
			}
		}
	}

	private void OnMouseMove(object s, MouseEventArgs e)
	{
		Fan fan = null;
		if (gpuFan.Available && gpuFan.Hit.Contains(e.Location))
		{
			fan = gpuFan;
		}
		else if (cpuFan.Available && cpuFan.Hit.Contains(e.Location))
		{
			fan = cpuFan;
		}
		bool flag = infoBtn.Contains(e.Location);
		if (flag)
		{
			infoMouse = e.Location;
		}
		if (flag != hoverInfo)
		{
			hoverInfo = flag;
			Invalidate();
		}
		else if (flag)
		{
			Invalidate(Rectangle.Ceiling(RectangleF.Inflate(infoBtn, 3f, 3f)));
		}
		if (fan != hoverFan)
		{
			hoverFan = fan;
			Invalidate();
		}
		if (ClientSize.Width > 0 && ClientSize.Height > 0)
		{
			parTx = Math.Max(-1f, Math.Min(1f, ((float)e.X / (float)ClientSize.Width) * 2f - 1f));
			parTy = Math.Max(-1f, Math.Min(1f, ((float)e.Y / (float)ClientSize.Height) * 2f - 1f));
			if (!parTimer.Enabled)
			{
				parTimer.Start();
			}
		}
		Fan knobFan = null;
		if (gpuFan.Available && gpuFan.Knob.Contains(e.Location))
		{
			knobFan = gpuFan;
		}
		else if (cpuFan.Available && cpuFan.Knob.Contains(e.Location))
		{
			knobFan = cpuFan;
		}
		if (knobFan != null)
		{
			knobMouse = e.Location;
		}
		if (knobFan != hoverKnob)
		{
			hoverKnob = knobFan;
			Invalidate();
		}
		else if (knobFan != null)
		{
			Invalidate(Rectangle.Ceiling(RectangleF.Inflate(knobFan.Knob, 4f, 4f)));
		}
		if (dragging != null && e.Button == MouseButtons.Left)
		{
			knobMouse = e.Location;
			SetFromMouse(dragging, e.X);
			return;
		}
		bool flag2 = ((gpuFan.Available && (gpuFan.Hit.Contains(e.Location) || gpuFan.Btn.Contains(e.Location))) || (cpuFan.Available && (cpuFan.Hit.Contains(e.Location) || cpuFan.Btn.Contains(e.Location)))) | flag | designBtn[0].Contains(e.Location) | designBtn[1].Contains(e.Location);
		Cursor = ((!flag2) ? Cursors.Default : Cursors.Hand);
	}

	private void ApplyLanguage()
	{
		gpuFan.Label = Loc.T("GPU Lüfter", "GPU Fan");
		cpuFan.Label = Loc.T("CPU Lüfter", "CPU Fan");
	}

	private void ShowInfo()
	{
		hoverInfo = false;
		dragging = null;
		Cursor = Cursors.WaitCursor;
		bool flag = Autostart.IsEnabled();
		Cursor = Cursors.Default;
		using (InfoForm infoForm = new InfoForm("1.0", "MaoiBaoi", DeviceDpi, cfg.Enabled, flag, cfg.GameMatrixStop, cfg.TempLo, cfg.TempHi))
		{
			infoForm.ShowDialog(this);
			cfg.TempLo = infoForm.TempLo;
			cfg.TempHi = infoForm.TempHi;
			ApplyLanguage();
			cfg.Enabled = infoForm.SaveSettings;
			cfg.GameMatrixStop = infoForm.StopMatrixInGames;
			ApplyMatrixRate();
			SaveNow();
			if (infoForm.AutoStart != flag)
			{
				Cursor = Cursors.WaitCursor;
				string text = Autostart.Set(infoForm.AutoStart);
				Cursor = Cursors.Default;
				if (text != null)
				{
					Alert(Loc.T("Autostart nicht geändert: ", "Autostart not changed: ") + text);
				}
			}
		}
		Invalidate();
	}

	private void ToggleFull()
	{
		if (FormBorderStyle == FormBorderStyle.None)
		{
			FormBorderStyle = FormBorderStyle.Sizable;
			WindowState = FormWindowState.Normal;
		}
		else
		{
			FormBorderStyle = FormBorderStyle.None;
			WindowState = FormWindowState.Normal;
			WindowState = FormWindowState.Maximized;
		}
		QueueSave();
	}

	private void OnKey(object s, KeyEventArgs e)
	{
		if (e.KeyCode == Keys.F11)
		{
			ToggleFull();
		}
		else if (e.KeyCode == Keys.Escape && FormBorderStyle == FormBorderStyle.None)
		{
			ToggleFull();
		}
		else if (e.KeyCode == Keys.T)
		{
			TopMost = !TopMost;
		}
		else if (e.KeyCode == Keys.D)
		{
			Dump();
		}
		else if (e.KeyCode == Keys.I)
		{
			InstallPawnIo();
		}
		QueueSave();
		Invalidate();
	}

	private void Dump()
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("PC Überblick – Sensorliste " + DateTime.Now.ToString());
			stringBuilder.AppendLine("PawnIO installiert: " + PawnIoInstalled());
			stringBuilder.AppendLine("Administrator: " + Program.IsAdmin());
			stringBuilder.AppendLine("Abfrage alle " + cfg.PollMs + " ms, GPU: " + cfg.UseGpu + ", Mainboard: " + cfg.UseMobo + ", Vollbildspiel erkannt: " + gameMode);
			if (computer != null)
			{
				foreach (IHardware item in computer.Hardware)
				{
					UpdateAll(item);
				}
				foreach (IHardware item2 in computer.Hardware)
				{
					DumpHw(item2, stringBuilder, 0);
				}
			}
			File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "PCUeberblick_Sensoren.txt"), stringBuilder.ToString(), Encoding.UTF8);
			Alert(Loc.T("Sensorliste auf dem Desktop gespeichert: PCUeberblick_Sensoren.txt", "Sensor list saved to the desktop: PCUeberblick_Sensoren.txt"));
		}
		catch (Exception ex)
		{
			Alert(Loc.T("Dump fehlgeschlagen: ", "Dump failed: ") + ex.Message);
		}
	}

	private void DumpHw(IHardware h, StringBuilder sb, int depth)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		string text = new string(' ', depth * 2);
		sb.AppendLine(string.Concat(text, "[", h.HardwareType, "] ", h.Name));
		ISensor[] sensors = h.Sensors;
		foreach (ISensor val in sensors)
		{
			sb.AppendLine(string.Concat(text, "   ", val.SensorType, " | ", val.Name, " = ", (!val.Value.HasValue) ? "null" : val.Value.Value.ToString("0.##"), (val.Control == null) ? "" : string.Concat(" | steuerbar [", val.Control.ControlMode, ", min ", val.Control.MinSoftwareValue, ", max ", val.Control.MaxSoftwareValue, "]")));
		}
		IHardware[] subHardware = h.SubHardware;
		foreach (IHardware h2 in subHardware)
		{
			DumpHw(h2, sb, depth + 1);
		}
	}

	private static IEnumerable<ISensor> AllSensors(IHardware h)
	{
		ISensor[] sensors = h.Sensors;
		for (int i = 0; i < sensors.Length; i++)
		{
			yield return sensors[i];
		}
		IHardware[] subHardware = h.SubHardware;
		IHardware[] array = subHardware;
		foreach (IHardware h2 in array)
		{
			foreach (ISensor item in AllSensors(h2))
			{
				yield return item;
			}
		}
	}

	private static void UpdateAll(IHardware h)
	{
		h.Update();
		IHardware[] subHardware = h.SubHardware;
		for (int i = 0; i < subHardware.Length; i++)
		{
			UpdateAll(subHardware[i]);
		}
	}

	private static bool Ok(ISensor x)
	{
		if (x.Value.HasValue && !float.IsNaN(x.Value.Value))
		{
			return x.Value.Value > 0f;
		}
		return false;
	}

	private static float? Pick(List<ISensor> list, SensorType type, string[] names, bool fallbackMax)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		foreach (string n in names)
		{
			ISensor val = list.FirstOrDefault((ISensor x) =>
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				return x.SensorType == type && Ok(x) && string.Equals(x.Name, n, StringComparison.OrdinalIgnoreCase);
			});
			if (val != null)
			{
				return val.Value;
			}
		}
		if (fallbackMax)
		{
			List<float> list2 = (from x in list.Where((ISensor x) =>
				{
					//IL_0001: Unknown result type (might be due to invalid IL or missing references)
					//IL_0007: Unknown result type (might be due to invalid IL or missing references)
					return x.SensorType == type && Ok(x) && x.Name.IndexOf("Distance", StringComparison.OrdinalIgnoreCase) < 0 && x.Name.IndexOf("TjMax", StringComparison.OrdinalIgnoreCase) < 0;
				})
				select x.Value.Value).ToList();
			if (list2.Count > 0)
			{
				return list2.Max();
			}
		}
		return null;
	}

	private void FinishPoll()
	{
		pollBusy = false;
		if (closing)
		{
			return;
		}
		UpdateReadings();
		UpdateGameMode();
		Invalidate();
	}

	private void PollHardware()
	{
		if (computer == null)
		{
			return;
		}
		try
		{
			Stopwatch pollWatch = Stopwatch.StartNew();
			long worstMs = 0L;
			string worstName = "";
			foreach (IHardware item in computer.Hardware)
			{
				if ((int)item.HardwareType == 0 && !cpuFan.Pct.HasValue)
				{
					continue;
				}
				long startMs = pollWatch.ElapsedMilliseconds;
				UpdateAll(item);
				long usedMs = pollWatch.ElapsedMilliseconds - startMs;
				if (usedMs > worstMs)
				{
					worstMs = usedMs;
					worstName = item.Name;
				}
			}
			if (worstMs >= 250 && (DateTime.Now - lastSlowLog).TotalSeconds > 30.0)
			{
				lastSlowLog = DateTime.Now;
				Log.Write("Langsame Abfrage: " + worstName + " " + worstMs + " ms");
			}
		}
		catch (Exception ex)
		{
			pollError = ex.Message;
			readErrors++;
			if (readErrors <= 10 || readErrors % 100 == 0)
			{
				Log.Write("Fehler beim Auslesen (" + readErrors + "): " + ex);
			}
		}
	}

	private void UpdateReadings()
	{
		if (computer == null)
		{
			return;
		}
		if (pollError != null)
		{
			status = Loc.T("Fehler beim Auslesen: ", "Error reading sensors: ") + pollError;
			pollError = null;
			return;
		}
		try
		{
			IHardware val = computer.Hardware.FirstOrDefault((IHardware h) =>
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_0007: Invalid comparison between Unknown and I4
				return (int)h.HardwareType == 2;
			});
			List<ISensor> list = ((val == null) ? new List<ISensor>() : AllSensors(val).ToList());
			List<ISensor> list2 = new List<ISensor>();
			float num = -1f;
			foreach (IHardware item2 in computer.Hardware.Where((IHardware x) =>
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_0007: Invalid comparison between Unknown and I4
				//IL_000a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0010: Invalid comparison between Unknown and I4
				//IL_0013: Unknown result type (might be due to invalid IL or missing references)
				//IL_0019: Invalid comparison between Unknown and I4
				return (int)x.HardwareType == 4 || (int)x.HardwareType == 5 || (int)x.HardwareType == 6;
			}))
			{
				List<ISensor> list3 = AllSensors(item2).ToList();
				float num2 = (from x in list3.Where((ISensor x) =>
					{
						//IL_0001: Unknown result type (might be due to invalid IL or missing references)
						//IL_0007: Invalid comparison between Unknown and I4
						return (int)x.SensorType == 2 && Ok(x);
					})
					select x.Value.Value).DefaultIfEmpty(0f).Max();
				if (num2 > num)
				{
					num = num2;
					list2 = list3;
				}
			}
			r.GpuTemp = Pick(list2, (SensorType)4, new string[1] { "GPU Core" }, fallbackMax: true);
			r.GpuHot = Pick(list2, (SensorType)4, new string[2] { "GPU Hot Spot", "GPU Hotspot" }, fallbackMax: false);
			r.GpuW = Pick(list2, (SensorType)2, new string[3] { "GPU Package", "GPU Power", "GPU Core" }, fallbackMax: true);
			r.CpuTemp = Pick(list, (SensorType)4, new string[5] { "CPU Package", "Core (Tctl/Tdie)", "Core (Tdie)", "CPU (Tctl/Tdie)", "Tctl/Tdie" }, fallbackMax: true);
			r.CpuW = Pick(list, (SensorType)2, new string[2] { "CPU Package", "Package" }, fallbackMax: true);
			Fan[] array = new Fan[2] { gpuFan, cpuFan };
			foreach (Fan fan in array)
			{
				List<float> list4 = (from x in fan.Sensors
					where x.Value.HasValue
					select x.Value.Value).ToList();
				fan.AutoNow = ((list4.Count <= 0) ? float.NaN : list4[0]);
				List<float> list5 = (from x in fan.RpmSensors
					where x.Value.HasValue && !float.IsNaN(x.Value.Value)
					select x.Value.Value).ToList();
				if (list5.Count == 0)
				{
					list5 = (from x in fan.RpmFallback
						where x.Value.HasValue && !float.IsNaN(x.Value.Value)
						select x.Value.Value).ToList();
				}
				fan.RpmNow = ((list5.Count <= 0) ? float.NaN : list5.Max());
				if (fan.Pct.HasValue && float.IsNaN(fan.RpmNow))
				{
					fan.NoRpm++;
					if (fan.NoRpm == 3)
					{
						string rpmInfo = fan.Label + ": keine Drehzahl trotz festem Wert. Steuer-Sensoren: " + fan.Sensors.Count + ", RPM-Sensoren: " + fan.RpmSensors.Count + " (Ersatz: " + fan.RpmFallback.Count + ")";
						foreach (ISensor rs in fan.RpmSensors.Concat(fan.RpmFallback))
						{
							rpmInfo = rpmInfo + " | " + rs.Name + "=" + (rs.Value.HasValue ? rs.Value.Value.ToString("0") : "null");
						}
						Log.Write(rpmInfo);
					}
				}
				else
				{
					fan.NoRpm = 0;
				}
			}
			Protect(cpuFan, r.CpuTemp.HasValue && r.CpuTemp.Value >= 85f, "CPU");
			Protect(gpuFan, (r.GpuTemp.HasValue && r.GpuTemp.Value >= 80f) || (r.GpuHot.HasValue && r.GpuHot.Value >= 90f), "GPU");
			ProtectNoTemp(cpuFan, r.CpuTemp.HasValue, "CPU");
			ProtectNoTemp(gpuFan, r.GpuTemp.HasValue || r.GpuHot.HasValue, "GPU");
			if (!Program.IsAdmin())
			{
				status = Loc.T("Nicht als Administrator gestartet – CPU-Werte und Lüftersteuerung gehen nur mit Admin-Rechten (Rechtsklick auf die EXE > Als Administrator ausführen)", "Not started as administrator: CPU values and fan control need admin rights (right click the EXE, then Run as administrator)");
			}
			else
			{
				float? cpuTemp = r.CpuTemp;
				if (cpuTemp.HasValue)
				{
					goto IL_04cf;
				}
				float? cpuW = r.CpuW;
				if (cpuW.HasValue)
				{
					goto IL_04cf;
				}
				status = (pawnOk ? Loc.T("CPU liefert keine Werte – Taste D drücken und die Datei auf dem Desktop schicken", "CPU returns no values: press D and send the file from the desktop") : Loc.T("CPU-Werte fehlen: PawnIO-Treiber nicht installiert (Taste I = installieren)", "CPU values missing: PawnIO driver not installed (press I to install)"));
			}
			goto end_IL_000a;
			IL_04cf:
			status = "";
			end_IL_000a:;
		}
		catch (Exception ex)
		{
			status = Loc.T("Fehler beim Auslesen: ", "Error reading sensors: ") + ex.Message;
			readErrors++;
			if (readErrors <= 10 || readErrors % 100 == 0)
			{
				Log.Write("Fehler beim Auslesen (" + readErrors + "): " + ex);
			}
		}
		if (status != "" && (DateTime.Now - started).TotalSeconds < 12.0)
		{
			status = "";
		}
	}

	private static Color TempTint(float t)
	{
		return Lerp(ICE, HOTC, (t - 40f) / 30f);
	}

	private static Color TempTint(float t, float lo, float hi)
	{
		return Lerp(ICE, HOTC, (t - lo) / Math.Max(1f, hi - lo));
	}

	private static Brush TempBrush(float? t, float top, float h, float lo, float hi)
	{
		if (!t.HasValue)
		{
			return new SolidBrush(DIM);
		}
		Color color = TempTint(t.Value, lo, hi);
		LinearGradientBrush linearGradientBrush = new LinearGradientBrush(new PointF(0f, top), new PointF(0f, top + h), Color.White, color);
		ColorBlend colorBlend = new ColorBlend(4);
		colorBlend.Colors = new Color[4]
		{
			Color.White,
			Color.White,
			color,
			color
		};
		colorBlend.Positions = new float[4] { 0f, 0.42f, 0.76f, 1f };
		linearGradientBrush.InterpolationColors = colorBlend;
		return linearGradientBrush;
	}

	private const float FOOT_FADE_STRENGTH = 0.65f;

	private const float PARALLAX_SCALE = 0.75f;

	private static Brush FadeBrush(Color c0, Color c1, float top, float h, float aMul, bool hold = false)
	{
		float[] pos = new float[4] { 0f, 0.4f, 0.78f, 1f };
		float[] alpha = new float[4] { 255f, 255f, 55f, 0f };
		Color[] cols = new Color[4];
		for (int i = 0; i < 4; i++)
		{
			Color m = Lerp(c0, c1, hold ? ((pos[i] - 0.42f) / 0.34f) : pos[i]);
			cols[i] = Color.FromArgb((int)((255f - (255f - alpha[i]) * FOOT_FADE_STRENGTH) * aMul), m.R, m.G, m.B);
		}
		LinearGradientBrush linearGradientBrush = new LinearGradientBrush(new PointF(0f, top), new PointF(0f, top + Math.Max(1f, h)), cols[0], cols[3]);
		ColorBlend colorBlend = new ColorBlend(4);
		colorBlend.Colors = cols;
		colorBlend.Positions = pos;
		linearGradientBrush.InterpolationColors = colorBlend;
		return linearGradientBrush;
	}

	private static Brush WattBrush(float top, float h)
	{
		return WattBrush(top, h, dim: false);
	}

	private static Brush WattBrush(float top, float h, bool dim)
	{
		return WattBrush(top, h, dim, flip: false);
	}

	private const float GESAMT_FADE_STRENGTH = 0.6f;

	private static Brush WattBrush(float top, float h, bool dim, bool flip)
	{
		return WattBrush(top, h, dim, flip, 1f);
	}

	private static Brush WattBrush(float top, float h, bool dim, bool flip, float strength)
	{
		Color color = (dim ? Color.FromArgb(255, 170, 174, 182) : Color.FromArgb(255, 255, 255, 255));
		Color color2 = (dim ? Color.FromArgb(255, 150, 154, 162) : Color.FromArgb(255, 226, 230, 238));
		int a3 = (int)(255f - 200f * strength);
		int a4 = (int)(255f - 255f * strength);
		Color color3 = (dim ? Color.FromArgb(a3, 128, 132, 140) : Color.FromArgb(a3, 176, 182, 196));
		Color color4 = (dim ? Color.FromArgb(a4, 128, 132, 140) : Color.FromArgb(a4, 176, 182, 196));
		LinearGradientBrush linearGradientBrush = new LinearGradientBrush(new PointF(0f, top), new PointF(0f, top + h), color, color4);
		ColorBlend colorBlend = new ColorBlend(4);
		if (flip)
		{
			colorBlend.Colors = new Color[4] { color4, color3, color2, color };
			colorBlend.Positions = new float[4] { 0f, 0.22f, 0.6f, 1f };
		}
		else
		{
			colorBlend.Colors = new Color[4] { color, color2, color3, color4 };
			colorBlend.Positions = new float[4] { 0f, 0.4f, 0.78f, 1f };
		}
		linearGradientBrush.InterpolationColors = colorBlend;
		return linearGradientBrush;
	}

	private static Color TempColor(float? t)
	{
		if (!t.HasValue)
		{
			return DIM;
		}
		if (t.HasValue && t.GetValueOrDefault() < 70f)
		{
			return GREEN;
		}
		if (t.HasValue && t.GetValueOrDefault() < 85f)
		{
			return YELLOW;
		}
		return RED;
	}

	private const float MX_OUTLINE_DARK = 0.6f;

	private static Color MxDark(Color c)
	{
		return MxDark(c, MX_OUTLINE_DARK);
	}

	private static Color MxDark(Color c, float k)
	{
		return Color.FromArgb(c.A, (int)((float)(int)c.R * k), (int)((float)(int)c.G * k), (int)((float)(int)c.B * k));
	}

	private static Color Lerp(Color a, Color b, float t)
	{
		t = Math.Max(0f, Math.Min(1f, t));
		return Color.FromArgb((int)((float)(int)a.R + (float)(b.R - a.R) * t), (int)((float)(int)a.G + (float)(b.G - a.G) * t), (int)((float)(int)a.B + (float)(b.B - a.B) * t));
	}

	private static string T(float? v)
	{
		if (v.HasValue)
		{
			return $"{v.Value:0}°";
		}
		return "--";
	}

	private static string W(float? v)
	{
		if (v.HasValue)
		{
			return $"{v.Value:0} Watt";
		}
		return "--";
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.Clear(BG);
		graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		string text = T(r.GpuTemp);
		string text2 = ((!r.GpuHot.HasValue) ? "" : $"(Hotspot {r.GpuHot.Value:0}°)");
		float? num = null;
		if (r.GpuW.HasValue || r.CpuW.HasValue)
		{
			float? gpuW = r.GpuW;
			float num2 = ((!gpuW.HasValue) ? 0f : gpuW.Value);
			float? cpuW = r.CpuW;
			num = num2 + ((!cpuW.HasValue) ? 0f : cpuW.Value);
		}
		float num3 = ((!num.HasValue) ? 0f : (30f + 0.14f * num.Value));
		float? v = ((!num.HasValue) ? ((float?)null) : (num + num3));
		string text3 = ((!num.HasValue) ? "" : $"(+{num3:0} W)");
		string[] array = new string[6] { "GPU:", "CPU:", "GPU:", "CPU:", "Total:", Loc.T("Gesamtverbrauch\ngeschätzt:", "Estimated\ntotal power:") };
		string[] array2 = new string[6]
		{
			text,
			T(r.CpuTemp),
			W(r.GpuW),
			W(r.CpuW),
			W(num),
			W(v)
		};
		Color[] array3 = new Color[6]
		{
			TempColor(r.GpuHot ?? r.GpuTemp),
			TempColor(r.CpuTemp),
			FG,
			FG,
			FG,
			FG
		};
		string text4 = "";
		bool flag = false;
		if (DateTime.Now < alertUntil)
		{
			text4 = alertMsg;
			flag = true;
		}
		else if (status != "")
		{
			text4 = status;
			flag = true;
		}
		float num4 = 0f;
		Font font = null;
		if (text4 != "")
		{
			font = new Font("Segoe UI", 9f);
			num4 = font.GetHeight(graphics) * 2f + 8f;
		}
		using (Font font2 = new Font("Consolas", 100f, FontStyle.Bold, GraphicsUnit.Pixel))
		{
			using StringFormat stringFormat = (StringFormat)StringFormat.GenericTypographic.Clone();
			stringFormat.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoWrap;
			float num5 = graphics.MeasureString("Total:", font2, 10000, stringFormat).Width;
			float num6 = graphics.MeasureString("00", font2, 10000, stringFormat).Width;
			float num7 = graphics.MeasureString("99° (Hotspot 99°)", font2, 10000, stringFormat).Width;
			string[] array4 = array2;
			foreach (string text5 in array4)
			{
				num7 = Math.Max(num7, graphics.MeasureString(text5, font2, 10000, stringFormat).Width);
			}
			float num8 = font2.GetHeight(graphics);
			num7 += num8 * 0.12f;
			float num9 = num8 * 0.35f;
			float num10 = num8 * 0.62f;
			float num11 = graphics.MeasureString("100%", font2, 10000, stringFormat).Width * 0.5f;
			float num12 = graphics.MeasureString("0000 RPM", font2, 10000, stringFormat).Width * 0.4f;
			float num13 = graphics.MeasureString("Auto", font2, 10000, stringFormat).Width * 0.5f + num10 * 0.5f;
			float num14 = num10 * 0.34f;
			float num15 = num5 + num6 + num7;
			float num16 = num8 * 6f + num9 * 2f + num10 * 2f + num8 * 0.22f;
			float num17 = 16f;
			float num18 = Math.Max(10f, (float)ClientSize.Width - 2f * num17);
			float num19 = Math.Max(10f, (float)ClientSize.Height - 2f * num17 - num4);
			float num20 = Math.Min(num18 / num15, num19 / num16);
			if (num20 < 0.06f)
			{
				num20 = 0.06f;
			}
			float num21 = ((float)ClientSize.Width - num15 * num20) / 2f;
			float num22 = num17 + (num19 - num16 * num20) / 2f;
			float num23 = num21 + (num5 + num6) * num20;
			using Font font3 = new Font("Consolas", 100f * num20, FontStyle.Bold, GraphicsUnit.Pixel);
			using Font font4 = new Font("Consolas", 100f * num20 * 0.5f, FontStyle.Bold, GraphicsUnit.Pixel);
			using Font font5 = new Font("Consolas", 100f * num20 * 0.4f, FontStyle.Bold, GraphicsUnit.Pixel);
			float num24 = num22 + (num8 * 2f + num9 * 0.5f) * num20;
			float lineY = num22 + (num8 * 6f + num9 * 1.5f) * num20;
			if (design == 1)
			{
				DrawMatrix(graphics);
			}
			else
			{
				DrawBackground(graphics, lineY);
			}
			DrawFadeLine(graphics, num21, num21 + num15 * num20, num24, Math.Max(1f, 3f * num20));
			DrawFadeLine(graphics, num21, num21 + num15 * num20, lineY, Math.Max(1f, 3f * num20));
			for (int j = 0; j < 6; j++)
			{
				float num25 = num22 + (float)j * num8 * num20 + ((j < 2) ? 0f : (num9 * num20));
				using (Brush brush = WattBrush(num25, font3.GetHeight(graphics), dim: true, flip: false))
				{
					if (j == 5)
					{
						string[] array5 = array[j].Split('\n');
						float num26 = font5.GetHeight(graphics);
						float num27 = num25 + (font3.GetHeight(graphics) - num26 * (float)array5.Length) / 2f;
						for (int k = 0; k < array5.Length; k++)
						{
							using Brush lineBrush = WattBrush(num27 + (float)k * num26, num26, dim: true, flip: false, GESAMT_FADE_STRENGTH);
							DrawSkew(graphics, array5[k], font5, lineBrush, num21, num27 + (float)k * num26, stringFormat, 0.2f);
						}
					}
					else
					{
						graphics.DrawString(array[j], font3, brush, num21, num25, stringFormat);
					}
				}
				using (Brush brush2 = ((j < 2) ? TempBrush((j == 0) ? r.GpuTemp : r.CpuTemp, num25, font3.GetHeight(graphics), cfg.TempLo[j], cfg.TempHi[j]) : WattBrush(num25, font3.GetHeight(graphics))))
				{
					graphics.DrawString(array2[j], font3, brush2, num23, num25, stringFormat);
				}
				if (j == 0 && text2 != "")
				{
					float num28 = graphics.MeasureString(array2[0], font3, 10000, stringFormat).Width;
					float num29 = graphics.MeasureString("00", font3, 10000, stringFormat).Width / 2f;
					using Brush brush3 = TempBrush(r.GpuHot, num25, font3.GetHeight(graphics), cfg.TempLo[2], cfg.TempHi[2]);
					DrawSkew(graphics, text2, font3, brush3, num23 + num28 + num29, num25, stringFormat, 0.2f);
				}
				if (j == 5)
				{
					float num30 = graphics.MeasureString(array2[j], font3, 10000, stringFormat).Width;
					float plusY = num25 + (font3.GetHeight(graphics) - font4.GetHeight(graphics)) * 0.7f;
					using Brush brush4 = WattBrush(plusY, font4.GetHeight(graphics), dim: true, flip: false, GESAMT_FADE_STRENGTH);
					DrawSkewX(graphics, text3, font4, brush4, num23 + num30 + num6 * 0.4f * num20, plusY, stringFormat, 0.2f, PLUS_XSCALE);
				}
			}
			float num31 = num22 + 2f * num8 * num20 + num9 * num20;
			float num32 = num8 * num20;
			float num33 = Math.Max(8f, num32 * 0.62f);
			float num34 = num21 + num15 * num20;
			infoBtn = new RectangleF(num34 - num33, num31 + num32 / 2f - num33 / 2f, num33, num33);
			using (Brush brush5 = new SolidBrush((design == 1) ? (hoverInfo ? Lerp(MX_FILL, MX_GREEN, 0.25f) : MX_FILL) : (hoverInfo ? Color.FromArgb(61, 65, 76) : Color.FromArgb(40, 43, 52))))
			{
				graphics.FillEllipse(brush5, infoBtn);
			}
			DrawInfoFaded(graphics, infoBtn, (design == 1) ? MxDark(hoverInfo ? Lerp(MX_GREEN, FG, 0.35f) : MX_GREEN) : (hoverInfo ? Lerp(DIM, FG, 0.7f) : DIM), Math.Max(1.5f, num33 * 0.07f), (design == 1) ? (hoverInfo ? Lerp(MX_GREEN, FG, 0.45f) : Lerp(MX_GREEN, FG, 0.2f)) : (hoverInfo ? Lerp(Color.FromArgb(200, 202, 208), FG, 0.7f) : Color.FromArgb(200, 202, 208)));
			if (design == 1)
			{
				using Brush infoDark = new SolidBrush(Color.FromArgb(51, 0, 0, 0));
				graphics.FillEllipse(infoDark, RectangleF.Inflate(infoBtn, num33 * 0.04f + 1f, num33 * 0.04f + 1f));
			}
			if (hoverInfo)
			{
				DrawInfoShine(graphics, infoBtn, infoMouse);
			}
			using (Font font6 = new Font("Consolas", 100f * num20 * 0.3f, FontStyle.Bold, GraphicsUnit.Pixel))
			{
				string[] array6 = new string[2] { "Standard", "Matrix" };
				Color[] array7 = new Color[2]
				{
					Color.White,
					Color.FromArgb(0, 235, 90)
				};
				float num35 = font6.GetHeight(graphics);
				float num36 = num22 + (num8 * 6f + num9 * 1.5f) * num20 - num35 - num9 * num20 * 0.2f;
				float num37 = num21 + num15 * num20 - num14 * num20 * 0.4f - 2f;
				float num38 = num35 * 0.2f;
				for (int num39 = 1; num39 >= 0; num39--)
				{
					float num40 = graphics.MeasureString(array6[num39], font6, 10000, stringFormat).Width;
					float num41 = num37 - num40;
					designBtn[num39] = new RectangleF(num41 - num38, num36 - num38 * 0.5f, num40 + num38 * 2f, num35 + num38);
					bool flag2 = design == num39;
					float shOff = Math.Max(1f, font6.Size * 0.06f);
					using (Brush shBrush1 = new SolidBrush(Color.FromArgb(flag2 ? 70 : 45, 0, 0, 0)))
					{
						DrawSkew(graphics, array6[num39], font6, shBrush1, num41 + shOff * 1.6f, num36 + shOff * 1.6f, stringFormat, 0.2f);
					}
					using (Brush shBrush2 = new SolidBrush(Color.FromArgb(flag2 ? 120 : 80, 0, 0, 0)))
					{
						DrawSkew(graphics, array6[num39], font6, shBrush2, num41 + shOff, num36 + shOff, stringFormat, 0.2f);
					}
					using (Brush brush6 = new SolidBrush(Color.FromArgb(flag2 ? 255 : 130, array7[num39])))
					{
						DrawSkew(graphics, array6[num39], font6, brush6, num41, num36, stringFormat, 0.2f);
					}
					if (flag2)
					{
						using Pen pen2 = new Pen(array7[num39], Math.Max(1f, num20 * 2.5f));
						graphics.DrawLine(pen2, num41, num36 + num35 * 0.95f, num41 + num40, num36 + num35 * 0.95f);
					}
					num37 = num41 - num6 * num20 * 0.35f;
				}
			}
			Fan[] array8 = new Fan[2] { gpuFan, cpuFan };
			for (int l = 0; l < 2; l++)
			{
				Fan fan = array8[l];
				float num42 = num22 + (num8 * 6f + num9 * 2f + num10 * (float)l) * num20;
				float num43 = num10 * num20;
				float num44 = num42 + num43 / 2f;
				float num45 = font4.GetHeight(graphics);
				float num46 = num44 - num45 / 2f;
				float num47 = num43 * 0.82f;
				fan.Btn = new RectangleF(num23 + (num7 - num13) * num20, num44 - num47 / 2f, num13 * num20, num47);
				float num48 = num23 + (num7 - num13 - num6 * 0.25f - num11) * num20;
				float num49 = num23 + num14 * num20;
				float num50 = num48 - num6 * 0.25f * num20 - num12 * num20;
				float num51 = num50 - num6 * 0.25f * num20 - num14 * num20;
				float num52 = Math.Max(3.4f * 1.2f * 1.15f * 1.05f * 1.05f, num8 * num20 * 0.17f * 0.85f * 0.85f * 1.2f * 1.15f * 1.05f * 1.05f);
				float num53 = Math.Max(10f, num51 - num49);
				float val = num21 + graphics.MeasureString("CPU Lüfter:", font4, 10000, stringFormat).Width + num14 * num20 * 1.5f;
				float num54 = Math.Max(Math.Min(num49, val), num49 - num53 * 0.2f);
				float labelEnd = num21 + graphics.MeasureString("CPU Lüfter:", font4, 10000, stringFormat).Width;
				float trackLeft0 = labelEnd + TRACK_GAP_LEFT * num20;
				float trackLen0 = Math.Max(10f, fan.Btn.Left - TRACK_GAP_RIGHT * num20 - trackLeft0);
				float trackLen = trackLen0 * TRACK_LEN_SCALE;
				float trackLeft = trackLeft0 + (trackLen0 - trackLen) / 2f;
				float ease = Ease(fan.Anim);
				fan.Track = new RectangleF(trackLeft, num44 - num52 / 2f, trackLen, num52);
				fan.Btn = new RectangleF(fan.Track.Right + AUTO_GAP * fan.Btn.Width + Math.Max(2f, num47 * 0.07f) / 2f, fan.Btn.Top, fan.Btn.Width, fan.Btn.Height);
				fan.Hit = RectangleF.Inflate(fan.Track, num14 * num20 * 1.5f, num43 * 0.5f);
				string text6 = "";
				float num55 = 50f;
				bool flag3 = false;
				if (!fan.Available)
				{
					text6 = "n/a";
				}
				else if (fan.Pct.HasValue)
				{
					flag3 = true;
					num55 = fan.Pct.Value;
					text6 = $"{num55:0}%";
				}
				bool flag4 = hoverFan == fan;
				using (Brush brush7 = new SolidBrush(DIM))
				{
					float lblW = graphics.MeasureString(fan.Label + ":", font4, 10000, stringFormat).Width;
					float lblX = Math.Max(num21, fan.Track.Left - AUTO_GAP * fan.Btn.Width - Math.Max(2f, num47 * 0.07f) / 2f - lblW);
					graphics.DrawString(fan.Label + ":", font4, brush7, lblX, num46, stringFormat);
				}
				bool flag5 = !flag3;
				float num56 = fan.Track.Left + fan.Track.Width * num55 / 100f;
				if (flag5)
				{
					DrawBlurredTrack(graphics, fan, num55);
				}
				else
				{
					DrawFadedTrack(graphics, fan, num56);
				}
				float inFont = num52 * 1.05f;
				if (inFont >= 6.5f)
				{
					using Font inF = new Font("Consolas", inFont, FontStyle.Bold, GraphicsUnit.Pixel);
					using StringFormat inSf = new StringFormat
					{
						Alignment = StringAlignment.Center,
						LineAlignment = StringAlignment.Center
					};
					if (!fan.Available)
					{
						DrawTrackText(graphics, text6, inF, fan.Track.Left + fan.Track.Width * 0.5f, num44, inSf, DIM, 255);
					}
					else
					{
						if (flag3)
						{
							fan.LastPctText = text6;
							fan.LastRpmText = (float.IsNaN(fan.RpmNow) ? "N/A RPM" : $"{fan.RpmNow:0} RPM");
						}
						int inAlpha = Math.Max(0, Math.Min(255, (int)(255f * (1f - ease))));
						DrawTrackText(graphics, fan.LastRpmText, inF, fan.Track.Left + fan.Track.Width * 0.25f, num44, inSf, Color.White, inAlpha);
						DrawTrackText(graphics, fan.LastPctText, inF, fan.Track.Left + fan.Track.Width * 0.75f, num44, inSf, Color.White, inAlpha);
					}
				}
				if (fan.Available)
				{
					float num57 = num14 * num20 * 0.75f * 0.8f;
					fan.Knob = new RectangleF(num56 - num57, num44 - num57, num57 * 2f, num57 * 2f);
					if (flag3)
					{
						DrawKnobShadow(graphics, num56, num44, num57);
						using (Brush brush12 = new SolidBrush((design == 1) ? MX_FILL : GradColor(num55 / 100f)))
						{
							graphics.FillEllipse(brush12, num56 - num57, num44 - num57, num57 * 2f, num57 * 2f);
						}
						using (Pen pen3 = new Pen((design == 1) ? MxDark(MX_GREEN, MX_KNOB_DARK) : Color.White, Math.Max(1.5f, num57 * 0.12f)))
						{
							graphics.DrawEllipse(pen3, num56 - num57, num44 - num57, num57 * 2f, num57 * 2f);
						}
						if (hoverKnob == fan || dragging == fan)
						{
							DrawInfoShine(graphics, fan.Knob, knobMouse);
						}
					}
					else if (flag4)
					{
						DrawKnobShadow(graphics, num56, num44, num57);
						using (Brush brush14 = new SolidBrush((design == 1) ? MX_FILL : Color.FromArgb(202, 202, 202)))
						{
							graphics.FillEllipse(brush14, num56 - num57, num44 - num57, num57 * 2f, num57 * 2f);
						}
						using (Pen pen3b = new Pen((design == 1) ? MxDark(MX_GREEN, MX_KNOB_DARK) : Color.FromArgb(34, 34, 38), Math.Max(1.5f, num57 * 0.12f)))
						{
							graphics.DrawEllipse(pen3b, num56 - num57, num44 - num57, num57 * 2f, num57 * 2f);
						}
						if (hoverKnob == fan || dragging == fan)
						{
							DrawInfoShine(graphics, fan.Knob, knobMouse);
						}
					}
					else
					{
						DrawLock(graphics, num56, num44, num57);
					}
				}
				bool flag6 = fan.Available && fan.Pct.HasValue;
				Color color3 = ((design == 1) ? (flag6 ? MX_FILL : MX_FILL_DARK) : (flag6 ? Color.FromArgb(30, 47, 62) : Color.FromArgb(21, 24, 32)));
				using (GraphicsPath path = RoundRect(fan.Btn, num47 * 0.3f))
				{
					using (Brush brush17 = new SolidBrush(color3))
					{
						graphics.FillPath(brush17, path);
					}
					if (design == 1)
					{
						using Pen pen4m = new Pen(MxDark(flag6 ? MX_GREEN : Color.FromArgb(150, 0, 235, 90)), Math.Max(2f, num47 * 0.07f));
						graphics.DrawPath(pen4m, path);
					}
					else if (flag6)
					{
						using Pen pen4 = new Pen(DIM, Math.Max(2f, num47 * 0.07f));
						graphics.DrawPath(pen4, path);
					}
				}
				using StringFormat format = new StringFormat
				{
					Alignment = StringAlignment.Center,
					LineAlignment = StringAlignment.Center
				};
				using Brush brush18 = WattBrush(num44 - font4.GetHeight(graphics) / 2f, font4.GetHeight(graphics), !flag6);
				graphics.DrawString("Auto", font4, brush18, fan.Btn, format);
				if (design == 1 && !flag6)
				{
					using GraphicsPath darkPath = RoundRect(fan.Btn, num47 * 0.3f);
					using Brush darkBrush = new SolidBrush(Color.FromArgb(51, 0, 0, 0));
					graphics.FillPath(darkBrush, darkPath);
					using Pen darkPen = new Pen(Color.FromArgb(51, 0, 0, 0), Math.Max(2f, num47 * 0.07f));
					graphics.DrawPath(darkPen, darkPath);
				}
			}
			using (Font footFont = new Font("Consolas", Math.Max(9f, num8 * num20 * 0.15f), FontStyle.Bold, GraphicsUnit.Pixel))
			{
				string footText = "PC Überblick " + Loc.T("von ", "by ") + AppAuthor;
				float footTop = num22 + (num8 * 6f + num9 * 2f + num10 * 2f) * num20 + num8 * num20 * 0.03f;
				float footH = footFont.GetHeight(graphics);
				float parMax = footFont.Size * 0.3f * PARALLAX_SCALE;
				float parVert = footFont.Size * 0.1f * PARALLAX_SCALE;
				footTop = Math.Max(footTop, (float)ClientSize.Height - num4 - footH - 1f - parVert);
				float footW = graphics.MeasureString(footText, footFont, 10000, stringFormat).Width;
				float footCx = gpuFan.Track.Left + gpuFan.Track.Width / 2f;
				float footX = footCx - footW / 2f;
				footArea = new RectangleF(footX - parMax - 8f, footTop - parVert - 4f, footW + parMax * 2f + 16f, footH + parVert * 2f + 8f);
				float fx = footX - parX * parMax;
				float fy = footTop - parY * parVert;
				using (Brush footShadow = FadeBrush(Color.Black, Color.Black, fy, footH, 120f / 255f))
				{
					DrawSkew(graphics, footText, footFont, footShadow, footX - parX * parMax * 0.3f + Math.Max(1f, footFont.Size * 0.07f), footTop - parY * parVert * 0.3f + Math.Max(1f, footFont.Size * 0.09f), stringFormat, 0.2f);
				}
				using Brush footBrush = ((design == 1) ? FadeBrush(Color.FromArgb(170, 255, 205), MX_GREEN, fy, footH, 1f) : FadeBrush(Color.White, TempTint(40f), fy, footH, 1f, hold: true));
				DrawSkew(graphics, footText, footFont, footBrush, fx, fy, stringFormat, 0.2f);
			}
		}
		if (font != null)
		{
			using (Brush brush19 = new SolidBrush((!flag) ? DIM : RED))
			{
				using StringFormat format2 = new StringFormat
				{
					Alignment = StringAlignment.Center,
					LineAlignment = StringAlignment.Center
				};
				graphics.DrawString(text4, font, brush19, new RectangleF(8f, (float)ClientSize.Height - num4, ClientSize.Width - 16, num4), format2);
			}
			font.Dispose();
		}
		DrawMax(graphics);
		DrawVideo(graphics);
	}

	private void DrawBackground(Graphics g, float lineY)
	{
		int num = ClientSize.Width;
		int num2 = ClientSize.Height;
		if (num <= 2 || num2 <= 2)
		{
			return;
		}
		int num3 = (int)Math.Round(lineY);
		if (bgCache == null || bgCache.Width != num || bgCache.Height != num2 || bgLineKey != num3)
		{
			bgLineKey = num3;
			if (bgCache != null)
			{
				bgCache.Dispose();
			}
			bgCache = new Bitmap(num, num2, PixelFormat.Format32bppPArgb);
			using Graphics graphics = Graphics.FromImage(bgCache);
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using (LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, 0, num, num2), Color.FromArgb(21, 24, 32), Color.FromArgb(11, 12, 16), LinearGradientMode.Vertical))
			{
				graphics.FillRectangle(brush, 0, 0, num, num2);
			}
			float num4 = (float)num * 0.47f;
			float num5 = (float)num2 * 0.62f;
			float num6 = (float)num * 0.62f;
			float num7 = (float)num2 * 0.7f;
			using GraphicsPath graphicsPath = new GraphicsPath();
			graphicsPath.AddEllipse(num4 - num6, num5 - num7, num6 * 2f, num7 * 2f);
			using PathGradientBrush pathGradientBrush = new PathGradientBrush(graphicsPath);
			pathGradientBrush.CenterPoint = new PointF(num4, num5);
			pathGradientBrush.SurroundColors = new Color[1] { Color.FromArgb(0, 38, 84, 110) };
			ColorBlend colorBlend = new ColorBlend(4);
			colorBlend.Colors = new Color[4]
			{
				Color.FromArgb(0, 38, 84, 110),
				Color.FromArgb(34, 38, 84, 110),
				Color.FromArgb(105, 38, 84, 110),
				Color.FromArgb(170, 38, 84, 110)
			};
			colorBlend.Positions = new float[4] { 0f, 0.35f, 0.7f, 1f };
			pathGradientBrush.InterpolationColors = colorBlend;
			graphics.SetClip(new RectangleF(0f, 0f, num, Math.Max(0f, Math.Min(num2, lineY))));
			graphics.FillPath(pathGradientBrush, graphicsPath);
			graphics.ResetClip();
		}
		g.DrawImageUnscaled(bgCache, 0, 0);
	}

	private static void DrawFadeLine(Graphics g, float x1, float x2, float y, float width)
	{
		if (x2 - x1 < 4f)
		{
			return;
		}
		float[] positions = new float[8] { 0f, 0.03f, 0.08f, 0.16f, 0.84f, 0.92f, 0.97f, 1f };
		float[] array = new float[8] { 0f, 0.12f, 0.4f, 1f, 1f, 0.4f, 0.12f, 0f };
		float[] array2 = new float[2] { 0.2f, 1f };
		float[] array3 = new float[2] { 4f, 1f };
		for (int i = 0; i < 2; i++)
		{
			Color[] array4 = new Color[8];
			for (int j = 0; j < 8; j++)
			{
				array4[j] = Color.FromArgb((int)(255f * array[j] * array2[i]), 92, 100, 124);
			}
			using LinearGradientBrush linearGradientBrush = new LinearGradientBrush(new PointF(x1, 0f), new PointF(x2, 0f), array4[0], array4[7]);
			ColorBlend colorBlend = new ColorBlend(8);
			colorBlend.Colors = array4;
			colorBlend.Positions = positions;
			linearGradientBrush.InterpolationColors = colorBlend;
			using Pen pen = new Pen(linearGradientBrush, width * array3[i]);
			g.DrawLine(pen, x1, y, x2, y);
		}
	}

	private static Color ToGrey(Color c, int alpha)
	{
		int num = (int)Math.Round(0.299 * (double)(int)c.R + 0.587 * (double)(int)c.G + 0.114 * (double)(int)c.B);
		return Color.FromArgb(alpha, num, num, num);
	}

	private static LinearGradientBrush Grad(RectangleF t, int alpha, bool grey)
	{
		Func<Color, Color> func = (Color c) => (!grey) ? Color.FromArgb(alpha, c) : ToGrey(c, alpha);
		return new LinearGradientBrush(new PointF(t.Left, 0f), new PointF(t.Right + 1f, 0f), func(C_RED), func(C_BLUE));
	}

	private static Color GradColor(float t)
	{
		return Lerp(C_RED, C_BLUE, t);
	}

	private const float PLUS_XSCALE = 0.8f;

	private static void DrawSkewX(Graphics g, string text, Font font, Brush brush, float x, float y, StringFormat sf, float skew, float xScale)
	{
		GraphicsState gstate = g.Save();
		g.TranslateTransform(x, 0f);
		g.ScaleTransform(xScale, 1f);
		g.TranslateTransform(0f - x, 0f);
		DrawSkew(g, text, font, brush, x, y, sf, skew / xScale);
		g.Restore(gstate);
	}

	private static void DrawSkew(Graphics g, string text, Font font, Brush brush, float x, float y, StringFormat sf, float skew)
	{
		float num = (float)font.FontFamily.GetCellAscent(font.Style) / (float)font.FontFamily.GetEmHeight(font.Style) * font.Size;
		float num2 = y + num;
		GraphicsState gstate = g.Save();
		using (Matrix matrix = new Matrix(1f, 0f, 0f - skew, 1f, skew * num2, 0f))
		{
			g.MultiplyTransform(matrix);
		}
		g.DrawString(text, font, brush, x, y, sf);
		g.Restore(gstate);
	}

	private static void BlurPass(byte[] a, byte[] tmp, int w, int h, int stride, int r, bool horiz)
	{
		int num = ((!horiz) ? h : w);
		int num2 = ((!horiz) ? w : h);
		int num3 = ((!horiz) ? stride : 4);
		int num4 = ((!horiz) ? 4 : stride);
		int num5 = 2 * r + 1;
		for (int i = 0; i < num2; i++)
		{
			int num6 = i * num4;
			for (int j = 0; j < 4; j++)
			{
				int num7 = 0;
				for (int k = -r; k <= r; k++)
				{
					if (k >= 0 && k < num)
					{
						num7 += a[num6 + k * num3 + j];
					}
				}
				for (int l = 0; l < num; l++)
				{
					tmp[num6 + l * num3 + j] = (byte)(num7 / num5);
					int num8 = l + r + 1;
					int num9 = l - r;
					if (num8 < num)
					{
						num7 += a[num6 + num8 * num3 + j];
					}
					if (num9 >= 0)
					{
						num7 -= a[num6 + num9 * num3 + j];
					}
				}
			}
		}
		Array.Copy(tmp, a, a.Length);
	}

	private static void BoxBlurBitmap(Bitmap bmp, int r)
	{
		int w = bmp.Width;
		int num = bmp.Height;
		BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, w, num), ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);
		try
		{
			int num2 = Math.Abs(bitmapData.Stride);
			byte[] array = new byte[num2 * num];
			Marshal.Copy(bitmapData.Scan0, array, 0, array.Length);
			byte[] tmp = new byte[array.Length];
			for (int i = 0; i < 3; i++)
			{
				BlurPass(array, tmp, w, num, num2, r, horiz: true);
				BlurPass(array, tmp, w, num, num2, r, horiz: false);
			}
			Marshal.Copy(array, 0, bitmapData.Scan0, array.Length);
		}
		finally
		{
			bmp.UnlockBits(bitmapData);
		}
	}

	private static float TrackFadeX(float t)
	{
		float[] pos = new float[8] { 0f, 0.03f, 0.08f, 0.16f, 0.84f, 0.92f, 0.97f, 1f };
		float[] val = new float[8] { 0f, 0.12f, 0.4f, 1f, 1f, 0.4f, 0.12f, 0f };
		t = Math.Max(0f, Math.Min(1f, t));
		for (int i = 1; i < 8; i++)
		{
			if (t <= pos[i])
			{
				float v = val[i - 1] + (val[i] - val[i - 1]) * ((t - pos[i - 1]) / (pos[i] - pos[i - 1]));
				return 1f - (1f - v) * TRACK_FADE_STRENGTH;
			}
		}
		return 0f;
	}

	private static void ApplyHorizontalFade(Bitmap bmp, float trackLeft, float trackW)
	{
		int w = bmp.Width;
		int h = bmp.Height;
		float[] fx = new float[w];
		for (int x = 0; x < w; x++)
		{
			fx[x] = TrackFadeX((((float)x + 0.5f) - trackLeft) / Math.Max(1f, trackW));
		}
		BitmapData bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);
		try
		{
			int stride = Math.Abs(bd.Stride);
			byte[] px = new byte[stride * h];
			Marshal.Copy(bd.Scan0, px, 0, px.Length);
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					int p = y * stride + x * 4;
					float f = fx[x];
					px[p] = (byte)((float)(int)px[p] * f);
					px[p + 1] = (byte)((float)(int)px[p + 1] * f);
					px[p + 2] = (byte)((float)(int)px[p + 2] * f);
					px[p + 3] = (byte)((float)(int)px[p + 3] * f);
				}
			}
			Marshal.Copy(px, 0, bd.Scan0, px.Length);
		}
		finally
		{
			bmp.UnlockBits(bd);
		}
	}

	private void DrawFadedTrack(Graphics g, Fan fan, float knobX)
	{
		RectangleF track = fan.Track;
		int pad = 2;
		int ix = (int)Math.Floor(track.Left) - pad;
		int iy = (int)Math.Floor(track.Top) - pad;
		int w = (int)Math.Ceiling(track.Width) + pad * 2 + 2;
		int h = (int)Math.Ceiling(track.Height) + pad * 2 + 2;
		RectangleF local = new RectangleF(track.Left - (float)ix, track.Top - (float)iy, track.Width, track.Height);
		float fill = knobX - track.Left;
		string key = w + "x" + h + "|" + local.Left.ToString("0.00") + "|" + local.Top.ToString("0.00") + "|" + local.Width.ToString("0.00") + "|" + local.Height.ToString("0.00") + "|" + fill.ToString("0.0");
		if (fan.Fixed == null || fan.FixedKey != key)
		{
			if (fan.Fixed != null)
			{
				fan.Fixed.Dispose();
			}
			Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
			using (Graphics gb = Graphics.FromImage(bmp))
			{
				gb.SmoothingMode = SmoothingMode.AntiAlias;
				using GraphicsPath path = RoundRect(local, local.Height / 2f);
				using LinearGradientBrush dimBrush = Grad(local, 88, grey: false);
				using LinearGradientBrush fullBrush = Grad(local, 255, grey: false);
				using (Brush bg = new SolidBrush(TRACK))
				{
					gb.FillPath(bg, path);
				}
				gb.FillPath(dimBrush, path);
				gb.SetClip(path);
				gb.FillRectangle(fullBrush, local.Left, local.Top, fill, local.Height);
				gb.ResetClip();
			}
			ApplyHorizontalFade(bmp, local.Left, local.Width);
			fan.Fixed = bmp;
			fan.FixedKey = key;
		}
		g.DrawImage(fan.Fixed, new Rectangle(ix, iy, w, h));
	}

	private void DrawBlurredTrack(Graphics g, Fan fan, float shown)
	{
		RectangleF track = fan.Track;
		int num = Math.Max(2, (int)Math.Round(track.Height * 0.22f));
		int num2 = num * 2 + 2;
		int num3 = (int)Math.Ceiling(track.Width) + num2 * 2;
		int num4 = (int)Math.Ceiling(track.Height) + num2 * 2;
		string text = num3 + "x" + num4 + "|" + num + "|" + ((!fan.Available) ? "n" : Math.Round(shown).ToString());
		if (fan.Blur == null || fan.BlurKey != text)
		{
			if (fan.Blur != null)
			{
				fan.Blur.Dispose();
			}
			Bitmap bitmap = new Bitmap(num3, num4, PixelFormat.Format32bppPArgb);
			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				RectangleF t = new RectangleF(num2, num2, track.Width, track.Height);
				using GraphicsPath graphicsPath = RoundRect(t, t.Height / 2f);
				using LinearGradientBrush brush = Grad(t, 70, grey: true);
				using LinearGradientBrush brush2 = Grad(t, 215, grey: true);
				using (Brush brush3 = new SolidBrush(TRACK))
				{
					graphics.FillPath(brush3, graphicsPath);
				}
				graphics.FillPath(brush, graphicsPath);
				if (fan.Available)
				{
					float num5 = t.Left + t.Width * shown / 100f;
					graphics.SetClip(graphicsPath);
					graphics.FillRectangle(brush2, t.Left, t.Top, num5 - t.Left, t.Height);
					graphics.ResetClip();
				}
			}
			BoxBlurBitmap(bitmap, num);
			ApplyHorizontalFade(bitmap, num2, track.Width);
			fan.Blur = bitmap;
			fan.BlurKey = text;
		}
		g.DrawImage(fan.Blur, (int)Math.Round(track.Left - (float)num2), (int)Math.Round(track.Top - (float)num2), num3, num4);
	}

	private static void DrawLock(Graphics g, float cx, float cy, float rr)
	{
		float num = rr * 1.25f;
		float num2 = rr * 0.95f;
		RectangleF rectangleF = new RectangleF(cx - num / 2f, cy - num2 * 0.15f, num, num2);
		float num3 = num * 0.62f;
		float num4 = cy - rr * 0.95f;
		RectangleF rect = new RectangleF(cx - num3 / 2f, num4, num3, num3);
		float lockTop = num4 - Math.Max(2f, rr * 0.22f) / 2f;
		float lockH = (rectangleF.Bottom - lockTop) / 0.58f;
		using Brush lockBrush = WattBrush(lockTop - 0.2f * lockH, lockH, dim: false);
		using (Pen pen = new Pen(lockBrush, Math.Max(2f, rr * 0.22f)))
		{
			g.DrawArc(pen, rect, 180f, 180f);
			g.DrawLine(pen, rect.Left, rect.Top + num3 / 2f, rect.Left, rectangleF.Top + 1f);
			g.DrawLine(pen, rect.Right, rect.Top + num3 / 2f, rect.Right, rectangleF.Top + 1f);
		}
		using (GraphicsPath path = RoundRect(rectangleF, num * 0.18f))
		{
			g.FillPath(lockBrush, path);
		}
		float num5 = num * 0.2f;
		using Brush brush2 = new SolidBrush(Color.FromArgb(40, 42, 50));
		g.FillEllipse(brush2, cx - num5 / 2f, rectangleF.Top + num2 * 0.26f, num5, num5);
		g.FillRectangle(brush2, cx - num5 * 0.18f, rectangleF.Top + num2 * 0.26f + num5 * 0.6f, num5 * 0.36f, num2 * 0.28f);
	}

	private static void DrawKnobShadow(Graphics g, float cx, float cy, float rr)
	{
		float dy = rr * 0.22f;
		int[] alphas = new int[4] { 28, 36, 46, 60 };
		float[] grow = new float[4] { 0.55f, 0.35f, 0.18f, 0f };
		for (int i = 0; i < 4; i++)
		{
			float rs = rr + rr * grow[i];
			using Brush brush = new SolidBrush(Color.FromArgb(alphas[i], 0, 0, 0));
			g.FillEllipse(brush, cx - rs, cy + dy - rs, rs * 2f, rs * 2f);
		}
	}

	private static void DrawTrackText(Graphics g, string text, Font f, float cx, float cy, StringFormat sf, Color c, int alpha)
	{
		if (string.IsNullOrEmpty(text) || alpha <= 0)
		{
			return;
		}
		int style = (int)f.Style;
		float refMid;
		using (GraphicsPath refPath = new GraphicsPath())
		{
			refPath.AddString("H", f.FontFamily, style, f.Size, new PointF(0f, 0f), StringFormat.GenericTypographic);
			RectangleF refB = refPath.GetBounds();
			refMid = refB.Top + refB.Height / 2f;
		}
		using GraphicsPath path = new GraphicsPath();
		path.AddString(text, f.FontFamily, style, f.Size, new PointF(0f, 0f), StringFormat.GenericTypographic);
		RectangleF tb = path.GetBounds();
		float dx = cx - (tb.Left + tb.Width / 2f);
		float dy = cy - refMid;
		SmoothingMode oldMode = g.SmoothingMode;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		float num = Math.Max(1f, f.Size * 0.07f);
		float[] offs = new float[3] { 1.7f, 1.1f, 0.6f };
		int[] al = new int[3] { 40, 70, 100 };
		for (int i = 0; i < 3; i++)
		{
			using GraphicsPath sh = (GraphicsPath)path.Clone();
			using Matrix shM = new Matrix(1f, 0f, 0f, 1f, dx + num * offs[i], dy + num * offs[i]);
			sh.Transform(shM);
			using Brush shBrush = new SolidBrush(Color.FromArgb(alpha * al[i] / 255, 0, 0, 0));
			g.FillPath(shBrush, sh);
		}
		using (Matrix m2 = new Matrix(1f, 0f, 0f, 1f, dx, dy))
		{
			path.Transform(m2);
		}
		using (Brush fill = new SolidBrush(Color.FromArgb(alpha, c)))
		{
			g.FillPath(fill, path);
		}
		g.SmoothingMode = oldMode;
	}

	private static GraphicsPath RoundRect(RectangleF r, float rad)
	{
		float num = rad * 2f;
		GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddArc(r.Left, r.Top, num, num, 180f, 90f);
		graphicsPath.AddArc(r.Right - num, r.Top, num, num, 270f, 90f);
		graphicsPath.AddArc(r.Right - num, r.Bottom - num, num, num, 0f, 90f);
		graphicsPath.AddArc(r.Left, r.Bottom - num, num, num, 90f, 90f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}

	private void DrawMax(Graphics g)
	{
		double totalMilliseconds = (DateTime.Now - maxStart).TotalMilliseconds;
		if (totalMilliseconds < 0.0 || totalMilliseconds > 2200.0)
		{
			return;
		}
		float alpha = (float)Math.Max(0.0, Math.Min(1.0, Math.Min(totalMilliseconds / 250.0, (2200.0 - totalMilliseconds) / 600.0)));
		using (Brush brush = new SolidBrush(Color.FromArgb((int)(150f * alpha), 0, 0, 0)))
		{
			g.FillRectangle(brush, 0, 0, ClientSize.Width, ClientSize.Height);
		}
		string s = "MAXIMUM POWER!";
		using StringFormat stringFormat = (StringFormat)StringFormat.GenericTypographic.Clone();
		stringFormat.FormatFlags |= StringFormatFlags.NoWrap;
		float val;
		using (Font font = new Font("Segoe UI Black", 100f, FontStyle.Regular, GraphicsUnit.Pixel))
		{
			SizeF sizeF = g.MeasureString(s, font, 10000, stringFormat);
			val = 100f * Math.Min((float)ClientSize.Width * 0.92f / sizeF.Width, (float)ClientSize.Height * 0.4f / sizeF.Height);
		}
		val = Math.Max(10f, val);
		using Font font2 = new Font("Segoe UI Black", val, FontStyle.Regular, GraphicsUnit.Pixel);
		SizeF sizeF2 = g.MeasureString(s, font2, 10000, stringFormat);
		float num = ((float)ClientSize.Width - sizeF2.Width) / 2f;
		float num2 = ((float)ClientSize.Height - sizeF2.Height) / 2f;
		using (Brush brush2 = new SolidBrush(Color.FromArgb((int)(220f * alpha), 0, 0, 0)))
		{
			g.DrawString(s, font2, brush2, num + val * 0.04f, num2 + val * 0.04f, stringFormat);
		}
		Color[] rb = new Color[8]
		{
			Color.Red,
			Color.Orange,
			Color.Yellow,
			Color.Lime,
			Color.Cyan,
			Color.DodgerBlue,
			Color.Magenta,
			Color.Red
		};
		ColorBlend colorBlend = new ColorBlend(rb.Length);
		colorBlend.Colors = rb.Select((Color c) => Color.FromArgb((int)(255f * alpha), c)).ToArray();
		colorBlend.Positions = (from i in Enumerable.Range(0, rb.Length)
			select (float)i / (float)(rb.Length - 1)).ToArray();
		using LinearGradientBrush linearGradientBrush = new LinearGradientBrush(new RectangleF(num, num2, sizeF2.Width, sizeF2.Height), Color.Red, Color.Red, LinearGradientMode.Horizontal);
		linearGradientBrush.InterpolationColors = colorBlend;
		linearGradientBrush.WrapMode = WrapMode.Tile;
		linearGradientBrush.TranslateTransform((float)(totalMilliseconds * 0.5 % (double)sizeF2.Width), 0f);
		g.DrawString(s, font2, linearGradientBrush, num, num2, stringFormat);
	}
}
