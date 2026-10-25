using System;
using System.Drawing;
using System.Windows.Forms;

internal class InfoForm : Form
{
	private static readonly Color BG = Color.FromArgb(16, 17, 20);

	private static readonly Color FG = Color.FromArgb(232, 232, 232);

	private static readonly Color DIM = Color.FromArgb(122, 125, 133);

	private static readonly Color BLUE = Color.FromArgb(96, 165, 250);

	private static readonly Color TRACK = Color.FromArgb(52, 55, 62);

	private readonly RichTextBox box;

	private readonly CheckBox chkSave;

	private readonly CheckBox chkAuto;

	private readonly CheckBox chkGame;

	private readonly Button btnClose;

	private readonly ComboBox cmbLang;

	private string verText;

	private string authText;

	private readonly NumericUpDown[] spLo = new NumericUpDown[3];

	private readonly NumericUpDown[] spHi = new NumericUpDown[3];

	private readonly Label lblScale;

	private readonly Button btnReset;

	private readonly ToolTip tip = new ToolTip();

	private bool tempBusy;

	private readonly Font titleFont = new Font("Segoe UI", 18f, FontStyle.Bold);

	private readonly Font headFont = new Font("Segoe UI", 11f, FontStyle.Bold);

	private readonly Font bodyFont = new Font("Segoe UI", 9.5f);

	private readonly Font keyFont = new Font("Consolas", 9.5f, FontStyle.Bold);

	public bool SaveSettings => chkSave.Checked;

	public bool AutoStart => chkAuto.Checked;

	public bool StopMatrixInGames => chkGame.Checked;

	public bool English => Loc.En;

	public int[] TempLo => new int[3] { (int)spLo[0].Value, (int)spLo[1].Value, (int)spLo[2].Value };

	public int[] TempHi => new int[3] { (int)spHi[0].Value, (int)spHi[1].Value, (int)spHi[2].Value };

