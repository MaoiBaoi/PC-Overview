# PC Überblick

**Temperatur, Leistung und Lüfter deines PCs. Groß, klar und auf einen Blick.**

PC Überblick ist ein kleines, kostenloses Windows Programm. Es zeigt dir GPU und CPU Temperatur sowie den Stromverbrauch in einer riesigen, gut lesbaren Schrift und lässt dich beide Lüfter direkt regeln. Eine einzige EXE, keine Installation, keine Werbung, kein Konto.

## Vorzüge

* **Alles Wichtige groß im Blick:** GPU Temperatur (mit Hotspot), CPU Temperatur, Leistungsaufnahme von GPU und CPU in Watt, Summe und geschätzter Gesamtverbrauch des PCs
* **Temperaturfarben:** Die Zahlen färben sich stufenlos von Eisblau bis Feuerrot, die Grenzen stellst du für GPU, CPU und Hotspot selbst ein
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

## English summary

**PC Überblick** shows GPU and CPU temperature and power draw in huge, readable numbers and lets you control the GPU fan and the CPU/motherboard fans with a clean slider UI. Single portable EXE, built on LibreHardwareMonitor, includes an automatic over temperature protection and two visual themes (Standard and Matrix). Windows 10/11, needs administrator rights and the PawnIO driver for CPU values and motherboard fan control. The UI is available in German and English (switch it in the info window, the default follows your system language). Use at your own risk.
