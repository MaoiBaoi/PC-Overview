using System.Collections.Generic;
using System.Drawing;
using LibreHardwareMonitor.Hardware;

internal class Fan
{
	public string Label;

	public List<ISensor> Sensors = new List<ISensor>();

	public float? Pct;

	public float AutoNow = float.NaN;

	public int NoTemp;

	public List<ISensor> RpmSensors = new List<ISensor>();

	public List<ISensor> RpmFallback = new List<ISensor>();

	public float RpmNow = float.NaN;

	public float Applied = -1f;

	public float Anim;

	public string LastPctText = "";

	public string LastRpmText = "";

	public Bitmap Blur;

	public string BlurKey;

	public Bitmap Fixed;

	public string FixedKey;

	public RectangleF Track;

	public RectangleF Hit;

	public RectangleF Btn;

	public RectangleF Knob;

	public int NoRpm;

	public bool Available => Sensors.Count > 0;
}