	public InfoForm(string version, string author, int dpi, bool saveSettings, bool autoStart, bool stopMatrix, int[] tempLo, int[] tempHi)
	{
		float num = ((dpi <= 0) ? 1f : ((float)dpi / 96f));
		Text = Loc.T("Info zu PC Überblick", "About PC Überblick");
		verText = version;
		authText = author;
		try
		{
			using (System.IO.Stream iconStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico"))
			{
				Icon = new System.Drawing.Icon(iconStream);
			}
		}
		catch
		{
		}
		BackColor = BG;
		ForeColor = FG;
		AutoScaleMode = AutoScaleMode.None;
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		MinimizeBox = false;
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.CenterParent;
		ClientSize = new Size((int)(620f * num), (int)(668f * num));
		int num2 = (int)(110f * num);
		Panel panel = new Panel
		{
			BackColor = BG,
			Location = new Point(0, 0),
			Size = new Size(ClientSize.Width, ClientSize.Height - num2),
			Padding = new Padding((int)(20f * num), (int)(16f * num), (int)(12f * num), (int)(8f * num)),
			Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right)
		};
		Controls.Add(panel);
		box = new RichTextBox();
		box.Dock = DockStyle.Fill;
		box.BorderStyle = BorderStyle.None;
		box.BackColor = BG;
		box.ForeColor = FG;
		box.TabStop = false;
		box.DetectUrls = false;
		box.ScrollBars = RichTextBoxScrollBars.Vertical;
		panel.Controls.Add(box);
		Button button = new Button();
		button.Text = Loc.T("Schließen", "Close");
		btnClose = button;
		button.FlatStyle = FlatStyle.Flat;
		button.FlatAppearance.BorderColor = TRACK;
		button.BackColor = Color.FromArgb(40, 43, 52);
		button.ForeColor = FG;
		button.Size = new Size((int)(120f * num), (int)(34f * num));
		button.Location = new Point(ClientSize.Width - button.Width - (int)(20f * num), ClientSize.Height - num2 + (int)(10f * num));
		button.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
		button.DialogResult = DialogResult.OK;
		Controls.Add(button);
		chkAuto = new CheckBox();
		chkAuto.Text = Loc.T("Mit Windows starten", "Start with Windows");
		chkAuto.Checked = autoStart;
		chkAuto.AutoSize = false;
		chkAuto.ForeColor = FG;
		chkAuto.BackColor = BG;
		chkAuto.Font = bodyFont;
		chkAuto.Size = new Size(ClientSize.Width - button.Width - (int)(60f * num), (int)(24f * num));
		chkAuto.Location = new Point((int)(20f * num), ClientSize.Height - num2 + (int)(4f * num));
		chkAuto.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
		Controls.Add(chkAuto);
		chkSave = new CheckBox();
		chkSave.Text = Loc.T("Einstellungen speichern (Fenster, Lüfterwerte, Vordergrund)", "Save settings (window, fan values, always on top)");
		chkSave.Checked = saveSettings;
		chkSave.AutoSize = false;
		chkSave.ForeColor = FG;
		chkSave.BackColor = BG;
		chkSave.Font = bodyFont;
		chkSave.Size = new Size(ClientSize.Width - button.Width - (int)(60f * num), (int)(24f * num));
		chkSave.Location = new Point((int)(20f * num), ClientSize.Height - num2 + (int)(28f * num));
		chkSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
		Controls.Add(chkSave);
		chkGame = new CheckBox();
		chkGame.Text = Loc.T("Matrix bei Vollbildspielen anhalten", "Pause Matrix during fullscreen games");
		chkGame.Checked = stopMatrix;
		chkGame.AutoSize = false;
		chkGame.ForeColor = FG;
		chkGame.BackColor = BG;
		chkGame.Font = bodyFont;
		chkGame.Size = new Size(ClientSize.Width - button.Width - (int)(60f * num), (int)(24f * num));
		chkGame.Location = new Point((int)(20f * num), ClientSize.Height - num2 + (int)(52f * num));
		chkGame.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
		Controls.Add(chkGame);
		int rowY = ClientSize.Height - num2 + (int)(78f * num);
		lblScale = new Label();
		lblScale.AutoSize = false;
		lblScale.ForeColor = FG;
		lblScale.BackColor = BG;
		lblScale.Font = bodyFont;
		lblScale.TextAlign = ContentAlignment.MiddleLeft;
		lblScale.Location = new Point((int)(20f * num), rowY);
		lblScale.Size = new Size((int)(100f * num), (int)(24f * num));
		lblScale.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
		Controls.Add(lblScale);
		string[] scaleNames = new string[3] { "GPU", "CPU", "Hotspot" };
		int scaleX = (int)(122f * num);
		for (int si = 0; si < 3; si++)
		{
			Label nameLbl = new Label();
			nameLbl.AutoSize = false;
			nameLbl.ForeColor = DIM;
			nameLbl.BackColor = BG;
			nameLbl.Font = bodyFont;
			nameLbl.Text = scaleNames[si];
			nameLbl.TextAlign = ContentAlignment.MiddleLeft;
			nameLbl.Location = new Point(scaleX, rowY);
			nameLbl.Size = new Size((int)(((si == 2) ? 56f : 34f) * num), (int)(24f * num));
			nameLbl.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
			Controls.Add(nameLbl);
			scaleX += nameLbl.Width;
			spLo[si] = MakeSpin(tempLo[si], Color.FromArgb(110, 195, 255), scaleX, rowY, num);
			scaleX += (int)(48f * num);
			spHi[si] = MakeSpin(tempHi[si], Color.FromArgb(255, 96, 64), scaleX, rowY, num);
			scaleX += (int)(48f * num) + (int)(12f * num);
			int idx = si;
			spLo[si].ValueChanged += delegate
			{
				FixPair(idx, lowChanged: true);
			};
			spHi[si].ValueChanged += delegate
			{
				FixPair(idx, lowChanged: false);
			};
		}
		btnReset = new Button();
		btnReset.Text = "\u21BA";
		btnReset.Font = new Font("Segoe UI Symbol", 10f);
		btnReset.FlatStyle = FlatStyle.Flat;
		btnReset.FlatAppearance.BorderColor = TRACK;
		btnReset.BackColor = Color.FromArgb(40, 43, 52);
		btnReset.ForeColor = FG;
		btnReset.Location = new Point(scaleX - (int)(2f * num), rowY - 1);
		btnReset.Size = new Size((int)(30f * num), (int)(26f * num));
		btnReset.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
		btnReset.TabStop = false;
		btnReset.Click += delegate
		{
			for (int ri = 0; ri < 3; ri++)
			{
				spLo[ri].Value = 40m;
				spHi[ri].Value = 70m;
			}
		};
		Controls.Add(btnReset);
		ApplyScaleTexts();
		cmbLang = new ComboBox();
		cmbLang.DropDownStyle = ComboBoxStyle.DropDownList;
		cmbLang.FlatStyle = FlatStyle.Flat;
		cmbLang.BackColor = Color.FromArgb(40, 43, 52);
		cmbLang.ForeColor = FG;
		cmbLang.Font = bodyFont;
		cmbLang.Items.AddRange(new object[2] { "Deutsch", "English" });
		cmbLang.SelectedIndex = (Loc.En ? 1 : 0);
		cmbLang.Width = (int)(120f * num);
		cmbLang.Location = new Point(ClientSize.Width - cmbLang.Width - (int)(20f * num), ClientSize.Height - num2 + (int)(48f * num));
		cmbLang.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
		cmbLang.SelectedIndexChanged += delegate
		{
			Loc.En = cmbLang.SelectedIndex == 1;
			Retext();
		};
		Controls.Add(cmbLang);
		cmbLang.BringToFront();
		AcceptButton = button;
		CancelButton = button;
		BuildText(version, author);
		box.ReadOnly = true;
		box.SelectionStart = 0;
		box.SelectionLength = 0;
		ActiveControl = button;
	}

