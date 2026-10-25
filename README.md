# PC Overview
## English
PC Overview shows GPU and CPU temperature and power draw in huge, readable numbers and lets you control both fans directly. Includes hotspot display, estimated total power, fan speed readout and an over temperature protection that switches back to Auto on its own. You set the temperature color scale for GPU, CPU and hotspot yourself. Two designs (Standard and Matrix), fullscreen, autostart, German and English. The software is also highly scalable

## German
PC Überblick zeigt Temperatur und Stromverbrauch deiner GPU und CPU riesig und gut lesbar und lässt dich beide Lüfter direkt regeln. Mit Hotspot Anzeige, geschätztem Gesamtverbrauch, Drehzahlanzeige und Schutzfunktion, die bei zu hohen Temperaturen automatisch zurück auf Auto schaltet. Die Farbskala der Temperaturen stellst du für GPU, CPU und Hotspot selbst ein. Zwei Designs (Standard und Matrix), Vollbild, Autostart, Deutsch und Englisch. Die Software ist zudem extrem gut skalierbar. 

<img width="911" height="661" alt="{A4FD4C39-7FC9-4470-9D0E-272EDC5A4CE6}" src="https://github.com/user-attachments/assets/773ff999-854e-4fbf-8971-bb3d31f0763f" />
<img width="909" height="660" alt="{240A2E39-B7A9-4ABD-BED0-E38B104910B9}" src="https://github.com/user-attachments/assets/dafaf50e-fdd2-48b5-bd3f-6a2f07aad129" />


<img width="614" height="254" alt="{3A22E719-5974-40CE-86D0-3BA6F7562811}" src="https://github.com/user-attachments/assets/edf374b2-e6dc-4d02-94db-332345aec550" />


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
