# PC Overview
## English
PC Overview shows GPU and CPU temperature and power draw in huge, readable numbers and lets you control both fans directly. Includes hotspot display, estimated total power, fan speed readout and an over temperature protection that switches back to Auto on its own. You set the temperature color scale for GPU, CPU and hotspot yourself. Two designs (Standard and Matrix), fullscreen, autostart, German and English. The software is also highly scalable

## German
PC Überblick zeigt Temperatur und Stromverbrauch deiner GPU und CPU riesig und gut lesbar und lässt dich beide Lüfter direkt regeln. Mit Hotspot Anzeige, geschätztem Gesamtverbrauch, Drehzahlanzeige und Schutzfunktion, die bei zu hohen Temperaturen automatisch zurück auf Auto schaltet. Die Farbskala der Temperaturen stellst du für GPU, CPU und Hotspot selbst ein. Zwei Designs (Standard und Matrix), Vollbild, Autostart, Deutsch und Englisch. Die Software ist zudem extrem gut skalierbar. 

<img width="911" height="661" alt="{A4FD4C39-7FC9-4470-9D0E-272EDC5A4CE6}" src="https://github.com/user-attachments/assets/773ff999-854e-4fbf-8971-bb3d31f0763f" />
<img width="910" height="662" alt="{751E1D01-A3EB-4E57-BCF4-9BB002CB1E0B}" src="https://github.com/user-attachments/assets/dcafc855-8140-4648-afd0-3944f06dfb99" />



<img width="614" height="254" alt="{3A22E719-5974-40CE-86D0-3BA6F7562811}" src="https://github.com/user-attachments/assets/edf374b2-e6dc-4d02-94db-332345aec550" />

English
Features
All essentials at a glance: GPU temperature (including hotspot), CPU temperature, GPU and CPU power draw in Watts, total combined power, and estimated PC power consumption

Dynamic temperature colors: Numbers smoothly transition in color from ice blue to fiery red; gradient ranges can be customized individually for GPU, CPU, and hotspot

Dual fan control: GPU fan and CPU fan (all controllable motherboard fans), complete with RPM display and an "Auto" button to return to automatic mode

Built-in safety feature: If temperatures get too high, the fan control automatically reverts to Auto mode

Safe shutdown: Fans automatically revert to Auto mode upon exiting or in case of a crash

Two visual themes: Standard and Matrix (featuring a smooth digital rain effect)

German and English: Switchable in the Info window; defaults automatically to your system language

Flexible: Fullscreen (F11), Always on Top (T), remembers window size and settings, optional start with Windows

Lightweight: Runs with low process priority; the Matrix theme can optionally pause during fullscreen gaming

Private: The program itself does not establish any internet connection

Installation
Download PCUeberblick.exe under Releases

Run the executable and confirm the Administrator prompt

Done. To read CPU values and control motherboard fans, the program will ask once if it should install the PawnIO driver via winget. Restart the program after installation

Requirements
Windows 10 or 11

Administrator privileges (required for CPU readouts and fan control)

PawnIO (for CPU readouts and motherboard fans)

Not all motherboards allow fan control; in that case, the slider will show n/a

Controls
Left-click or drag on a slider: Set a fixed speed

Right-click on a slider or click Auto: Return to automatic control

F11 or double-click: Toggle fullscreen (Esc to exit)

T: Toggle Always on Top

D: Export a list of all detected sensors to the desktop

I: Install PawnIO

Click the "i" icon: Open Info and Settings

Important Notes
Use at your own risk. When adjusting fans manually, always keep an eye on temperatures. The built-in safety fallback triggers at CPU 85 °C, GPU 80 °C, or Hotspot 90 °C, but is not a substitute for proper system monitoring.

Windows SmartScreen or your antivirus might display a warning because the executable is not digitally signed and accesses hardware drivers. The entire source code is available in this repository; you can also compile the executable yourself.

Building from Source
Requires a .NET SDK with C# 14 on Windows.

cd src/PCUeberblick
dotnet build -c Release
The resulting binary will be located at bin/Release/net472/PCUeberblick.exe. All required DLLs, images, and sounds are embedded directly into the executable. For in-depth technical details, refer to docs/Entwicklerdoku.md.

