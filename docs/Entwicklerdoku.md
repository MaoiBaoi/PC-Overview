# PCUeberblick v1.0 – Entwicklerdokumentation (Übergabe an andere Agenten)

Autor/Besitzer: MaoiBaoi. Sprache von UI und Kommentaren: Deutsch. Bitte Antworten an den Besitzer auf Deutsch schreiben.

Stand: 25.10.2026 (nach Runde 7, Hintergrund Abfrage, Spielmodus Option, Parallax; davor Runde 6, Icon und Matrix Farben; davor Runde 5, Feinschliff Optik; davor Runde 4, Leistung und Stabilität). Diese Doku beschreibt den Code so, wie er im Projektordner liegt (Version 1.0, Konstante `AppVersion = "1.0"`). Die frühere Doku v0.9 war veraltet (Einstellungen, Autostart, Matrix-Design, RPM-Anzeige fehlten); alles Nachgetragene stammt aus dem Quellcode. Was in Runde 3 und Runde 4 geändert wurde, steht in Abschnitt 11. **Runde 4 (Spiele werden gebremst oder stürzen ab) ist in 4.12 und 12.7 beschrieben, dort stehen auch die Messwerte und die offenen Vermutungen.** Hintergründe, Herleitungen und verworfene Ideen aus dem Chat mit dem Besitzer stehen neu in Abschnitt 12.

## 1. Was ist das?

