namespace UGeekStore.Core;

public static class Phone
{
    /// <summary>Normalizes Armenian numbers to 374XXXXXXXX (digits only). 091123456 / +374 91 123456 → 37491123456.</summary>
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var d = new string(raw.Where(char.IsDigit).ToArray());
        if (d.StartsWith("00")) d = d[2..];
        if (d.Length == 9 && d.StartsWith("0")) d = "374" + d[1..];
        else if (d.Length == 8) d = "374" + d;
        return d;
    }

    public static string Display(string? normalized)
    {
        if (string.IsNullOrEmpty(normalized)) return "";
        return normalized.StartsWith("374") && normalized.Length == 11
            ? $"0{normalized.Substring(3, 2)} {normalized.Substring(5, 2)}-{normalized.Substring(7, 2)}-{normalized.Substring(9, 2)}"
            : normalized;
    }
}
