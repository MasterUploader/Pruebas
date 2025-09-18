using System;
using System.Globalization;

public static class JulianDate
{
    /// <summary>
    /// Formatos de salida de fecha juliana (año + día del año).
    /// </summary>
    public enum Format { YyyyDdd, YyDdd }

    /// <summary>
    /// Convierte una fecha en formato "yyyyMMdd" a fecha juliana (yyyyddd o yyddd).
    /// </summary>
    /// <param name="yyyymmdd">Fecha en texto, p.ej. "20250918".</param>
    /// <param name="format">Formato de salida (por defecto yyyyddd).</param>
    /// <returns>Cadena juliana, p.ej. "2025261" o "25261".</returns>
    public static string ToJulian(string yyyymmdd, Format format = Format.YyyyDdd)
    {
        if (string.IsNullOrWhiteSpace(yyyymmdd) || yyyymmdd.Length != 8)
            throw new ArgumentException("Se espera una fecha en formato yyyyMMdd de 8 dígitos.", nameof(yyyymmdd));

        var dt = DateTime.ParseExact(yyyymmdd, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None);
        string doy = dt.DayOfYear.ToString("000", CultureInfo.InvariantCulture); // Día del año con 3 dígitos

        return format == Format.YyyyDdd
            ? dt.ToString("yyyy", CultureInfo.InvariantCulture) + doy
            : dt.ToString("yy", CultureInfo.InvariantCulture) + doy;
    }
}