Kleines Windows-Programm (C# / WinForms, .NET Framework 4.7.2), das GPU- und CPU-Temperatur sowie Leistungsaufnahme groß und gut lesbar anzeigt und zwei Lüfter-Regler bietet (GPU-Lüfter, CPU-Lüfter). Sensorik und Lüftersteuerung kommen von **LibreHardwareMonitorLib** (Open Source). Für CPU-Werte und Mainboard-Lüfter braucht LibreHardwareMonitor den Kernel-Treiber **PawnIO**.

- Läuft nur unter Windows, braucht **Administrator-Rechte** (das Programm startet sich selbst mit `runas` neu).
- Ergebnis ist **eine einzige .exe**: alle DLLs, 39 PNG-Frames, `info.png` und eine WAV-Datei sind als Ressourcen eingebettet.
- Der Quellcode stammt ursprünglich aus einem **ILSpy-Export** (Dekompilat). Das erklärt den Stil (siehe Abschnitt 8).

## 2. Dateiübersicht

| Datei | Zeilen | Inhalt |
|---|---|---|
| `Program.cs` | 163 | Einstieg `Main()`: DPI-Awareness, Admin-Check und Selbstneustart per `runas`, **Priorität „Unter normal“**, **Fehlerbehandlung (`OnThreadException`, `OnFatal`, `ProcessExit` mit Notfall-Freigabe der Lüfter)**, `AssemblyResolve`-Handler (lädt eingebettete DLLs aus den Ressourcen), startet `MainForm`. `IsAdmin()` ist öffentlich. |
| `MainForm.cs` | 2448 | Das gesamte Hauptfenster: Sensoren, Lüftersteuerung, Eingabe, Zeichnen, Matrix-Design, Info-Glanz, Video/Sound, PawnIO-Abfrage, Einstellungen speichern. Das Herzstück. |
| `InfoForm.cs` | 187 | Info-Dialog („i“-Button): Version, Autor, Dokumentationstext, Häkchen „Mit Windows starten“ und „Einstellungen speichern“. |
| `Settings.cs` | 147 | Lädt/speichert `settings.ini` in `%APPDATA%\PCUeberblick` (Fenster, Vollbild, Vordergrund, Design, Lüfterwerte). |
| `Loc.cs` | 25 | **Neu in Runde 12.** Sprachumschaltung (`Loc.T(de, en)`). |
| `Log.cs` | 35 | **Neu in Runde 4.** Einfaches Protokoll `%APPDATA%\PCUeberblick\log.txt` (siehe 4.12). |
| `Autostart.cs` | 101 | „Mit Windows starten“ über die Windows-Aufgabenplanung (`schtasks.exe`, Aufgabe `PCUeberblick`). |
| `Fan.cs` | 42 | Datenklasse für einen Lüfter-Regler (Sensorliste, RPM-Sensoren, Sollwert, Animationswert `Anim` für das Ein-/Ausblenden der Reglertexte, zuletzt gezeigte Texte `LastPctText`/`LastRpmText`, Zeichen-Rechtecke, Blur-Cache). |
| `Readings.cs` | 12 | Datenklasse mit den aktuellen Messwerten (`GpuTemp`, `GpuHot`, `GpuW`, `CpuTemp`, `CpuW`, alle `float?`). |
| `Properties/AssemblyInfo.cs` | ~10 | Assembly-Version 1.0.0.0. |
| `PCUeberblick.csproj` | – | SDK-Style-Projekt, `net472`, `WinExe`, `UseWindowsForms`, `LangVersion 14.0`, `AllowUnsafeBlocks`, Ressourcen-Einbettung (siehe Abschnitt 6). |
| `f00.png … f38.png` | 39 | Einzelbilder der „MAX“-Animation (Video wird als Bildfolge abgespielt). |
| `max.wav` | 1 | Sound zur Animation. |
| `info.png` | 1 | Symbol im „i“-Button (wird zur Laufzeit eingefärbt). |
| `*.dll` | 22 | Eingebettete Abhängigkeiten (LibreHardwareMonitorLib, HidSharp, BlackSharp.Core, DiskInfoToolkit, RAMSPDToolkit-NDD, System.*-Backports). |

Eine `build.bat` liegt im aktuellen Projektordner nicht bei; Build siehe Abschnitt 6.

## 3. Ablauf beim Start

1. `Program.Main()` setzt per `SetProcessDpiAwarenessContext(-4)` Per-Monitor-DPI (Fallback `SetProcessDPIAware`).
2. Kein Administrator? → Programm mit `Verb = "runas"` neu starten und beenden. Schlägt das fehl (UAC abgelehnt), läuft es trotzdem weiter, dann zeigt es eine Statusmeldung.
3. Priorität auf „Unter normal“ setzen (`LowerPriority`). Danach `AssemblyResolve` registrieren: sucht fehlende Assemblies (`<Name>.dll`) in den eingebetteten Ressourcen und lädt sie aus dem Speicher. **Deshalb darf `Start()` nicht inline-optimiert werden** (`[MethodImpl(NoInlining)]`): `MainForm` darf erst nach dem Registrieren des Handlers geladen werden, weil es `LibreHardwareMonitorLib` referenziert.
4. `new MainForm()`:
   - `cfg = Settings.Load()` (Feldinitialisierer). Ist Speichern aktiv (`cfg.Enabled`): gespeicherte Fensterposition/-größe (nur wenn noch auf einem Bildschirm sichtbar, `IsOnScreen()`), `TopMost`, Design, Maximiert werden angewendet.
   - Fenster standardmäßig 760×520 (DPI-skaliert), Mindestgröße 160×120, Doppelpuffer, `ResizeRedraw`, `KeyPreview`.
   - `pawnOk = PawnIoInstalled()` (Registry-Schlüssel `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO`, 64- und 32-Bit-Sicht).
   - `Computer` (LibreHardwareMonitor) mit `IsCpuEnabled`, `IsGpuEnabled`, `IsMotherboardEnabled`, dann `Open()`, `DiscoverFans()` und `RestoreFans()` (gespeicherte Prozentwerte wieder setzen). Fehler dabei landen als Text in `status`.
   - Timer `timer` (1000 ms) → `UpdateReadings()` + `Invalidate()`.
   - Timer `matrixTimer` (40 ms) → `UpdateMatrix()` + `Invalidate()`; läuft **nur**, solange das Matrix-Design aktiv ist.
   - Timer `saveTimer` (1200 ms, einmalig) → Speichern, entprellt über `QueueSave()` (bei Größen-/Positionsänderung, Tasten, Reglern, Designwechsel).
   - Timer `fanAnimTimer` (15 ms) → `FanAnimTick()`; läuft nur, solange ein Regler zwischen Auto und Fest animiert (siehe 4.4). Vor dem Start wird `Fan.Anim` passend zum geladenen Zustand gesetzt (Auto = 1, fest = 0), damit beim Start nichts animiert.
   - Timer `animTimer` (30 ms) → Neuzeichnen während der MAX-Animation (stoppt nach 2200 ms).
   - Timer `vidTimer` (33 ms) → Video-Bildfolge.
5. Event `Shown`: war beim letzten Mal Vollbild aktiv → `ToggleFull()`; dann `LoadMedia()` (Frames + WAV aus den Ressourcen) und `AskPawnIo()`.
3b. Fehlerbehandlung registrieren: `AppDomain.UnhandledException`, `ProcessExit`, `Application.SetUnhandledExceptionMode(CatchException)` und `Application.ThreadException` (statt des Windows-Absturzdialogs wird protokolliert, die UI-Fehler führen nicht mehr zu einem blockierenden Dialog).
6. `FormClosing`: `SaveNow()`, `ReleaseAll()` (beide Lüfter zurück auf Auto), Video/Sound stoppen. `FormClosed`: Timer stoppen, Caches freigeben, `computer.Close()`.

## 4. MainForm im Detail

### 4.1 Sensoren (`UpdateReadings`, `Pick`, `Ok`, `AllSensors`, `UpdateAll`)
- Jede Sekunde wird die ganze Hardware-Hierarchie aktualisiert.
- **CPU-Sensoren** kommen von der ersten Hardware vom Typ `Cpu`.
- **GPU**: Unter allen GPUs (`GpuNvidia`/`GpuAmd`/`GpuIntel`) wird die mit der **höchsten Leistungsaufnahme** gewählt (wählt in der Praxis die dedizierte Karte vor der iGPU).
- `Pick(liste, typ, namen[], fallbackMax)`: nimmt den ersten Sensor, dessen Name (case-insensitive) in der Namensliste vorkommt und dessen Wert gültig ist (`> 0`, nicht NaN). Mit `fallbackMax = true` nimmt es sonst den größten gültigen Wert des Typs (ohne Sensoren mit „Distance“ oder „TjMax“ im Namen).
- Verwendete Namen: GPU-Temp „GPU Core“; Hotspot „GPU Hot Spot“/„GPU Hotspot“; GPU-Watt „GPU Package“/„GPU Power“/„GPU Core“; CPU-Temp „CPU Package“, „Core (Tctl/Tdie)“, „Core (Tdie)“, „CPU (Tctl/Tdie)“, „Tctl/Tdie“; CPU-Watt „CPU Package“/„Package“.
- Pro Lüfter wird außerdem `AutoNow` (erster Control-Sensorwert) und `RpmNow` (größter gültiger RPM-Wert aus `RpmSensors`, sonst aus `RpmFallback`) gelesen.
- Statuszeile (`status`): ohne Admin → Hinweis auf Admin-Rechte; mit Admin, aber ohne CPU-Werte → Hinweis auf PawnIO (Taste I) oder Taste D. In den ersten 12 Sekunden nach dem Start wird der Status unterdrückt (`started`).

### 4.2 Anzeige
- Sechs Zeilen: GPU-Temp (mit Hotspot in Klammern, schräg), CPU-Temp, GPU-Watt, CPU-Watt, **Total** (GPU+CPU), **Gesamtverbrauch geschätzt**.
- **Schätzformel:** `Total + (30 W + 14 % von Total)`. Konstanten `BASE_EXTRA = 30` und `EXTRA_PCT = 0.14`. Der Aufschlag steht in Klammern daneben (klein, schräg, grau). Es gibt **keine** Idle-/Last-Erkennung: Der Aufschlag wächst stufenlos mit der gemessenen Leistung (Total 35 W → +35 W, 100 W → +44 W, 250 W → +65 W, 350 W → +79 W). Herleitung und verworfene Idle/Last-Ideen: Abschnitt 12.1 und 12.2.
- **Fußzeile:** Unter den Reglern steht mittig (unter der Balkenmitte) kursiv „PC Überblick von MaoiBaoi“ (`AppAuthor`): Consolas fett, Höhe 0,15 × Zeilenhöhe, `DrawSkew` mit `SKEW 0.2`, Brush `FadeBrush(...)` (seit Runde 8): Farbe wie bisher (oben weiß, unten Eisblau, im Matrix Design hellgrün nach `MX_GREEN`), dazu der Verlaufseffekt der Beschriftungen in Richtung „oben hell, unten auslaufend zu transparent“ (Alpha 255/255/55/0 bei 0/0,4/0,78/1); der Schatten läuft genauso aus. Seit Runde 5 sitzt sie **ganz unten am Fensterrand** (`footTop = max(altePosition, Höhe − num4 − Schrifthöhe − 1 px)`; bei Status/Warnmeldung steht sie direkt darüber). Das Layout reserviert weiter 0,22 Zeilenhöhen (`num16`), damit nichts überlappt. Warn- und Statusmeldungen erscheinen weiter ganz unten (`num4`).
- **Temperaturzahlen** (Zeilen 1 und 2, `TempBrush` + `TempTint`): senkrechter Farbverlauf, oben weiß, unten Tönung. Die Tönung läuft stufenlos von Eisblau (`ICE`, ≤ 40 °C) nach Feuerrot (`HOTC`, **ab 70 °C voll feuerrot**, `TempTint`: `(t - 40) / 30`). Bei der GPU gilt für die Hauptzahl `GpuTemp`, für die Hotspot-Klammer `GpuHot`. Ohne Wert grau (`DIM`).
- **Wattzahlen** (Zeilen 3–6, `WattBrush`): alle weiß, statischer senkrechter Silberverlauf. Weiß oben, Hellsilber bis ca. 40 % der Zeilenhöhe, danach auslaufend zu transparent (Alpha 55 bei 78 %, 0 am unteren Rand). Der Verlauf ist unabhängig vom Wert. Die früheren Farben (Blau für Total, Orange für Gesamtverbrauch) gibt es nicht mehr.
- Schrift: **Consolas fett**, Basis 100 px. In `OnPaint` wird ein Skalierungsfaktor (`num20`) aus Fensterbreite und -höhe berechnet, sodass alles proportional zur Fenstergröße wächst/schrumpft (Minimum 0,06).
- Hintergrund: Standard-Design = `DrawBackground` (dunkler Verlauf plus bläulicher Lichtfleck oberhalb der unteren Linie, gecacht in `bgCache`); Matrix-Design = `DrawMatrix` (siehe 4.3). Dazu zwei Trennlinien (`DrawFadeLine`).
- Hilfsfunktionen: `DrawSkew` (schräge Schrift mit `SKEW = 0.2`), `Grad`/`GradColor`/`ToGrey` (Slider-Farbverlauf), `DrawTrackText` (Text im Regler), `DrawBlurredTrack` + `BoxBlurBitmap`/`BlurPass` (weichgezeichneter Slider im Automodus, per `Fan.Blur`/`BlurKey` gecacht), `DrawLock` (Schloss = Automatik aktiv, mit Wattverlauf), `Ease`/`StartFanAnim`/`FanAnimTick` (Fade der Reglertexte), `RoundRect`, `DrawMax`, `DrawVideo`, `DrawInfoIcon`, `DrawInfoShine`.

### 4.3 Design-Umschalter und Matrix-Design
- Unten rechts über der unteren Linie stehen zwei Textknöpfe **„Standard“** und **„Matrix“** (`designBtn[0]`/`designBtn[1]`, `SetDesign`). Das aktive Design ist unterstrichen. Seit Runde 5 haben beide Beschriftungen einen zweistufigen Schlagschatten. Die Wahl wird gespeichert (`Design=0/1`).
- Matrix: Zeichenregen aus Spalten (`mHead`, `mSpeed`, `mLen`, `mChars`). Zeichen: Halbbreit-Katakana, Ziffern, Buchstaben, Sonderzeichen, Griechisch, Kyrillisch (`RandChar`). Schrift „MS Gothic“. Zellgröße 14×17 px (× DPI-Faktor). `UpdateMatrix` (alle 40 ms) bewegt die Köpfe, setzt Spalten zurück und tauscht zufällig Zeichen.
- `DrawMatrix` zeichnet alles scharf in die Bitmap `mBmp`, verkleinert sie auf `1/mDiv` (`mSmall`, HighQualityBilinear) und zeichnet sie wieder hochskaliert (HighQualityBicubic, Kanten per `TileFlipXY`). **Nur dieses weichgezeichnete Bild kommt auf den Schirm** (über den gesamten Hintergrund), mit 1,5-facher Helligkeit (ColorMatrix), damit es durch die Unschärfe nicht zu dunkel wird. Darüber ein schwarzer Schleier (Alpha 80).
- **Blur-Stärke:** Konstanten `MATRIX_BLUR = 0.25f` (Anteil der Zeichenhöhe) und `MATRIX_BLUR_SCALE = 0.9f` (Runde 3: 10 % weniger Blur). `MatrixDiv()` liefert jetzt einen **float**: `max(2, round(mCh × MATRIX_BLUR) × MATRIX_BLUR_SCALE)`, bei 100 % DPI also 3,6 (vorher 4). `mSmall` hat die Größe `Fenster / mDiv` (auf int abgeschnitten). Größerer Wert = stärker weichgezeichnet. Ändert sich der Teiler (DPI-Wechsel), werden `mBmp`/`mSmall` neu angelegt.
- Die Beschriftungen „Standard“ und „Matrix“ sitzen 2 px weiter links als zuvor (`- 2f` bei `num37` in `OnPaint`).

### 4.4 Regler (Slider): Optik, Farbverlauf, Texte
- Konstanten `C_RED` (255,36,0 = Feuerrot) und `C_BLUE` (80,215,255 = Eisblau).
- **Farbverlauf:** `GradColor(t)` = lineare RGB-Mischung `Lerp(C_RED, C_BLUE, t)`; `Grad()` baut daraus einen zweistufigen `LinearGradientBrush` über die Balkenlänge. Keine harten Übergänge, keine Zusatzfarben. Die Mitte wird wegen des hohen Grünanteils von Eisblau ein neutrales Rosa-Grau, **kein Lila**. Nicht gamma-korrekt mischen (das ergäbe Lavendel). Die früheren Lösungen (tanh-Kurve `Sigma`, dunkler Mittelton `C_MID`, `KnobBrush` mit harter Kante bei 50 %) wurden in Runde 3 auf Wunsch des Besitzers entfernt.
- **Kein senkrechter Verlaufseffekt** auf den Balken (in Runde 8 eingebaut, in Runde 9 wieder entfernt). Länge: Ausgangslänge × `TRACK_LEN_SCALE` (0,75), mittig um die Ausgangslänge.
- **Fester Wert:** Balken in voller Farbe bis zum Regler (der Verlauf liegt über dem ganzen Balken und wird abgeschnitten), Rest abgedunkelt (Alpha 88). **Automodus:** derselbe Verlauf in Graustufen (`ToGrey`), weichgezeichnet (`DrawBlurredTrack`, `BoxBlurBitmap`/`BlurPass`, Cache in `Fan.Blur`/`BlurKey`).
- **Reglerkreis:** einfarbig `GradColor(Prozent / 100)` (0 % Feuerrot, 100 % Eisblau, dazwischen stufenlos), weißer Rand, Schatten; Radius `num14 × Skalierung × 0,75 × 0,8` (Runde 4: Kreis 20 % kleiner; das Schloss im Automodus nutzt denselben Radius und schrumpft mit). **Schatten (Runde 5):** `DrawKnobShadow`, vier weiche Ellipsen (Alpha 28 bis 60, Zuschlag 55/35/18/0 % des Radius), leicht nach unten versetzt; in beiden Designs gleich. **Glanz (Runde 5):** wie beim Info i folgt ein Glanz der Maus (`DrawInfoShine` auf `Fan.Knob`), solange die Maus auf dem Kreis ist (`hoverKnob`, `knobMouse`) oder der Regler gezogen wird (`dragging`). Im Automodus erscheint statt des Kreises das **Schloss** (`DrawLock`, Wattverlauf), bei Mausüberfahrt ein weißer Kreis.
- **Maße:** Balkendicke `num52 = max(3.4 × 1,2 × 1,15; Zeilenhöhe × 0,17 × 0,85 × 0,85 × 1,2 × 1,15)` (Runde 8: Balken nochmal 15 % dicker) (Runde 4: Balken 20 % dicker, die Schrift im Balken folgt der Dicke und wächst mit). Die Layout-Abstände (`Track`, `Hit`) rechnen mit dem ursprünglichen Radius `num14`.
- **Länge und Position (Runde 8):** Der Balken beginnt `TRACK_GAP_LEFT × Skalierung` (54) rechts vom Ende des Labels („CPU Lüfter:“, `labelEnd`) und endet `TRACK_GAP_RIGHT × Skalierung` (66) links vom Auto Button: `trackLeft = labelEnd + TRACK_GAP_LEFT × num20`, `trackLen = Btn.Left − TRACK_GAP_RIGHT × num20 − trackLeft`. Maße aus dem Screenshot des Besitzers (rote Striche). Er **wächst nicht** im Automodus (Runde 3). Früher: Länge aus `num51 − num54`, mittig zwischen Label und Button. Die Größen `num11`/`num12` (alte Breiten von Prozent- und RPM-Text) bestimmen weiter die Balkenlänge, auch wenn die Texte nicht mehr daneben stehen.
- **Texte im Regler** (`DrawTrackText`, Runde 3): links die Drehzahl (`"<n> RPM"`), rechts der Prozentwert, jeweils in der Mitte der linken bzw. rechten Balkenhälfte (25 % und 75 % der Balkenlänge). Consolas fett, Schrifthöhe = 1,05 × Balkendicke (unter 6,5 px wird nichts gezeichnet), weiß mit **dreistufigem weichem Schlagschatten** (Versatz 1,7/1,1/0,6 × 7 % der Schrifthöhe, Alpha 40/70/100). **Senkrecht exakt mittig im Balken (Runde 5):** `DrawTrackText` zeichnet mit `LineAlignment.Near` und legt die Grundlinie über `GetCellAscent` so, dass die Ziffernhöhe (0,64 × Schrifthöhe) auf der Balkenmitte liegt (vorher mit `Center` rund 0,08 Schrifthöhen zu hoch). Gezeichnet **vor** dem Reglerkreis, der Kreis kann sie also verdecken. „n/a“ steht mittig im Balken (grau, ohne Animation). Die Drehzahl erscheint nur bei festem Wert und gültigem RPM-Sensor.
- **Ausblenden (Fade):** `Fan.Anim` (0 = fester Wert, 1 = Auto) läuft zeitbasiert in 0,3 s (`FanAnimTick`, `StartFanAnim()` in `Release()` und `SetFromMouse()`, Smoothstep `Ease`). Die Texttransparenz ist `1 − Ease(Anim)`. Beim Ausblenden ist `Pct` schon null, deshalb zeichnet der Code die zuletzt gezeigten Texte aus `Fan.LastPctText`/`LastRpmText` weiter. Vor dem Start wird `Anim` passend zum geladenen Zustand gesetzt (Auto = 1, fest = 0).
- **Auto Button:** Füllung **aktiv** (fester Wert gesetzt, Button hell mit Rand `DIM`) = RGB (35,55,73); **inaktiv** (Automatik läuft, auch bei „n/a“) = RGB (21,24,32) (seit Runde 8, Farbe aus dem Screenshot des Besitzers; vorher (20,23,30)). Die Schrift hat den Wattverlauf (`WattBrush(top, h, dim)`): weiß bei aktiv, gräulich bei inaktiv. Ursprünglich grün, Runde 3 laut Mustern des Besitzers geändert.
- **Schloss** (`DrawLock`): gleicher Silberverlauf wie die Wattzahlen, auf die Schlosshöhe abgebildet.

### 4.5 Lüftersteuerung
- `DiscoverFans()`: Sucht alle Sensoren vom Typ `Control` mit `Control != null`.
  - GPU (NVIDIA/AMD): Sensoren der **ersten** gefundenen GPU gehen an `gpuFan`.
  - **Alle steuerbaren Mainboard-Lüfter** (Hardwaretyp `Motherboard`, inkl. Sub-Hardware wie Super-I/O-Chip) hängen an **einem** Regler `cpuFan` („CPU Lüfter“). Das ist Absicht (siehe Abschnitt 7).
  - `AddRpmSensors` ordnet jedem Steuer-Sensor einen RPM-Sensor zu (gleicher Name, sonst gleiche Endnummer). Nicht zugeordnete RPM-Sensoren der Hardware landen in `RpmFallback`.
- `Fan.Available` = hat mindestens einen Sensor. Sonst zeigt der Regler „n/a“.
- `SetFromMouse` → rechnet die Mausposition auf 0–100 % um → `ApplyFan` → `sensor.Control.SetSoftware(clamp(Min..MaxSoftwareValue))`. `Applied` verhindert doppeltes Setzen desselben Werts.
- `Release(f)`: `Pct = null`, `Applied = -1`, `SetDefault()` auf allen Sensoren → zurück auf **Auto**. `ReleaseAll()` läuft in `FormClosing`.
- **RPM-Anzeige:** (Runde 5: Ist bei festem Wert nach 3 Messungen keine Drehzahl da, schreibt `UpdateReadings` einmal die Sensoren samt Werten ins `log.txt`, Zähler `Fan.NoRpm`. Grund: Beim CPU Lüfter fehlte die Anzeige, Ursache aus dem Code allein nicht sicher bestimmbar.) Die Drehzahl steht **nur bei festem Wert** in der linken Balkenhälfte (siehe 4.4), nie im Automodus (dort blendet sie aus). Vorher stand im Automodus teils „0 RPM“ (Bug, behoben). Liefert der Sensor auch bei festem Wert keine Zahl, wird nichts gezeichnet.
- **Schutzfunktion** `Protect()` (jede Sekunde): Ist ein Regler gesetzt (< 100 %) und die Temperatur zu hoch, wird er automatisch auf Auto zurückgesetzt und eine Warnung für 8 s gezeigt (`Alert`). Schwellen: CPU ≥ 85 °C; GPU ≥ 80 °C **oder** Hotspot ≥ 90 °C. Bei 100 % greift sie nicht. (Diese Schwellen sind unabhängig von der Farbskala der Zahlen.)
- Bei Erreichen von 100 % (von < 100 % kommend) startet die **MAX-Animation**: `maxStart`, `animTimer`, `PlayVideo()`.

### 4.6 Video/Sound
- `LoadMedia()` lädt `f00.png`, `f01.png`, … aus den Ressourcen, bis keine mehr gefunden wird (derzeit 39 Frames), und wandelt sie in `Format32bppPArgb` (schnelles Zeichnen). Danach `max.wav` in einen `SoundPlayer` (aus `MemoryStream`).
- `PlayVideo()` startet Sound + `vidTimer`. Der aktuelle Frame ergibt sich aus der **verstrichenen Zeit × 14,985 FPS** (`CurrentFrame()`), nicht aus einem Zähler, damit die Wiedergabe bei langsamem Zeichnen synchron bleibt. Ende = Frame-Index ≥ Anzahl Frames.
- Fehlen Frames, wird „Video nicht abspielbar“ als Alert gezeigt; ohne WAV läuft das Video stumm.

### 4.7 Tastatur und Maus
| Eingabe | Wirkung |
|---|---|
| Linksklick/Ziehen auf Regler | festen Prozentwert setzen |
| Rechtsklick auf Regler oder Klick auf „Auto“-Knopf | zurück auf Automatik |
| Klick auf „Standard“ / „Matrix“ | Design wechseln |
| Klick auf „i“ | Info-Fenster |
| Maus über „i“ | Knopf wird heller, ein **Glanzeffekt folgt der Maus** (siehe 4.8) |
| Doppelklick auf freie Fläche / **F11** | Vollbild an/aus (`ToggleFull`: rahmenlos + maximiert) |
| **Esc** | Vollbild verlassen |
| **T** | „Immer im Vordergrund“ umschalten (`TopMost`) |
| **D** | Sensorliste (alle Hardware, Sensoren, Steuerbarkeit) als `PCUeberblick_Sensoren.txt` auf den Desktop schreiben (Debug-Hilfe) |
| **I** | PawnIO per winget installieren |

Die Klickflächen sind die Rechtecke `Track`, `Hit`, `Btn` in `Fan`, sowie `infoBtn` und `designBtn[]` in `MainForm`. Sie werden **in `OnPaint` gesetzt**, die Maus-Handler lesen sie nur. Der Mauszeiger wird über diesen Flächen zur Hand.

### 4.8 Info-Knopf und Glanzeffekt
- `infoBtn` ist ein Kreis rechts neben der Zeile „GPU:“ (Wattzeile), Symbol aus der Ressource `info.png` (`DrawInfoIcon`, per ColorMatrix eingefärbt).
- Die Hover-Hervorhebung wurde in Runde 3 um **30 % reduziert**: Alpha-Stufen des Glanzes 0/14/60/133 (vorher 0/20/85/190), Füllung beim Hover (61,65,76) statt (70,74,86), Rand und Symbol hellen nur zu 70 % auf (`Lerp(…, 0.7f)`).
- `hoverInfo` ist wahr, solange die Maus im Rechteck des Knopfs ist. `infoMouse` speichert die letzte Mausposition. Der Hover-Test arbeitet mit dem umgebenden Rechteck, der Glanz begrenzt die Mausposition aber auf den Kreis.
- `DrawInfoShine(g, btn, mouse)` (nur bei Hover, nach dem Symbol gezeichnet): ein weicher weißer Radialverlauf (`PathGradientBrush`, Radius ≈ 0,95 × Knopfbreite) sitzt an der Mausposition, auf den Kreis zugeschnitten (Clip), plus derselbe Verlauf als Randlicht (Pen) am Rand des Kreises, der der Maus am nächsten ist. Bewegt sich die Maus, wandert der Glanz mit.
- In `OnMouseMove` wird bei Bewegung über dem Knopf nur das Rechteck des Knopfs neu gezeichnet (`Invalidate(Rectangle)`), beim Betreten/Verlassen das ganze Fenster. `MouseLeave` setzt `hoverInfo` zurück.

### 4.9 Einstellungen (`Settings.cs`)
- Datei `%APPDATA%\PCUeberblick\settings.ini` (einfaches `Schlüssel=Wert`): `Save`, `X`, `Y`, `W`, `H`, `Max`, `Full`, `Top`, `Design`, `GpuFan`, `CpuFan` (`auto` oder 0–100), `Lang` (`de` oder `en`, immer geschrieben).
- **Neu (Runde 4), Experten-Optionen, werden immer mitgeschrieben und auch bei `Save=0` gelesen:** `PollMs` (Abfrageintervall in ms, 500 bis 10000, Standard 1000), `Gpu` (`0` = GPU gar nicht abfragen, nur zum Eingrenzen von Problemen), `Mobo` (`0` = Mainboard gar nicht abfragen, dann kein CPU Lüfter), `GameMatrixFps` (Matrix Bilder pro Sekunde bei Vollbildspielen, `0` = angehalten, Standard 0, höchstens 25).
- `Save=0` schaltet das Speichern ab (dann steht nur diese Zeile in der Datei, nichts wird mehr angewendet). Umschalter: Häkchen im Info-Fenster.
- `SaveNow()` schreibt Fensterbounds (im Normalzustand `Bounds`, sonst `RestoreBounds`), Vollbild, Maximiert, TopMost, Design und beide Lüfterwerte. Aufgerufen über `saveTimer`, beim Schließen und nach dem Info-Fenster.
- Beim Start stellt `RestoreFans()` gespeicherte feste Werte wieder her. Steht ein Lüfter auf Auto, bleibt er auf Auto.

### 4.10 Autostart (`Autostart.cs`)
- Häkchen „Mit Windows starten“ im Info-Fenster. Legt eine Aufgabe `PCUeberblick` in der Windows-Aufgabenplanung an (`schtasks /Create /XML … /F`): Auslöser = Anmeldung des aktuellen Benutzers mit 10 s Verzögerung, `RunLevel = HighestAvailable` (Administrator ohne UAC-Nachfrage), kein Zeitlimit. Abmelden: `schtasks /Delete`. Status: `schtasks /Query`.
- Wird die EXE verschoben, muss das Häkchen einmal aus- und wieder eingeschaltet werden (Pfad steht in der Aufgabe).
- Fehler kommen als Alert „Autostart nicht geändert: …“.

### 4.11 PawnIO
- `AskPawnIo()` (nach `Shown`): Fehlt PawnIO und existiert nicht `%APPDATA%\PCUeberblick\nopawnio.txt`, erscheint ein Ja/Nein-Dialog. „Nein“ legt die Datei an, danach wird nicht mehr gefragt.
- `InstallPawnIo()` startet `cmd.exe` mit `winget install --id namazso.PawnIO -e --accept-source-agreements --accept-package-agreements`, anschließend Hinweis „Bitte PC-Ueberblick jetzt neu starten“ und `pause`.
- Nach der Installation muss das Programm **neu gestartet** werden, weil `pawnOk` und der LibreHardwareMonitor-`Computer` beim Start gebildet werden.
- Dateien, die das Programm in `%APPDATA%\PCUeberblick` anlegt: `settings.ini` und (optional) `nopawnio.txt`.

### 4.12 Leistung, Spielmodus und Stabilität (Runde 4)

Anlass: Der Besitzer meldete spürbare FPS Einbrüche in Spielen, solange das Programm läuft, und regelmäßige Spielabstürze (teils ohne Meldung, teils „Out of video memory“, teils andere Meldungen). **Die Ursache ist nicht bewiesen** (kein Zugriff auf seinen PC, siehe 12.7). Die Änderungen verkleinern den Einfluss des Programms an allen Stellen, die sich im Code belegen ließen, und liefern Protokolldaten für die nächste Runde.

**Spielmodus (`UpdateGameMode`, `ApplyMatrixRate`):**
- Jede Messung (alle `PollMs`) fragt `SHQueryUserNotificationState` ab. Die Zustände 2 (Vollbildanwendung), 3 (D3D Vollbild) und 4 (Präsentationsmodus) gelten als „Spiel läuft“, aber **nur, wenn nicht eines der eigenen Fenster aktiv ist** (`Form.ActiveForm == null`). Eigener Vollbildmodus (F11) löst ihn also nicht aus.
- (Seit Runde 7 nur mit dem Häkchen „Matrix bei Vollbildspielen anhalten“, Standard ist weiterlaufen.) Im Spielmodus wird der Matrix Timer **angehalten** oder auf `GameMatrixFps` gedrosselt (Intervall `1000 / fps`, mindestens 40 ms). Das letzte Bild bleibt stehen und wird weiter einmal pro Messung neu gezeichnet (Werte aktuell). Beim Ende wird der Timer wieder auf 40 ms gesetzt.
- `ApplyMatrixRate()` hält den Matrix Timer außerdem an, wenn das Fenster minimiert ist oder das Standard Design aktiv ist. Aufgerufen bei Design Wechsel, Start, Zustandswechsel des Fensters (`lastWs`) und Spielmodus Wechsel. Das Standard Design zeichnet ohnehin nur einmal pro Messung.

**Weniger Zugriffe auf die Hardware:**
- Das **Mainboard (Super I/O) wird nur abgefragt, solange der CPU Lüfter auf einem festen Wert steht** (nur dann wird die Drehzahl angezeigt). Im Automodus entfällt dieser Zugriff komplett. Geprüft in der Bibliothek: `SuperIOHardware.Update` schreibt keine Lüfterwerte, die Steuerung läuft nur über `SetSoftware`/`SetDefault`. Folge: Sensorwerte des Mainboards sind im Automodus veraltet. Taste D fragt deshalb vor dem Schreiben einmal alles frisch ab.
- Die Prozesspriorität ist „Unter normal“ (`Program.LowerPriority`). Nachteil: Bei komplett ausgelasteter CPU kann die Anzeige einige Sekunden hinterherhinken.
- Die Matrix verwendet einen Zeichenketten Cache (`GlyphStr`) statt `char.ToString()` pro Zeichen (weniger Müll, Bild identisch). Am Bild selbst (Weichzeichnung, Helligkeit, Schleier) wurde **nichts** geändert.

**Schutz und Absturzsicherheit für die Lüfter:**
- `ProtectNoTemp`: Steht ein Regler auf einem festen Wert unter 100 % und liefert der zugehörige Sensor 5 Messungen in Folge keine Temperatur (CPU: `CpuTemp`, GPU: `GpuTemp` oder `GpuHot`), wird der Regler auf Auto gestellt und eine Meldung gezeigt (`Fan.NoTemp` zählt mit). Vorher blieb der Regler in diesem Fall ungeschützt.
- `Program.OnFatal`, `ProcessExit` und `MainForm.EmergencyRelease()` stellen beide Lüfter auf Auto (`HardRelease`: nur `SetDefault`, keine UI), wenn das Programm unerwartet endet. Läuft in einem eigenen Thread mit 2,5 s Zeitlimit, damit der Absturz nicht hängen bleibt. Nach normalem Schließen (`closedNormally`) macht `EmergencyRelease` nichts mehr.
- Nicht abgefangene Fehler im UI Thread führen nicht mehr zum Windows Dialog „Unbehandelte Ausnahme“, sondern nur zu einem Logeintrag (die ersten 20, danach jeder hundertste).
- **Nicht geändert:** Die Schutzschwellen (CPU 85 °C, GPU 80 °C, Hotspot 90 °C) und das Wiederherstellen gespeicherter fester Lüfterwerte beim Start (`RestoreFans`). Beides ist ein möglicher Beitrag zu Instabilität (siehe 12.7), wurde aber nicht ohne Rücksprache mit dem Besitzer verändert.

**Protokoll (`Log.cs`, `%APPDATA%\PCUeberblick\log.txt`):**
- Wird bei 256 KB halbiert. Einträge: Start (Version, Admin, PawnIO, Windows Version, Dauer von `computer.Open()`, Optionen), alle erkannten Hardwareteile, Lüfterzustand beim Start, Meldungen (`Alert`, entprellt: gleiche Meldung höchstens alle 5 s), Spielmodus Wechsel, Lesefehler, „Langsame Abfrage: <Hardware> <ms>“ (ab 250 ms, höchstens alle 30 s), Abstürze, Beenden.
- Zweck: Beim nächsten Spielabsturz die Datei prüfen. Steht kurz vorher „Langsame Abfrage“ oder ein Fehler, ist das ein Hinweis auf die Sensorabfrage. Fehlt jeder Eintrag, spricht das eher gegen das Programm.

## 5. InfoForm
Modaler Dialog mit Rich-Text-Box (Überschriften via `Head()`, Absätze via `Body()`, Tasten via `Line()`), Häkchen „Mit Windows starten“ und „Einstellungen speichern“ und „Schließen“-Button. Konstruktor: `InfoForm(version, author, dpi, saveSettings, autoStart)`. Unter „Designs“ steht seit Runde 4 ein Satz, dass das Matrix Design bei Vollbildspielen anhält. Ergebnis über `SaveSettings` und `AutoStart`. `MainForm.ShowInfo()` übergibt die Werte **als Literale** `"1.0"` und `"MaoiBaoi"`, nicht die Konstanten `AppVersion`/`AppAuthor`.

**Bei Änderungen am Verhalten (Schwellen, Tasten, Formel, Design) muss der Text in `InfoForm.cs` (`BuildText`) mitgepflegt werden**, er dupliziert diese Werte als reinen Text.

**Regeln für den Infotext (Wunsch des Besitzers, Runde 3):**
- Kurz halten und nur Dinge nennen, die der Nutzer wissen will oder muss. Keine reinen Optik-Details (Farbverläufe, Kursivschrift, Glanzeffekt, Reglerfarben) und keine Herstellernamen wie „NVIDIA oder AMD“.
- **Keine Gedankenstriche und keine Bindestriche** im Text (auch nicht in Wörtern: „Auto Button“, „CPU Lüfter“, „Info i“, „Administratorrechte“). Auch der Fenstertitel („Info zu PC Überblick“) enthält keinen Strich. Das Zeichen `·` in der Versionszeile ist erlaubt.
- Überschriften: „Was zeigt das Programm?“, „Lüftersteuerung“, „Schutzfunktion“, „Tasten und Maus“, „Designs“ (nicht „Design“), „Einstellungen“, „Zusatz Infos“ (nicht „Voraussetzungen“).
- Der Absatz zu „Mit Windows starten“ steht nicht mehr im Text (das Häkchen im Fenster bleibt). Die Funktionsweise steht in 4.10.
- Die Schätzformel wird so beschrieben, wie sie im Code steht: 30 W und 14 % der Summe (keine Idle/Last-Stufen).

## 6. Build

### 6.1 Auf Windows (Normalfall)
```
cd PCUeberblick
dotnet build -c Release
```
Ausgabe: `bin\Release\net472\PCUeberblick.exe` (ca. 9 MB). Voraussetzung: .NET SDK mit C# 14 (die `.csproj` setzt `LangVersion 14.0`) + Internet für NuGet-Restore (Referenz-Assemblies).

### 6.2 Ressourcen
In der `.csproj` ist jede DLL, jedes PNG, `info.png` und `max.wav` als `<EmbeddedResource ... LogicalName="<Dateiname>">` eingetragen, plus `<None Remove=...>`. Die `LogicalName` **muss exakt dem Dateinamen entsprechen**, weil `Program.Resolve`, `LoadMedia` und `DrawInfoIcon` genau diese Namen suchen. `LibreHardwareMonitorLib` ist zusätzlich als `<Reference>` (ohne HintPath, die DLL liegt im Projektordner) eingetragen.

Neue DLL hinzufügen heißt also: Datei ins Projekt, `<None Remove>` + `<EmbeddedResource LogicalName>` eintragen. Weitere Frames: `fNN.png` lückenlos weiterzählen und eintragen.

### 6.3 Build und Compile-Prüfung unter Linux (in Runde 3 so verwendet, funktioniert)
Ohne Windows lässt sich die .exe trotzdem bauen. Das Ergebnis ist eine echte `net472`-Assembly (0 Warnungen, 0 Fehler), die unter Windows läuft; sie hat dieselbe Größe wie die vom Besitzer genutzte .exe, nur Zeitstempel/MVID unterscheiden sich. Die Datei `PCUeberblick.exe.config` muss nicht mitgeliefert werden.
```
apt-get update                       # wichtig, sonst 404 beim Paket dotnet-sdk-8.0
apt-get install -y dotnet-sdk-8.0 mono-devel
mkdir -p /tmp/emptysrc
# Ordner: <root>/Directory.Build.props (siehe unten) und <root>/proj/ (Projektordner)
cd <root>/proj
dotnet build PCUeberblick.csproj -c Release -o <Ausgabeordner> \
  -p:LangVersion=latest -p:UseWindowsForms=false \
  -p:AutomaticallyUseReferenceAssemblyPackages=false \
  -p:FrameworkPathOverride=/usr/lib/mono/4.7.2-api \
  -p:RestoreSources=/tmp/emptysrc
```
`<root>/Directory.Build.props`:
```xml
<Project>
  <ItemGroup>
    <Reference Include="System.Windows.Forms" />
    <Reference Include="System.Drawing" />
    <ReferencePath Include="$(MSBuildThisFileDirectory)proj/*.dll" />
  </ItemGroup>
</Project>
```
Der SDK 8 kennt `LangVersion 14.0` nicht, deshalb `-p:LangVersion=latest`; die `.csproj` bleibt unverändert. Die `ReferencePath`-Zeile ist nötig, damit `LibreHardwareMonitorLib` gefunden wird. Die Referenz-Assemblies kommen von Mono statt von Microsoft.

**Testen:** Die echte .exe lässt sich unter Mono **nicht** starten (Mono kennt `Control.DeviceDpi` nicht, außerdem Windows-Treiber und Admin). Hilfsweise lässt sich das Zeichnen prüfen, ohne Hardware: Kopie des Projekts anlegen, `DeviceDpi` durch `1f` ersetzen, `computer.Open();` durch `throw new Exception("test");` ersetzen (landet im `catch`), eine Testmethode in `MainForm` ergänzen, die `gpuFan.Sensors.Add(null)` (damit `Available` wahr ist), `Pct`, `Anim`, `RpmNow` und `r.*` mit Beispielwerten füllt und `OnPaint(new PaintEventArgs(g, rect))` in eine `Bitmap` zeichnet; Aufruf mit `xvfb-run -a mono …`. Die Schrift sieht dort anders aus (kein Consolas), Positionen und Layout stimmen aber. Vorsicht: `pkill -f PCUeberblick` beendet auch die eigene Shell (der Suchtext steht im Kommandozeilentext), stattdessen `pkill -x mono`.

**Selbsttests (Runde 4):** Dieselbe Messkopie lässt sich um eine `partial`-Datei erweitern (`internal class MainForm` in der Kopie zu `internal partial class MainForm` ändern, `DeviceDpi` durch eine Eigenschaft `DevDpi` ersetzen, `computer.Open();` durch `throw`). Darin lassen sich `ProtectNoTemp`, `ApplyMatrixRate`, `Settings.Load/Save` und `Log.Write` direkt aufrufen, und zwar ohne Hardware. Messung der Zeichenzeit: `OnPaint` in eine `Bitmap` zeichnen, `/proc/self/status` (VmRSS) vor und nach vielen Bildern vergleichen (Leckprüfung). Unter Mono/libgdiplus sind die Zeiten **nicht auf Windows übertragbar** (dort dominiert das Verkleinern des Matrix Bildes, unter Windows GDI+ wahrscheinlich nicht), nur Vergleiche und Speicherverlauf taugen.

**Windows-Falle (wichtig):** Mono kennt einzelne Methoden, die es im Windows-.NET-Framework 4.x nicht gibt. Der Code kompiliert, stürzt aber unter Windows mit `MissingMethodException` ab (so geschehen mit `String.Split(char, StringSplitOptions)` in einem frühen Build, der mit dem Mono-Compiler `mcs` gegen Monos eigenes `mscorlib` gebaut wurde). Mit Roslyn gegen `/usr/lib/mono/4.7.2-api` bindet `Split('\n')` an `Split(char[])` und ist sicher; geprüft per Reflection auf den IL-Code von `OnPaint`. Im Zweifel `Split(new[] { '\n' })`, kein `Contains(char)`, `Join(char, …)`, `Math.Clamp`, `Path.Join` usw. verwenden.

## 7. Entscheidungen des Besitzers (bitte beibehalten)

- Alle steuerbaren Mainboard-Lüfter hängen bewusst am **einen** „CPU Lüfter“-Regler.
- Keine Anzeige von Auslastung, RAM, Festplatten oder Netzwerk. Die Drehzahl (RPM) wird **nur bei festem Reglerwert** gezeigt, **nie im Automodus**.
- Speichern von Fenstergröße/Lüfterwerten ist umgesetzt (Abschnitt 4.9) und abschaltbar.
- Versionszählung: aktuell 1.0. Bei einer neuen Version `AppVersion` (`MainForm.cs`), das Literal `"1.0"` in `ShowInfo()` **und** `AssemblyInfo.cs` (drei Einträge) anpassen.
- Temperaturzahlen: Feuerrot ist ab **70 °C** voll erreicht. Wattzahlen: weiß mit statischem Silber-zu-transparent-Verlauf.
- Regler: Farbskala Feuerrot bis Eisblau, Balken schlank, Kreis klein (Maße in 4.4).
- Matrix-Design: gleichmäßig weichgezeichnet über den gesamten Hintergrund (Stärke `MATRIX_BLUR`).
- Schätzwert: stufenlose Formel (30 W + 14 %), **keine** Idle/Last-Erkennung (Abschnitt 12.2).
- Regler: schlichter Verlauf Feuerrot nach Eisblau ohne harte Kante und ohne weiße/dunkle Mitte; Balken wächst nicht im Automodus; RPM und Prozent stehen im Balken (Mitte linke/rechte Hälfte) und blenden im Automodus aus.
- Auto Button: aktiv (30,47,62), inaktiv (21,24,32).
- Fußzeile „PC Überblick von MaoiBaoi“ unter den Reglern, kursiv mit Temperatur-Verlauf.
- Infotext: kurz, ohne Striche, ohne Optik-Details (Abschnitt 5).
- Beschriftungen mit Doppelpunkt („GPU:“, „Total:“, „GPU Lüfter:“); „Gesamtverbrauch geschätzt:“ zweizeilig und leicht kursiv (`SKEW = 0.2`).
- Keine Hinweistexte beim Start oder beim Bewegen der Regler. Es bleiben nur Schutz-Warnung (Lüfter wurde zurückgesetzt) und Video-Fehler als `Alert`; die Statuszeile erscheint erst nach 12 s.
- Aussehen und Funktionen nicht ungefragt ändern. Bei Fehlern möglichst kleine Änderungen.

## 8. Besonderheiten des Codes (wichtig für Agenten)

- Herkunft ILSpy-Dekompilat: Viele Variablen heißen `num`, `num2`, `array`, `text3` usw. (besonders in `OnPaint`, ~330 Zeilen). Es gibt Reste wie `goto IL_00e6`, `end_IL_000a:;` und `//IL_…`-Kommentare (z. B. in `LoadMedia` und `UpdateReadings`). Funktional korrekt, aber unleserlich. Beim Refactoring zuerst sprechende Namen vergeben, Verhalten nicht ändern. Neu hinzugekommene Methoden (`DrawInfoShine`, `WattBrush`, `MatrixDiv`) folgen dem Stil der Datei (kurze `num…`-Namen), damit sie sich einfügen.
- Tote Reste (noch vorhanden): Konstante `BLUE`, die Funktion `TempColor` (wird nur noch in einem ungenutzten Array in `OnPaint` aufgerufen) und `Fan.AutoNow`. In Runde 3 entfernt: `SPLIT`, `HALF_BAND`, `C_MID`, `RPMCOL`, `Sigma`, `KnobBrush`.
- Es gibt **keine** Tests und keine Konfigurationsdatei außer der Einstellungsdatei. Schwellwerte sind Konstanten oder Literale im Code.
- Zeilenenden **CRLF**, Einrückung mit **Tabs**. Beim Bearbeiten beibehalten (beim Bearbeiten per Skript auf `\r\n` achten).
- Nach dem Build entsteht zusätzlich `PCUeberblick.exe.config` mit Binding-Redirects (mscorlib, System.Core usw.). Normalerweise nicht nötig, kann aber neben der .exe liegen.
- `catch { }` mit leerem Body ist an vielen Stellen Absicht (Hardware/Registry/Audio/Einstellungen sollen nie das Programm abstürzen lassen).

## 9. Bekannte Stolpersteine im Betrieb

- **PawnIO-Installation scheitert mit Exitcode 1072** (`ERROR_SERVICE_MARKED_FOR_DELETE`): Ein alter PawnIO-Dienst hängt in `STOP_PENDING` und ist zum Löschen markiert. `sc stop` meldet 1061, `sc delete` meldet 1072. Lösung: alle Hardware-Tools schließen, **Windows neu starten** (nicht „Herunterfahren“), prüfen mit `sc query PawnIO` (Fehler 1060 = weg), dann `winget install namazso.PawnIO`.
- Läuft das Programm ohne Admin-Rechte, fehlen CPU-Werte und Lüftersteuerung.
- Antivirus/SmartScreen kann bei einer unsignierten .exe mit Treiberzugriff warnen.
- Nicht jedes Mainboard erlaubt Lüftersteuerung. Dann zeigt der Regler „n/a“. Mit Taste **D** lässt sich prüfen, welche Sensoren steuerbar sind.
- Das Matrix-Design zeichnet alle 40 ms das ganze Fenster neu und kostet auf großen Bildschirmen (Vollbild 4K) merklich CPU.
- Autostart startet das Programm mit Pfad der EXE zum Zeitpunkt des Anlegens (siehe 4.10).
- **Spiele und Matrix:** Das Matrix Design ist der einzige Teil, der im Dauerbetrieb nennenswert Rechenleistung braucht (Messung 12.7). Bei Vollbildspielen hält es jetzt an (4.12). „Immer im Vordergrund“ (Taste T) über einem Spiel auf demselben Monitor zwingt Windows zum Zusammensetzen der Bilder und kann FPS kosten. Auf einem zweiten Monitor spielt das keine Rolle.
- **Fester Lüfterwert beim Spielen:** Ein gespeicherter fester Wert wird beim nächsten Start wieder gesetzt. Läuft dann ein Spiel mit hoher GPU Last, kann die Karte zu heiß werden (Schutz greift erst bei 80 °C/Hotspot 90 °C). Zum Eingrenzen von Spielabstürzen beide Regler auf Auto stellen.

## 10. Wo ändere ich was?

| Ziel | Ort |
|---|---|
| Schwellwerte der Schutzfunktion (85 °C, 80 °C, 90 °C) | `UpdateReadings` (Protect-Aufrufe) |
| Farbskala der Temperaturzahlen (Eisblau bis Feuerrot, 40 bis 70 °C) | `ICE`, `HOTC`, `TempTint` (Teiler `30f`), `TempBrush` |
| Verlauf der Wattzahlen (Silber, transparent unten) | `WattBrush` (Farben und Positionen `0, 0.4, 0.78, 1`) |
| Schätzformel Gesamtverbrauch | Konstanten `BASE_EXTRA`, `EXTRA_PCT`; Berechnung in `OnPaint` (Literale `30f` und `0.14f` stehen dort noch einmal) |
| Neue Messwerte | `Readings` erweitern, in `UpdateReadings` füllen, in `OnPaint` anzeigen (Zeilenanzahl 6 ist fest in mehreren Berechnungen) |
| Sensornamen anderer Hersteller | `Pick(...)`-Aufrufe in `UpdateReadings` |
| Reglerfarben | `C_RED`, `C_BLUE`, `GradColor`, `Grad` |
| Reglerkreis Schatten / Glanz | `DrawKnobShadow`, Aufrufe von `DrawInfoShine` im Block `num57` in `OnPaint`, Hover in `OnMouseMove` (`hoverKnob`) |
| Text im Regler senkrecht verschieben | `DrawTrackText` (`capH`, `top`) |
| Matrix Farben (Auto Button, Reglerkreis, Info i, Fußzeile) | Konstanten `MX_GREEN`, `MX_FILL`, `MX_FILL_DARK`, Abfragen `design == 1` in `OnPaint` |
| Programm Icon | `app.ico`, `.csproj` (`ApplicationIcon`), Konstruktoren von `MainForm` und `InfoForm` |
| Fußzeile Position | Zeile `footTop = Math.Max(...)` in `OnPaint` |
| Reglerkreis-Farbe | `GradColor(num55 / 100f)` in `OnPaint` (Block mit `num57`) |
| Fade der Reglertexte | Dauer `0.3f` in `FanAnimTick`, Kurve `Ease`, Alpha `inAlpha` in `OnPaint` |
| Texte im Regler (Größe, Position) | Block `inFont` in `OnPaint` (Faktor `1.05f`, Positionen `0.25f`/`0.75f`), `DrawTrackText` |
| Balkenlänge und Mittigkeit | `trackLen`, `trackLeft` in `OnPaint` |
| Fußzeile | Block `footFont` am Ende von `OnPaint` (Größe `0.15f`, Abstand `0.03f`), Platzreserve `0.22f` bei `num16` |
| Auto-Knopf-Farben | `color3` (Füllung aktiv/inaktiv) und Block `flag6` in `OnPaint` (Pen `DIM`, `WattBrush(..., !flag6)`), `WattBrush` (Parameter `dim`) |
| Balkenlänge (Abstand zu Label und Auto Button) | Konstanten `TRACK_GAP_LEFT`, `TRACK_GAP_RIGHT`, Zeilen `trackLeft`/`trackLen` in `OnPaint` |
| Balkenlänge gesamt | `TRACK_LEN_SCALE` |
| Verlaufseffekt Fußzeile | `FadeBrush` (Aufruf am Ende von `OnPaint`) |
| Info i Effektstärke | Konstante `INFO_FADE_STRENGTH` (1 = alter Effekt, 0,5 = halb) |
| Breite von „(+Zahl Watt)“ | Konstante `PLUS_XSCALE`, `DrawSkewX` |
| Reglermaße | Balkendicke: `num52` in `OnPaint` (`0.17f * 0.85f * 0.85f * 1.2f * 1.15f`, Mindestwert `3.4f * 1.2f * 1.15f`); Kreisradius: `num57` (`0.75f * 0.8f`) |
| RPM-Anzeige (wann sichtbar) | `flag3` und `fan.RpmNow` im Block `inFont` in `OnPaint` |
| Matrix-Weichzeichnung | Konstanten `MATRIX_BLUR` und `MATRIX_BLUR_SCALE`, `MatrixDiv()`, Helligkeitsfaktor `1.5f` und Schleier (Alpha 80) in `DrawMatrix` |
| Info-Knopf Glanz | `DrawInfoShine` (Radius `0.95f`, Alpha-Stufen `0/14/60/133`), Hover-Farben in `OnPaint` (`Lerp(…, 0.7f)`), Hover in `OnMouseMove` |
| Einstellungen | `Settings.cs` (`Load`/`Save`), `SaveNow` in `MainForm` |
| Autostart | `Autostart.cs` |
| Tasten | `OnKey` |
| Info-Text | `InfoForm.cs` (`BuildText`, Methoden `Head`/`Body`/`Line`), Regeln in Abschnitt 5 |
| Standard/Matrix Beschriftung (Position) | `num37` in `OnPaint` (Abzug `- 2f` = 2 px nach links) |
| Timer-Intervall der Messung | Option `PollMs` in `settings.ini` (Standard 1000), gesetzt im Konstruktor (`timer.Interval = cfg.PollMs`) |
| Spielmodus, Matrix Drosselung | `UpdateGameMode`, `ApplyMatrixRate`, Option `GameMatrixFps` |
| Mainboard nur bei festem CPU Lüfter abfragen | Schleife am Anfang von `UpdateReadings` (`(int)item.HardwareType == 0`) |
| Schutz ohne Temperatur | `ProtectNoTemp` (Schwelle 5 Messungen) |
| Notfall Freigabe der Lüfter | `Program.SafeRelease`, `MainForm.EmergencyRelease`, `HardRelease` |
| Protokoll | `Log.cs` (Grenze 262144 Bytes) |

> Hinweis zu `OnPaint`: Die Zeilenzahl 6 und die Textlisten (`array`, `array2`, `array3`) sind mit den Layout-Berechnungen (`num16 = num8 * 6f + ...`) verknüpft. Wer eine Zeile hinzufügt, muss alle diese Stellen anpassen.

## 11. Änderungsprotokoll

### Runde 12 (25.10.2026, Version bleibt 1.0, Bearbeiter: Claude)
1. **Ausblenden der Regler 18 % stärker** (`TRACK_FADE_STRENGTH` von 0,65 auf 0,767, in Runde 12 nachträglich nochmal 10 % stärker auf 0,844, dann 4 % zurück auf 0,81 (endgültig); der Effekt war nicht entfernt, nur schwach, Alpha am Balkenende jetzt etwa 23 % statt 35 %).
2. **Beschriftungen „GPU Lüfter:“ und „CPU Lüfter:“ nach rechts** (Richtung Regler): rechter Rand der Beschriftung sitzt `AUTO_GAP × Buttonbreite` links vom Balkenanfang, gespiegelt zum Auto Button (Variablen `lblW`, `lblX` im Block der Lüfterzeilen in `OnPaint`; nie weiter links als vorher).
3. **Auto Button aktiv** (fester Wert gesetzt, Standard Design) 20 % dunkler: (35,55,73) wird (28,44,58), danach 7 % heller auf (30,47,62) (endgültig). Matrix Design und inaktiver Zustand unverändert.
4. **Easter Egg:** `Ghstlyecft.wav` (aus der MP3 des Besitzers umgewandelt, 44,1 kHz, 16 Bit, Stereo, ca. 9 s, eingebettet) wird nach dem Start zufällig alle 60 bis 90 Minuten ohne Meldung abgespielt (`StartEgg`, `ScheduleEgg`, `EggTick`, Timer `eggTimer`). Läuft gerade die MAX Animation, wird der Ton übersprungen. `SoundPlayer` kann kein MP3, deshalb WAV. Im Info Text bewusst nicht erwähnt.
6. **Fußzeile:** Verlaufseffekt 35 % schwächer (`FOOT_FADE_STRENGTH = 0.65f` in `FadeBrush`: Alpha unten 35 % statt 0 %, Mitte angehoben). **Parallax 25 % schwächer** (`PARALLAX_SCALE = 0.75f`, Weg seitlich 0,225 und senkrecht 0,075 Schrifthöhen).
7. **Zweisprachig (Deutsch und Englisch):** neue Datei `Loc.cs` (`Loc.En`, `Loc.T(de, en)`, `Loc.Init`). Alle sichtbaren Texte in `MainForm.cs` (Statuszeile, Meldungen, PawnIO Dialog, Beschriftungen „GPU Lüfter“ / „CPU Lüfter“ über `ApplyLanguage()`, Fußzeile „von“ / „by“, „Watt“ / „W“, „Gesamtverbrauch geschätzt“ / „Estimated total power“) und der komplette Infotext in `InfoForm.cs` (`BuildText` deutsch, `BuildEn` englisch) laufen über `Loc.T`. **Umschalter:** Auswahlfeld „Deutsch / English“ oben rechts im Info Fenster (`cmbLang`), schaltet den Info Text sofort um; nach dem Schließen wirkt es im Hauptfenster. Gespeichert als `Lang=de` oder `Lang=en` in `settings.ini` (wird wie die Experten Optionen immer geschrieben). Ohne Eintrag: Systemsprache (Deutsch, sonst Englisch). Der Name „PC Überblick“ bleibt in beiden Sprachen. Das Protokoll `log.txt` und die Sensorliste (Taste D) bleiben deutsch (Entwicklerhilfen). Der Layoutbreite der Lüfterzeilen misst weiter den deutschen Text „CPU Lüfter:“, damit beide Sprachen gleich aussehen. **Regel für neue Texte:** immer `Loc.T("deutsch", "english")`, im Infotext auch englisch ohne Striche.
8. **Englisch: „Watt“ statt „W“** bei den Zahlenzeilen (nur „(+Zahl W)“ bleibt kurz). **Matrix Reglerkreis Outline 35 % heller** (`MX_KNOB_DARK` 0,5 auf 0,675).
9. **Einstellbare Farbskala der Temperaturzahlen** (GPU, CPU, Hotspot getrennt): im Info Fenster unterste Zeile, je zwei Zahlenfelder (links Eisblau ab, rechts Feuerrot ab, 0 bis 100 Grad, rechts immer größer als links) und ein Pfeil zum Zurücksetzen auf 40 und 70 (Standard, wie vorher). Gespeichert in `settings.ini` als `GpuLo`, `GpuHi`, `CpuLo`, `CpuHi`, `HotLo`, `HotHi` (immer geschrieben, auch bei `Save=0`). Code: `Settings.TempLo[]`/`TempHi[]` (Index 0 GPU, 1 CPU, 2 Hotspot), `TempTint(t, lo, hi)`, `TempBrush(..., lo, hi)`, `InfoForm` (`MakeSpin`, `FixPair`). Die Schutzschwellen (85, 80, 90 °C) und die Fußzeilenfarbe bleiben unabhängig davon.
10. **Sprachauswahl im Info Fenster** nach unten verschoben (rechts unter dem Schließen Button, über der Farbskala Zeile). **Weißer Hover Kreis im Automodus** (Standard Design) 30 % grauer: Füllung (202,202,202) statt Weiß (zuerst 178, dann 13,5 % heller); der Glanz folgt der Maus wie beim aktiven Regler und ist jetzt sichtbar (auf reinem Weiß war er unsichtbar).
11. **Hover Kreis im Automodus** hat jetzt auch im Standard Design die weiße Outline wie der aktive Kreis. **Regler Balken 5 % dicker** (`num52` mit Faktor `1.05f`, auch der Mindestwert; die Schrift im Balken wächst mit).
12. **Regler Balken nochmal 5 % dicker** (Faktor `1.05f` doppelt in `num52`). **Outline des Hover Kreises im Automodus** (Standard Design) dunkelgrau (zuletzt 34,34,38) statt weiß (Punkt 11 geändert); Matrix Design unverändert.
13. Build unter Linux ohne Warnungen und Fehler. Nicht unter Windows getestet.

### Runde 11 (25.10.2026, Version bleibt 1.0, Bearbeiter: Claude)
1. **Auto Buttons:** Der äußere linke Rand sitzt jetzt `AUTO_GAP × Buttonbreite` (0,69, aus dem Screenshot des Besitzers mit rotem Strich) rechts vom Balkenende (`fan.Btn` wird nach `fan.Track` gesetzt). Die Verbreiterung des Layouts aus Runde 10 (`AUTO_SHIFT`, `num15x`) ist entfernt; Skalierung und Mitte sind wieder wie in Runde 9. Die Balkenlänge hängt nicht mehr von der Button Lage ab.
2. **Ausblenden der Regler 35 % schwächer** (`TRACK_FADE_STRENGTH = 0.65f`: Alpha am Balkenende 35 % statt 0 %).
3. **Reglerkreis Outline im Matrix Design dunkler** (`MX_KNOB_DARK = 0.5f`, `MxDark(c, k)`), nur bei `design == 1`, auch beim Hover Kreis im Automodus.

### Runde 10 (25.10.2026, Version bleibt 1.0, Bearbeiter: Claude)
1. **Matrix Outlines** (Info i, Auto Buttons) dunkler: `MX_OUTLINE_DARK` von 0,7 auf 0,6 (0,7 war laut Besitzer optisch nicht erkennbar dunkler; 0,6 entspricht wahrnehmungsmäßig etwa 30 %).
2. **Verlauf bei „Gesamtverbrauch geschätzt:“ und „(+Zahl W)“ 40 % schwächer** (`GESAMT_FADE_STRENGTH = 0.6f`, neuer Parameter `strength` in `WattBrush`).
3. (in Runde 11 ersetzt) Auto Buttons 35 % nach rechts (`AUTO_SHIFT`). Die Layoutbreite (`num15x`) wächst um diese Strecke, damit nichts aus dem Fenster ragt (Inhalt wird minimal kleiner und leicht nach links gerückt). Der Regler behält seine Lage (`btnShift` wird bei der Balkenlänge abgezogen).
4. **Regler mit waagerechtem Ausblenden an beiden Enden**, Kurve wie `DrawFadeLine` (Positionen 0/0,03/0,08/0,16/0,84/0,92/0,97/1, Werte 0/0,12/0,4/1/1/0,4/0,12/0): `TrackFadeX`, `ApplyHorizontalFade`, `DrawFadedTrack` (Cache `Fan.Fixed`), im Automodus nach dem Weichzeichnen.

### Runde 9 (25.10.2026, Version bleibt 1.0, Bearbeiter: Claude)
1. **Regler 25 % kürzer** (`TRACK_LEN_SCALE = 0.75f`, um die bisherige Mitte; die Abstände `TRACK_GAP_*` definieren weiter die Ausgangslänge).
2. **Verlaufseffekt der Regler entfernt** (Runde 8 Punkt 8 rückgängig; `DrawFadedTrack`, `ApplyVerticalFade`, `Fan.Fixed` gelöscht).
3. **„(+Zahl W)“** statt „(+Zahl Watt)“ (nur der Aufschlag, die Zahlenzeilen behalten „Watt“).
4. **Matrix Design:** Outline von Info i und Auto Button 30 % dunkler (`MX_OUTLINE_DARK = 0.7f`, `MxDark`).

### Runde 8 (25.10.2026, Version bleibt 1.0, Bearbeiter: Claude)
1. **Verlauf der Beschriftungen zurückgedreht:** „GPU:“, „CPU:“, „Total:“, „Gesamtverbrauch geschätzt:“ und „(+Zahl Watt)“ laufen wieder oben hell, unten auslaufend (`WattBrush(..., dim: true, flip: false)`). Die Änderung aus Runde 7 Punkt 5 ist damit für diese Texte rückgängig.
2. **Info i Effekt 50 % schwächer** (`INFO_FADE_STRENGTH = 0.5f` in `DrawInfoFaded`). Die Richtung blieb wie in Runde 7 (oben transparent, unten hell), nur die Stärke wurde halbiert (Helligkeit unten 100 %, oben 50 % statt 0 %).
3. **Fußzeile „PC Überblick von MaoiBaoi“** mit dem Verlaufseffekt der Beschriftungen (oben hell, unten transparent), Farben wie vorher (`FadeBrush`).
4. **Regler 15 % dicker** (`num52` × 1,15; die Schrift im Balken wächst mit).
5. **Auto Button inaktiv** (Standard Design) = RGB (21,24,32).
6. **„(+Zahl Watt)“ 20 % schmaler** (`DrawSkewX`, `PLUS_XSCALE = 0.8`; Schräge bleibt optisch gleich).
7. **Regler länger:** Anfang und Ende wie vom Besitzer im Screenshot markiert (siehe 4.4), Länge wächst und schrumpft mit der Fenstergröße.
8. **Regler mit Verlaufseffekt** (oben voll, unten transparent), siehe 4.4.
9. **Neues Programm Icon** (`app.ico`, 9 Größen 16 bis 256 px): Entwurf des Besitzers, Schachmuster und Rand entfernt, auf die Karte zugeschnitten, Ecken transparent.
10. Build unter Linux ohne Warnungen/Fehler; Probezeichnung beider Designs unter Mono geprüft. Nicht unter Windows getestet.

### Runde 7 (25.10.2026, Version bleibt 1.0, Bearbeiter: Claude)
1. **Matrix ruckelte alle paar Sekunden.** Zwei Maßnahmen: (a) Die Hardware Abfrage (`PollHardware`) läuft jetzt in einem **Hintergrund Thread** (`ThreadPool`); nur das Auswerten (`UpdateReadings`, `FinishPoll`) und Zeichnen läuft im UI Thread. Läuft eine Abfrage noch (`pollBusy`), wird der nächste Takt übersprungen. Beim Schließen wartet `FormClosed` bis zu 2 s auf eine laufende Abfrage, bevor `computer.Close()` aufgerufen wird. (b) Der Zeichenregen verschiebt die Spalten jetzt **um Bruchteile einer Zeile** (`headFrac` in `DrawMatrix`) statt in ganzen Zeilen zu springen. Zusätzlich `GCLatencyMode.SustainedLowLatency` im Konstruktor. Nicht auf Windows getestet; falls es weiter ruckelt, `log.txt` auf „Langsame Abfrage“ prüfen.
2. **Spielmodus:** Der Zeichenregen läuft jetzt **auch bei Vollbildspielen weiter**. Neues Häkchen im Info Fenster „Matrix bei Vollbildspielen anhalten“ (`Settings.GameMatrixStop`, Schlüssel `GameMatrixStop=0/1`, wird immer geschrieben und gelesen, auch bei `Save=0`; Standard aus). Nur bei gesetztem Häkchen greift das alte Verhalten (angehalten oder auf `GameMatrixFps` gedrosselt, siehe 4.12). Das Info Fenster hat dafür eine dritte Häkchenzeile (untere Leiste 82 statt 56).
3. **Matrix Design, 20 % dunkler:** Info i (Ring, Symbol, Füllung) und die **Auto Buttons nur wenn Auto aktiv läuft (Knopf „inaktiv“)** bekommen eine schwarze Auflage mit Alpha 51 (20 %).
4. **Info i** hat den Verlauf der Beschriftungen (`DrawInfoFaded`: Ring und Symbol in ein kleines Bitmap, Alpha Rampe `InfoRamp`, gecacht in `infoFade`).
5. **Verlauf umgedreht:** „GPU:“, „CPU:“, „Total:“, „Gesamtverbrauch geschätzt:“ und „(+Zahl Watt)“ sind jetzt oben transparent und unten hell (`WattBrush(..., dim: true, flip: true)`). Auch das Info i nutzt diese Richtung. Die Zahlen selbst unverändert (oben hell, unten auslaufend).
6. **„(+Zahl Watt)“** hat denselben Verlauf (grau, `dim`).
7. **Parallax** für „PC Überblick von MaoiBaoi“: Der Text verschiebt sich leicht gegen die Mausposition (`parX`/`parY`, Zeitgeber `parTimer` 16 ms, weich nachgeführt, maximal 0,3 Schrifthöhen seitlich und 0,1 senkrecht), dazu ein Schatten, der sich weniger bewegt (Tiefe). Beim Verlassen des Fensters gleitet der Text zurück. Neugezeichnet wird nur der Bereich `footArea`; im laufenden Matrix Design entfällt das extra Neuzeichnen (das Bild wird ohnehin 25 mal pro Sekunde gezeichnet).

### Runde 6 (25.10.2026, Version bleibt 1.0, Bearbeiter: Claude)
1. **Programm Icon:** `app.ico` (9 Größen 16 bis 256 px, aus dem Entwurf des Besitzers, Schachmuster freigestellt, Ecken transparent). In der `.csproj` als `<ApplicationIcon>` (Datei Icon der .exe) **und** als `EmbeddedResource app.ico` eingetragen; `MainForm` und `InfoForm` laden es für die Fensterleiste/Taskleiste (`new Icon(Stream)`, Fehler werden verschluckt).
2. **Text im Regler** (`DrawTrackText`) wird jetzt als **Pfad** (`GraphicsPath.AddString`) gezeichnet und an den echten Glyphenkanten zentriert (Höhe der Referenz „H“, Breite des Textes). Die Metrik Variante aus Runde 5 saß im Screenshot noch 1 px zu hoch. Test: Mitte auf ca. 0,5 px genau bei Balkendicken 12 bis 20 px. Schatten dreistufig, ebenfalls als Pfad.
3. **CPU Lüfter ohne Drehzahl:** Bei festem Wert ohne Sensorwert steht jetzt „N/A RPM“ (`LastRpmText`). Ursache der fehlenden Drehzahl weiter offen, Diagnose im `log.txt` (siehe Runde 5).
4. **Beschriftungen** („GPU:“, „CPU:“, „Total:“, „Gesamtverbrauch geschätzt:“) haben den Wattverlauf in der grauen Variante (`WattBrush(..., dim: true)`): oben Grau, nach unten auslaufend zu transparent. Wunsch war „gleicher Färbeeffekt wie die Wattzahlen“; für reines Weiß statt Grau `dim: false` setzen.
5. **„Watt“ statt „W“** in `W()` und im Aufschlag „(+35 Watt)“, ebenso im Infotext. Die Layout Breite wird aus den Texten berechnet, die Schrift wird dadurch etwas kleiner.
6. **Matrix Design Farben** (`design == 1`): Konstanten `MX_GREEN` (0,235,90, wie die Beschriftung „Matrix“), `MX_FILL` (0,58,26), `MX_FILL_DARK` (0,28,13). Auto Button: grüne Outline (aktiv voll, inaktiv Alpha 150), Füllung dunkler als die Outline. Reglerkreis und Info i genauso (dunkelgrüne Füllung, grüne Outline, Info Symbol grünlich). Im Matrix Design hat der Reglerkreis keine Rot/Blau Farbe mehr. Fußzeile im Matrix Design grün (hellgrün nach `MX_GREEN`).
7. Nicht am Bildschirm getestet (nur Build und Zentrierungstest unter Linux).

### Runde 5 (25.10.2026, Version bleibt 1.0, Bearbeiter: Claude)
1. **Fußzeile** „PC Überblick von MaoiBaoi“ ganz an den unteren Fensterrand (über einer eventuellen Statuszeile).
2. **RPM und Prozent im Balken** exakt senkrecht mittig (`DrawTrackText`, Grundlinie über Schriftmetrik) und mit weichem dreistufigem Schlagschatten.
3. **Reglerkreis:** weicher Schlagschatten (`DrawKnobShadow`), Hover/Zieh-Glanz wie beim Info i (`DrawInfoShine`, Feld `Fan.Knob`).
4. **Standard/Matrix:** Beschriftungen mit Schlagschatten.
5. **CPU Lüfter ohne RPM:** Diagnose ins Protokoll (`Fan.NoRpm`), Ursache offen (siehe 4.5).
6. Nicht am Bildschirm getestet (Build unter Linux, 0 Warnungen/Fehler); Optik bitte unter Windows prüfen.

### Runde 4 (25.10.2026, Version bleibt 1.0, Bearbeiter: Claude)
Anlass: FPS Einbrüche und Spielabstürze, solange das Programm läuft (Besitzer, 24./25.10.2026). Ursache nicht bewiesen, Maßnahmen siehe 4.12.
1. **Spielmodus:** Bei Vollbildspielen (`SHQueryUserNotificationState`) hält das Matrix Design an (Option `GameMatrixFps`). Matrix Timer läuft nicht bei minimiertem Fenster. Hinweis im Infotext (Designs).
2. **Weniger Hardwarezugriffe:** Mainboard (Super I/O) wird nur bei festem CPU Lüfter abgefragt. Taste D fragt vor dem Auslesen frisch ab.
3. **Priorität** „Unter normal“.
4. **Lüfter Sicherheit:** `ProtectNoTemp` (Auto nach 5 Messungen ohne Temperatur), Notfall Freigabe bei Absturz und Prozessende (`OnFatal`, `ProcessExit`, `EmergencyRelease`), kein Windows Absturzdialog mehr bei UI Fehlern.
5. **Protokoll** `log.txt` (neu, `Log.cs`) mit Start, Hardware, Meldungen, langsamen Abfragen, Fehlern, Abstürzen.
6. **Experten Optionen** in `settings.ini`: `PollMs`, `Gpu`, `Mobo`, `GameMatrixFps` (zum Eingrenzen ohne neuen Build).
7. **Matrix:** Zeichenketten Cache pro Zeichen, Bild unverändert.
8. **Regler auf Wunsch des Besitzers:** Balken 20 % dicker (`num52`, Faktor 1,2), Reglerkreis 20 % kleiner (`num57`, Faktor 0,8). Balkenlänge, Klickfläche und Position unverändert. Schrift im Balken wächst mit der Dicke, das Schloss schrumpft mit dem Kreis.
9. **Geprüft, ohne Befund:** kein Speicher oder GDI Leck im Zeichencode (4000 Standard Bilder und 1500 Matrix Bilder, Speicher +2 MB), alle Aufrufer geben Pinsel und Pfade frei. Details und Messwerte: 12.7.

### Runde 3 (24.10.2026, Version bleibt 1.0, Bearbeiter: Claude)
1. **Regler zurückgesetzt:** Balken wächst im Automodus nicht mehr (`Anim` steuert nur noch das Ausblenden der Texte). Reglerkreis wieder einfarbig; `KnobBrush`, `Sigma`, `C_MID`, `SPLIT`, `HALF_BAND`, `RPMCOL` entfernt.
2. **Neuer Farbverlauf:** einfache lineare Mischung von Feuerrot nach Eisblau (`GradColor`, `Grad`) ohne harte Übergänge; Kreisfarbe läuft mit 0 bis 100 %.
3. **Auto Button:** aktiv (35,55,73), inaktiv (20,23,30) statt Grün.
4. **Texte im Regler:** RPM links, Prozent rechts, jeweils in der Mitte der Hälfte; im Automodus mit kurzem Fade ausgeblendet. Balken gleich lang, aber mittig zwischen Label und Auto Button.
5. **Matrix:** Blur 10 % schwächer (`MATRIX_BLUR_SCALE = 0.9f`, `mDiv`/`MatrixDiv()` jetzt float).
6. **Info i:** Hover-Hervorhebung um 30 % reduziert.
7. **„Standard“/„Matrix“:** 2 px nach links.
8. **Fußzeile** „PC Überblick von MaoiBaoi“ (kursiv, Temperatur-Verlauf) unter den Reglern; Platzreserve im Layout.
9. **Infotext** gekürzt, ohne Striche, „Zusatz Infos“, „Designs“, Absatz „Mit Windows starten“ entfernt, Schätzformel korrekt beschrieben (30 W und 14 %). Fenstertitel „Info zu PC Überblick“.
10. **Doku:** Abschnitte 4.2 bis 4.5, 4.8, 5, 6.3, 7, 8, 10 angepasst; neuer Abschnitt 12 mit Hintergründen und verworfenen Ideen.

### Letzte Runde (24.10.2026, Version bleibt 1.0)
1. **Temperatur-Farbverlauf:** Feuerrot jetzt schon bei 70 °C statt 80 °C (`TempTint`, Teiler 40 → 30). Gilt für GPU-Zahl, Hotspot und CPU-Zahl.
2. **Matrix-Design:** Weichzeichnung über den gesamten Hintergrund (`MATRIX_BLUR = 0.25`). Vorher lag das scharfe Bild halbtransparent über der Unschärfe, wodurch es kaum weich wirkte. Jetzt kommt nur noch das weichgezeichnete Bild (mit Helligkeitsausgleich) auf den Schirm.
3. **RPM-Bug:** Die Drehzahl wird im Automodus nicht mehr angezeigt (kein „0 RPM“ mehr). Sie erscheint nur bei festem Reglerwert.
4. **Regler:** Kreis 25 % kleiner, Balken 15 % dünner.
5. **Reglerfarben knalliger:** `C_RED` von (240,40,60) auf Feuerrot (255,36,0), `C_BLUE` von (60,160,255) auf Eisblau (80,215,255).
6. **Wattzahlen:** alle weiß mit statischem Silberverlauf zu transparent in der unteren Hälfte (`WattBrush`), statt Weiß/Blau/Orange.
7. **Info-Knopf:** Hover-Glanzeffekt, der der Maus folgt, nur solange die Maus über dem Knopf ist (`DrawInfoShine`).
8. **Info-Text** (`InfoForm.cs`) angepasst: 70 °C, Wattverlauf, RPM nur bei festem Wert, Reglerfarben, Matrix-Blur, i-Knopf-Zeile.
9. **Doku** neu geschrieben und auf den Code-Stand 1.0 gebracht (Einstellungen, Autostart, Matrix, RPM, Info-Knopf).

### Runde 2 (24.10.2026, Version bleibt 1.0)
1. **Reglerkreis:** statt einer Einzelfarbe ein Verlauf mit harter Kante bei 50 %: links Rottöne, rechts Blautöne, keine Mischfarben (`KnobBrush`). Vorher war der Kreis bei 50 % grau, weil `GradColor` dort den dunklen Mittelton liefert.
2. **Balken im Automodus länger:** wächst weich bis zum Auto-Knopf (Abstand = Abstand Doppelpunkt → Balkenanfang) und schrumpft beim Wechsel auf einen festen Wert wieder. Neu: `Fan.Anim`, `fanAnimTimer`, `Ease`.
3. **Auto-Knopf:** Schrift weiß (aktiv) bzw. gräulich (inaktiv), Rand in `DIM` wie beim Info-Knopf, Schrift mit Wattverlauf.
4. **Schloss:** Wattverlauf statt reinem Weiß.
5. Info-Text (`InfoForm.cs`) um Reglerkreis, Balken-Animation und Auto-Knopf ergänzt.

### Von 0.9 auf 1.0 (aus dem Code abgeleitet)
- Einstellungen speichern/wiederherstellen (`Settings.cs`), abschaltbar im Info-Fenster.
- „Mit Windows starten“ über die Aufgabenplanung (`Autostart.cs`).
- Zweites Design „Matrix“ mit Umschalter „Standard“/„Matrix“.
- Drehzahlanzeige (RPM) neben den Reglern, `AddRpmSensors`.
- Neuer Info-Knopf mit Symbol `info.png`, Info-Fenster mit Häkchen.
- Reglerfarbverlauf per `Sigma`/`GradColor`, Automodus mit weichgezeichnetem Grau-Balken und Schloss.
- Temperaturzahlen mit senkrechtem Weiß-zu-Tönung-Verlauf (`TempBrush`); die Ampelfarben (`TempColor`) werden nicht mehr gezeichnet.

## 12. Hintergründe, Herleitungen und verworfene Ideen (aus dem Chat mit dem Besitzer)

Dieser Abschnitt hält fest, was in den Gesprächen entschieden wurde, damit spätere Bearbeiter nichts doppelt ausprobieren.

### 12.1 System des Besitzers und Herleitung der Schätzformel
- Mainboard B450M, zwei RAM-Riegel (16 GB), eine NVMe-SSD (M.2), SATA-SSDs, eine HDD, ein Gehäuse-/CPU-Lüfter, zwei RGB-Streifen, Tastatur kabelgebunden, Maus drahtlos mit eigenem Akku, keine Wasserkühlung, Netzteil vermutlich 80 Plus Bronze oder Gold.
- Der Besitzer schätzte den nicht messbaren Anteil auf **35 W im Leerlauf** und **65 W unter Last**. Claude hielt 35 W für passend, 65 W bei hoher Last für zu niedrig, weil die Netzteilverluste mit der Leistung wachsen (bei 350 W Verbrauch gehen bei ca. 85 % Effizienz allein rund 50 W verloren), dazu kommen Spannungswandler-Verluste.
- Daraus entstand die stufenlose Formel **30 W fester Sockel (Board, RAM, Laufwerke, Lüfter, RGB, USB) + 14 % der gemessenen Leistung (Netzteil- und Wandlerverluste)**. Sie trifft die beiden Eckpunkte des Besitzers: gemessen ca. 35 W → +35 W, gemessen ca. 250 W → +65 W. Darüber steigt sie weiter (350 W → +79 W). Ein Strommessgerät an der Steckdose wäre der beste Abgleich.

### 12.2 Warum es keine Idle-/Last-Erkennung gibt
- Zuerst umgesetzt: Stufenwechsel Idle/Last anhand der Auslastung (CPU „CPU Total“ oder GPU „GPU Core“ ab 30 % = Last, zurück auf Idle erst unter 20 %). Beim Besitzer blieb die Anzeige dauerhaft auf „Leistung“ (Ursache unklar, vermutlich Hintergrundlast).
- Die Idee „Takt über 3,9 GHz = Last“ wurde verworfen, weil Ryzen-CPUs auch im Leerlauf kurz über 4 GHz boosten.
- Ersetzt durch die stufenlose Formel aus 12.1. **Der Besitzer erinnert sich teils noch an „30 W Idle / 60 W Last“. Das ist nicht implementiert** und steht deshalb auch nicht im Infotext.

### 12.3 Textdarstellung, DPI, Admin
- Text wird mit `TextRenderingHint.AntiAlias` gezeichnet (nicht `ClearTypeGridFit`): Letzteres machte große und schräge Schrift kantig.
- Der Besitzer sah anfangs unscharfe, kantige Schrift, weil die App Windows nicht meldete, dass sie DPI-fähig ist (bei 125/150 % Skalierung wurde das Fenster hochgezogen). Lösung: `SetProcessDpiAwarenessContext(-4)` zur Laufzeit (`Program.EnableDpiAwareness`).
- Ein eingebettetes Manifest (Admin, DPI) kam in frühen Builds nicht in die .exe (der Mono-Compiler ignorierte `-win32manifest`). Deshalb: Admin per `runas`-Neustart und DPI per API, nicht per Manifest.
- Ohne Admin-Rechte liefern PawnIO (CPU-Werte) und die Lüftersteuerung nichts. Bei fehlendem PawnIO lieferte die Bibliothek bei der CPU Nullen statt „keine Daten“; deshalb gilt in `Ok()` nur `> 0` als gültig.

### 12.4 Video „MAX“ (Quelle und Aufbereitung)
- Quelle: `.webm` des Besitzers (VP9/Opus, 640×360, 2,6 s, 14,985 fps, 39 Frames, schwarzer Hintergrund).
- Zuerst per Windows Media Player (ActiveX `AxHost`/WMP-OCX) abgespielt. Das kann keine Transparenz, deshalb Umstieg auf eine Bildfolge mit Alpha, gezeichnet per GDI+ (`DrawImage`), Ton per `SoundPlayer`.
- Freistellung (Python mit numpy/scipy): Der Hintergrund ist reines Schwarz. Pixel mit `max(R,G,B) ≤ 10`, die mit dem Bildrand zusammenhängen (Connected Components), gelten als Hintergrund; 1 px Dilatation; Übergangsring von 3 px: Alpha aus der Helligkeit (Smoothstep 12 bis 75), Randfarben vom nächsten sicheren Innenpixel (sonst roter Saum); leichter Gauß-Weichzeichner (0,6). Das schwarze Parfümfläschchen bleibt dabei undurchsichtig, weil es nicht mit dem Rand zusammenhängt.
- Zuschnitt auf die Person (x 26 bis 487, 462×360 px), PNG optimiert (zusammen ca. 4 MB). Ton: WAV 44,1 kHz Stereo 16 Bit, Lautstärke ×0,8 (der Clip ist laut), 2,48 s.
- Darstellung: 50 % der Fensterhöhe, unten mittig, höchstens 85 % der Breite, über der Oberfläche (Bedienung bleibt möglich).

### 12.5 Sensorik und Treiber
- Bibliothek: `LibreHardwareMonitorLib` aus dem Release v0.9.6 (net472-Build, MPL 2.0) samt Abhängigkeiten, alle als Ressourcen eingebettet. Die Bibliothek braucht für CPU-Werte und Mainboard-Lüfter den Treiber **PawnIO** (Modul-Dateien stecken in der Bibliothek, der Treiber selbst nicht).
- Installation: `winget install --id namazso.PawnIO -e`; Prüfung über den Registry-Schlüssel `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO`.
- Warum alle Mainboard-Lüfter an einem Regler hängen: Welcher Anschluss der CPU-Lüfter ist, lässt sich auf vielen Boards nicht sicher erkennen. Der Besitzer hat nur einen Lüfter und akzeptiert, dass Gehäuselüfter mitlaufen.

### 12.6 Farbgeschichte der Regler (damit nichts doppelt probiert wird)
1. Rot → Gelb → Grün: verworfen (Besitzer wollte nur Rot nach Blau).
2. Rot → Blau, lineare Mischung mit früherem Blau (60,160,255): Mitte wurde **Lila** (150,100,157). Verworfen.
3. Weiße/helle Mitte: verworfen (Besitzer wollte keine weiße Mitte).
4. Dunkler Mittelton `C_MID` und harter Übergang per tanh, Kreis mit harter Kante: umgesetzt, dem Besitzer gefiel es nicht („meine Idee war schlecht“).
5. **Jetzt:** einfache lineare Mischung Feuerrot (255,36,0) → Eisblau (80,215,255). Wegen des hohen Grünanteils von Eisblau entsteht in der Mitte ein neutrales Rosa-Grau statt Lila. Nicht ändern, ohne den Besitzer zu fragen.

### 12.7 Spielabstürze und FPS Einbrüche (Runde 4): Befunde, Messwerte, offene Vermutungen

**Meldung des Besitzers:** FPS in Spielen sinken spürbar, solange das Programm aktiv ist. Spiele stürzen regelmäßig ab: mal ohne Meldung, mal „Out of video memory“ (hält er für unwahrscheinlich), mal andere Meldungen (ohne Screenshots). GPU Hersteller, Spiele, Design und Reglerstellung zum Zeitpunkt sind **nicht bekannt**.

**Belegt (aus Code, Bibliothek und Messung):**
- Die Sensorbibliothek (LibreHardwareMonitorLib 0.9.6) ruft bei **jeder GPU Abfrage** `D3DKMTQueryStatistics` auf (GPU Auslastung und Speicher über D3DKMT, dieselbe Schnittstelle wie der Task Manager). Das lässt sich nicht einzeln abschalten, nur die komplette GPU Abfrage über `Gpu=0`. Dazu kommen GPU Treiber (NVAPI oder ADL), CPU Register über PawnIO und das Mainboard (Super I/O) bei jeder Messung. Mit `Gpu=0` und `Mobo=0` in der `settings.ini` lässt sich testen, ob diese Abfragen eine Rolle spielen.
- Standard Design: 2 bis 5 ms pro Bild, einmal pro Sekunde. Vernachlässigbar. Kein Speicherzuwachs.
- Matrix Design: Unter Mono gemessen 28 ms (760×520), etwa 140 bis 270 ms (1920×1080, Messung schwankt), 490 ms (3840×2160) **pro Bild bei 25 Bildern pro Sekunde**. Aufteilung bei 1080p unter Mono: Zeichen 23 ms, Verkleinern 105 ms, Hochskalieren mit Helligkeit 8 ms. Die Aufteilung gilt für libgdiplus und **nicht** für Windows GDI+. Qualitativ gilt: Der Aufwand wächst mit der Fensterfläche, und das ganze Fenster wird 25 mal pro Sekunde neu gezeichnet (Windows muss jedes Mal neu zusammensetzen).
- Kein Speicher oder GDI Leck im Zeichencode (siehe 11, Runde 4, Punkt 9).
- Der Fehler „Out of video memory“ stammt in vielen Spielen aus einer allgemeinen Meldung bei einem **Grafiktreiber Reset** oder verlorenem Gerät. Er bedeutet nicht zwingend, dass wirklich der Grafikspeicher voll war. Dieses Programm belegt selbst keinen nennenswerten Grafikspeicher (GDI+ rechnet im Hauptspeicher).

**Vermutungen, nicht bewiesen (nach Wahrscheinlichkeit grob geordnet, mit Prüfung):**
1. **Fester Lüfterwert** (vor allem GPU) bei Last: Karte wird zu heiß oder instabil, Treiber Reset, Spiel meldet „Out of video memory“. Prüfung: Beide Regler auf Auto, dann spielen. `log.txt` zeigt den Zustand beim Start („Luefter: GPU … fest … %“).
2. **Sensorabfragen kollidieren** mit Spiel, Grafiktreiber oder anderen Tools (RGB Software, Afterburner, HWiNFO). Prüfung: `Gpu=0`, dann `Mobo=0`, dann `PollMs=3000` in `%APPDATA%\PCUeberblick\settings.ini` (Programm vorher schließen), jeweils ein paar Spielrunden. Nach `log.txt` suchen: „Langsame Abfrage“ kurz vor einem Absturz wäre ein starker Hinweis.
3. **Anti Cheat** (Easy Anti Cheat, BattlEye, Vanguard, FACEIT) mag Kernel Treiber wie PawnIO nicht oder beendet das Spiel, wenn ein Hardware Monitor läuft. Typisch: Absturz ohne Meldung nur in Spielen mit Anti Cheat. Prüfung: Programm vor dem Spielen beenden, wenn nur diese Spiele betroffen sind.
4. **Matrix Design** und Fenster über dem Spiel („Immer im Vordergrund“) kosten FPS. Mit Runde 4 gemildert, am sichersten ist Standard Design.
5. **CPU Auslastung** durch das Programm bei vollen Kernen (das Spiel hat dann weniger Takt). Priorität „Unter normal“ ist die Gegenmaßnahme.

**Entscheidungsweg für die nächste Runde:** Von dem Besitzer erfragen: GPU Modell und Hersteller, welche Spiele (mit oder ohne Anti Cheat), welches Design, ob Regler auf Auto oder fest standen, ob „Immer im Vordergrund“ aktiv war, sowie die Datei `log.txt` und `PCUeberblick_Sensoren.txt` (Taste D) und die Windows Ereignisanzeige (Windows Protokolle, System: Quelle `nvlddmkm`, `amdkmdag` oder „Display“, Ereignis 4101 = Treiber Timeout). Ohne diese Daten nicht weiter am Programm raten.
