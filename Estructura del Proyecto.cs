using System;
using System.Globalization;

namespace RestUtilities.Common.Helpers;

/// <summary>
/// Helper para operaciones comunes con fechas y horas.
/// Permite conversión, formateo y validación de DateTime y DateTimeOffset.
/// </summary>
public static class DateTimeHelper
{
    /// <summary>
    /// Devuelve la fecha y hora actual en UTC.
    /// </summary>
    public static DateTime UtcNow => DateTime.UtcNow;

    /// <summary>
    /// Devuelve la fecha y hora actual en la zona horaria local.
    /// </summary>
    public static DateTime Now => DateTime.Now;

    /// <summary>
    /// Convierte una fecha UTC a la hora local del sistema.
    /// </summary>
    public static DateTime ToLocalTime(DateTime utcDateTime)
    {
        return utcDateTime.ToLocalTime();
    }

    /// <summary>
    /// Convierte una fecha local a UTC.
    /// </summary>
    public static DateTime ToUtc(DateTime localDateTime)
    {
        return localDateTime.ToUniversalTime();
    }

    /// <summary>
    /// Intenta convertir un string a DateTime. Devuelve null si falla.
    /// </summary>
    public static DateTime? TryParse(string? input, string? format = null, CultureInfo? culture = null)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        culture ??= CultureInfo.InvariantCulture;

        if (format == null)
        {
            return DateTime.TryParse(input, culture, DateTimeStyles.None, out var result) ? result : null;
        }
        else
        {
            return DateTime.TryParseExact(input, format, culture, DateTimeStyles.None, out var result) ? result : null;
        }
    }

    /// <summary>
    /// Formatea un DateTime a una cadena con formato ISO 8601 (UTC).
    /// </summary>
    public static string ToIsoUtc(DateTime dateTime)
    {
        return dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formatea un DateTime con un formato personalizado.
    /// </summary>
    public static string Format(DateTime dateTime, string format, CultureInfo? culture = null)
    {
        return dateTime.ToString(format, culture ?? CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Convierte un timestamp UNIX (segundos desde 1970) a DateTime (UTC).
    /// </summary>
    public static DateTime FromUnixTimestamp(long timestamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
    }

    /// <summary>
    /// Convierte un DateTime (UTC o local) a timestamp UNIX (segundos desde 1970).
    /// </summary>
    public static long ToUnixTimestamp(DateTime dateTime)
    {
        return new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeSeconds();
    }

    /// <summary>
    /// Devuelve true si el string es una fecha válida en el formato especificado.
    /// </summary>
    public static bool IsValidDate(string input, string? format = null, CultureInfo? culture = null)
    {
        return TryParse(input, format, culture).HasValue;
    }
}