	private NumericUpDown MakeSpin(int value, Color color, int x, int y, float scale)
	{
		NumericUpDown n = new NumericUpDown();
		n.Minimum = 0m;
		n.Maximum = 100m;
		n.Value = Math.Max(0, Math.Min(100, value));
		n.Font = bodyFont;
		n.BackColor = Color.FromArgb(40, 43, 52);
		n.ForeColor = color;
		n.BorderStyle = BorderStyle.FixedSingle;
		n.TextAlign = HorizontalAlignment.Center;
		n.Location = new Point(x, y + 1);
		n.Width = (int)(46f * scale);
		n.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
		Controls.Add(n);
		return n;
	}

	private void FixPair(int i, bool lowChanged)
	{
		if (tempBusy)
		{
			return;
		}
		tempBusy = true;
		try
		{
			decimal lo = spLo[i].Value;
			decimal hi = spHi[i].Value;
			if (lo >= hi)
			{
				if (lowChanged)
				{
					if (lo >= 100m)
					{
						spLo[i].Value = 99m;
						spHi[i].Value = 100m;
					}
					else
					{
						spHi[i].Value = lo + 1m;
					}
				}
				else if (hi <= 0m)
				{
					spLo[i].Value = 0m;
					spHi[i].Value = 1m;
				}
				else
				{
					spLo[i].Value = hi - 1m;
				}
			}
		}
		finally
		{
			tempBusy = false;
		}
	}

	private void ApplyScaleTexts()
	{
		lblScale.Text = Loc.T("Farbskala in °C", "Color scale in °C");
		for (int i = 0; i < 3; i++)
		{
			tip.SetToolTip(spLo[i], Loc.T("Ab dieser Temperatur beginnt der Verlauf (Eisblau)", "The gradient starts at this temperature (ice blue)"));
			tip.SetToolTip(spHi[i], Loc.T("Ab dieser Temperatur ist die Zahl voll Feuerrot", "At this temperature the number is fully fire red"));
		}
		tip.SetToolTip(btnReset, Loc.T("Zurücksetzen auf 40° und 70°", "Reset to 40° and 70°"));
	}

