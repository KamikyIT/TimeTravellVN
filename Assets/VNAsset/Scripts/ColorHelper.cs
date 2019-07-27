using UnityEngine;

public static class ColorHelper
{
	public static Color? HexToColor(string hex)
	{

		if (string.IsNullOrEmpty(hex))
			return null;

		if (hex.StartsWith("0x") && hex.Length > 2)
			hex = hex.Substring(2);

		hex = hex.Replace("#", "");
		byte a = 255;
		byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
		byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
		byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

		if (hex.Length == 8)
			a = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

		return new Color32(r, g, b, a);
	}
}