Credits
Special thanks to LibreHardwareMonitor for sensor and fan interfacing, and to PawnIO for driver-level hardware access. For additional third-party libraries, see THIRD_PARTY_NOTICES.md.

License
MIT, see LICENSE. Author: MaoiBaoi

German
## Vorzüge

* **Alles Wichtige groß im Blick:** GPU Temperatur (mit Hotspot), CPU Temperatur, Leistungsaufnahme von GPU und CPU in Watt, Summe und geschätzter Gesamtverbrauch des PCs
* **Temperaturfarben:** Die Zahlen färben sich stufenlos von Eisblau bis Feuerrot, die Farbverlaufbereiche stellst du für GPU, CPU und Hotspot selbst ein
* **Zwei Lüfterregler:** GPU Lüfter und CPU Lüfter (alle steuerbaren Mainboard Lüfter), mit Drehzahlanzeige und Auto Button für die Rückkehr zur Automatik
* **Eingebaute Schutzfunktion:** Wird es zu heiß, springt der Regler automatisch zurück auf Auto
* **Sicher beim Beenden:** Beim Schließen oder Absturz gehen die Lüfter wieder auf Automatik
* **Zwei Designs:** Standard und Matrix, mit weichem Zeichenregen
* **Deutsch und Englisch:** umschaltbar im Info Fenster, standardmäßig nach Systemsprache
* **Flexibel:** Vollbild (F11), immer im Vordergrund (T), merkt sich Fenster und Einstellungen, optional mit Windows starten
* **Leicht:** Läuft mit niedriger Priorität, das Matrix Design hält bei Vollbildspielen auf Wunsch an
* **Privat:** Das Programm selbst baut keine Internetverbindung auf

## Installation

1. Unter **Releases** die Datei `PCUeberblick.exe` herunterladen
2. Starten und die Administrator Abfrage bestätigen
3. Fertig. Für CPU Werte und Mainboard Lüfter fragt das Programm einmalig, ob es den Treiber PawnIO per winget installieren soll. Danach das Programm neu starten

## Voraussetzungen

* Windows 10 oder 11
* Administratorrechte, sonst fehlen CPU Werte und Lüftersteuerung
* PawnIO für CPU Werte und Mainboard Lüfter
* Nicht jedes Mainboard erlaubt Lüftersteuerung. Dann zeigt der Regler n/a

## Bedienung

* Linksklick oder Ziehen auf einem Regler: festen Wert setzen
* Rechtsklick auf den Regler oder Klick auf Auto: zurück zur Automatik
* F11 oder Doppelklick: Vollbild, Esc beendet es
* T: immer im Vordergrund
* D: Liste aller Sensoren auf den Desktop schreiben
* I: PawnIO installieren
* Klick auf das i: Info und Einstellungen

## Hinweise

* **Nutzung auf eigene Verantwortung.** Wer Lüfter von Hand einstellt, sollte die Temperaturen im Auge behalten. Die Schutzfunktion greift ab CPU 85 °C, GPU 80 °C oder Hotspot 90 °C, ersetzt aber keine Überwachung.
* Windows SmartScreen oder dein Virenschutz können warnen, weil die EXE nicht signiert ist und Hardware Treiber nutzt. Der komplette Quellcode liegt in diesem Repository, du kannst die EXE auch selbst bauen.

## Selbst bauen

Voraussetzung ist ein .NET SDK mit C# 14 unter Windows.

```
cd src/PCUeberblick
dotnet build -c Release
```

Das Ergebnis liegt unter `bin/Release/net472/PCUeberblick.exe`. Alle DLLs, Bilder und Töne werden in die EXE eingebettet. Ausführliche technische Details stehen in `docs/Entwicklerdoku.md`.

## Danke an

[LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) für die Sensor und Lüfteranbindung und [PawnIO](https://pawnio.eu) für den Treiberzugriff. Weitere Bibliotheken siehe `THIRD_PARTY_NOTICES.md`.

## Lizenz

MIT, siehe `LICENSE`. Autor: MaoiBaoi

---