	private void Retext()
	{
		ApplyScaleTexts();
		Text = Loc.T("Info zu PC Überblick", "About PC Überblick");
		btnClose.Text = Loc.T("Schließen", "Close");
		chkAuto.Text = Loc.T("Mit Windows starten", "Start with Windows");
		chkSave.Text = Loc.T("Einstellungen speichern (Fenster, Lüfterwerte, Vordergrund)", "Save settings (window, fan values, always on top)");
		chkGame.Text = Loc.T("Matrix bei Vollbildspielen anhalten", "Pause Matrix during fullscreen games");
		box.ReadOnly = false;
		box.Clear();
		BuildText(verText, authText);
		box.ReadOnly = true;
		box.SelectionStart = 0;
		box.SelectionLength = 0;
		btnClose.Focus();
	}

	private void Add(string text, Font font, Color color)
	{
		box.SelectionStart = box.TextLength;
		box.SelectionLength = 0;
		box.SelectionFont = font;
		box.SelectionColor = color;
		box.SelectedText = text;
	}

	private void Title(string text)
	{
		Add(text + "\n", titleFont, FG);
	}

	private void Head(string text)
	{
		Add("\n", bodyFont, FG);
		Add(text + "\n", headFont, BLUE);
	}

	private void Body(string text)
	{
		Add(text + "\n", bodyFont, FG);
	}

	private void Dim(string text)
	{
		Add(text + "\n", bodyFont, DIM);
	}

	private void Line(string key, string text)
	{
		Add(key, keyFont, FG);
		Add("  " + text + "\n", bodyFont, FG);
	}

	private void BuildText(string version, string author)
	{
		if (Loc.En)
		{
			BuildEn(version, author);
			return;
		}
		Title("PC Überblick");
		Dim("Version " + version + "   ·   von " + author);
		Head("Was zeigt das Programm?");
		Body("Temperatur von GPU und CPU (bei der GPU zusätzlich der Hotspot), die Leistungsaufnahme in Watt und eine geschätzte Gesamtleistung.");
		Body("Die Schätzung ist die Summe aus GPU und CPU plus ein Aufschlag von 30 Watt und 14 % der Summe. Der Aufschlag deckt ab, was sich nicht messen lässt (Mainboard, RAM, Laufwerke, Lüfter, Netzteilverluste) und steht in Klammern dahinter.");
		Head("Lüftersteuerung");
		Line("GPU Lüfter", "Steuert die Lüfter der Grafikkarte, sofern die Karte das zulässt.");
		Line("CPU Lüfter", "Steuert alle steuerbaren Lüfteranschlüsse des Mainboards. Das können auch Gehäuselüfter sein.");
		Body("Regler ziehen oder anklicken stellt einen festen Wert in Prozent ein. Im Regler stehen dann links die Drehzahl (RPM) und rechts der Prozentwert. Beim CPU Lüfter zeigt die Drehzahl den schnellsten der gesteuerten Lüfter.");
		Body("Auto Button oder Rechtsklick auf den Regler gibt die Regelung wieder an den PC zurück. Das Schloss zeigt, dass die Automatik läuft. Beim Schließen des Programms werden beide Lüfter wieder auf Auto gestellt.");
		Body("\"n/a\" bedeutet: Dieser Lüfter ist auf diesem PC nicht steuerbar.");
		Head("Schutzfunktion");
		Body("Wird die CPU 85 °C heiß oder die GPU 80 °C (Hotspot 90 °C), stellt das Programm den betroffenen Lüfter automatisch zurück auf Auto und zeigt eine Warnung an. Bei 100 % greift der Schutz nicht ein.");
		Head("Tasten und Maus");
		Line("F11", "Vollbild an/aus. Das geht auch mit Doppelklick auf eine freie Fläche.");
		Line("Esc", "Vollbild beenden.");
		Line("T", "Fenster immer im Vordergrund an/aus.");
		Line("D", "Sensorliste als Textdatei auf dem Desktop speichern (hilfreich bei Problemen).");
		Line("I", "PawnIO Treiber per winget installieren.");
		Line("Info i", "Öffnet dieses Fenster.");
		Head("Designs");
		Body("Unten rechts über der Linie wählst du zwischen \"Standard\" und \"Matrix\" (schwarzer Hintergrund mit fallendem Zeichenregen). Der Zeichenregen läuft auch bei Vollbildspielen weiter. Wer dabei Leistung sparen will, setzt unten das Häkchen \"Matrix bei Vollbildspielen anhalten\".");
		Head("Einstellungen");
		Body("Fenstergröße und Position, Vollbild, \"Immer im Vordergrund\", das Design, die Sprache und die Lüfterwerte werden automatisch gespeichert und beim nächsten Start wiederhergestellt. Das Speichern lässt sich unten mit dem Häkchen ausschalten.");
		Body("Die Farbskala der Temperaturzahlen stellst du ganz unten ein, getrennt für GPU, CPU und Hotspot. Die linke Zahl ist die Temperatur für Eisblau, die rechte die für Feuerrot. Der Pfeil setzt alles auf 40 und 70 Grad zurück.");
		Head("Zusatz Infos");
		Body("Administratorrechte: Das Programm startet sich bei Bedarf selbst neu und fragt danach.");
		Body("Für CPU Temperatur, CPU Watt und den CPU Lüfter wird der kostenlose Treiber PawnIO gebraucht. Fehlt er, bietet das Programm beim Start die Installation an.");
		Body("Die Sensordaten kommen von LibreHardwareMonitor.");
	}


