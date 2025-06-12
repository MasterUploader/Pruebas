using System;
using System.Globalization;

namespace RestUtilities.Common.Helpers;

/// <summary>
/// Métodos auxiliares para trabajar con TimeSpan (duraciones de tiempo).
/// </summary>
public static class TimeSpanHelper
{
    /// <summary>
    /// Convierte un TimeSpan a un formato legible, por ejemplo: "1 hora, 2 minutos, 3 segundos".
    /// </summary>
    public static string ToReadableString(TimeSpan span)
    {
        if (span == TimeSpan.Zero)
            return "0 segundos";

        var parts = new List<string>();
        if (span.Days > 0) parts.Add($"{span.Days} día{(span.Days > 1 ? "s" : "")}");
        if (span.Hours > 0) parts.Add($"{span.Hours} hora{(span.Hours > 1 ? "s" : "")}");
        if (span.Minutes > 0) parts.Add($"{span.Minutes} minuto{(span.Minutes > 1 ? "s" : "")}");
        if (span.Seconds > 0) parts.Add($"{span.Seconds} segundo{(span.Seconds > 1 ? "s" : "")}");
        if (span.Milliseconds > 0) parts.Add($"{span.Milliseconds} ms");

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Devuelve el TimeSpan resultante entre dos fechas.
    /// </summary>
    public static TimeSpan GetDuration(DateTime start, DateTime end)
        => end - start;

    /// <summary>
    /// Convierte segundos a TimeSpan.
    /// </summary>
    public static TimeSpan FromSeconds(double seconds)
        => TimeSpan.FromSeconds(seconds);

    /// <summary>
    /// Convierte minutos a TimeSpan.
    /// </summary>
    public static TimeSpan FromMinutes(double minutes)
        => TimeSpan.FromMinutes(minutes);

    /// <summary>
    /// Devuelve la cantidad total de milisegundos de un TimeSpan.
    /// </summary>
    public static double ToTotalMilliseconds(TimeSpan span)
        => span.TotalMilliseconds;

    /// <summary>
    /// Devuelve la cantidad total de segundos de un TimeSpan.
    /// </summary>
    public static double ToTotalSeconds(TimeSpan span)
        => span.TotalSeconds;

    /// <summary>
    /// Indica si un TimeSpan es mayor que otro.
    /// </summary>
    public static bool IsGreaterThan(TimeSpan a, TimeSpan b)
        => a > b;

    /// <summary>
    /// Indica si un TimeSpan está dentro de un rango específico.
    /// </summary>
    public static bool IsBetween(TimeSpan value, TimeSpan min, TimeSpan max)
        => value >= min && value <= max;

    /// <summary>
    /// Devuelve una cadena con formato corto: "hh:mm:ss.fff"
    /// </summary>
    public static string ToShortFormat(TimeSpan span)
        => span.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);
}