	private void BuildEn(string version, string author)
	{
		Title("PC Überblick");
		Dim("Version " + version + "   ·   by " + author);
		Head("What does the program show?");
		Body("Temperature of GPU and CPU (for the GPU also the hotspot), the power draw in watts and an estimated total power.");
		Body("The estimate is the sum of GPU and CPU plus a surcharge of 30 watts and 14 % of the sum. The surcharge covers what cannot be measured (mainboard, RAM, drives, fans, power supply losses) and is shown in brackets behind it.");
		Head("Fan control");
		Line("GPU Fan", "Controls the fans of the graphics card, if the card allows it.");
		Line("CPU Fan", "Controls all controllable fan headers of the mainboard. These can also be case fans.");
		Body("Dragging or clicking the slider sets a fixed value in percent. The slider then shows the speed (RPM) on the left and the percentage on the right. For the CPU fan the speed shows the fastest of the controlled fans.");
		Body("The Auto button or a right click on the slider hands control back to the PC. The lock shows that automatic mode is running. When the program is closed, both fans are set back to Auto.");
		Body("\"n/a\" means: this fan cannot be controlled on this PC.");
		Head("Protection");
		Body("If the CPU reaches 85 °C or the GPU 80 °C (hotspot 90 °C), the program automatically sets the affected fan back to Auto and shows a warning. At 100 % the protection does not step in.");
		Head("Keys and mouse");
		Line("F11", "Fullscreen on or off. A double click on an empty area works too.");
		Line("Esc", "Leave fullscreen.");
		Line("T", "Window always on top on or off.");
		Line("D", "Save a sensor list as a text file on the desktop (helpful for problems).");
		Line("I", "Install the PawnIO driver with winget.");
		Line("Info i", "Opens this window.");
		Head("Designs");
		Body("At the bottom right above the line you choose between \"Standard\" and \"Matrix\" (black background with falling characters). The character rain keeps running during fullscreen games. To save performance there, tick \"Pause Matrix during fullscreen games\" at the bottom.");
		Head("Settings");
		Body("Window size and position, fullscreen, \"Always on top\", the design, the language and the fan values are saved automatically and restored at the next start. Saving can be switched off with the tick box at the bottom.");
		Body("You set the color scale of the temperature numbers at the very bottom, separately for GPU, CPU and hotspot. The left number is the temperature for ice blue, the right one the temperature for fire red. The arrow resets everything to 40 and 70 degrees.");
		Head("Extra info");
		Body("Administrator rights: the program restarts itself when needed and asks for them.");
		Body("For CPU temperature, CPU watts and the CPU fan the free driver PawnIO is needed. If it is missing, the program offers to install it at startup.");
		Body("The sensor data comes from LibreHardwareMonitor.");
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			titleFont.Dispose();
			headFont.Dispose();
			bodyFont.Dispose();
			keyFont.Dispose();
		}
		base.Dispose(disposing);
	}
}
