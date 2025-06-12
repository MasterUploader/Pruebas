using System;
using System.Diagnostics;

namespace RestUtilities.Common.Helpers;

/// <summary>
/// Clase utilitaria para medir tiempos de ejecución mediante Stopwatch de forma simplificada.
/// </summary>
public sealed class StopwatchHelper : IDisposable
{
    private readonly Stopwatch _stopwatch;
    private readonly Action<string>? _onDisposeMessage;
    private readonly string? _label;

    /// <summary>
    /// Crea una nueva instancia y empieza la medición automáticamente.
    /// </summary>
    private StopwatchHelper(string? label = null, Action<string>? onDisposeMessage = null)
    {
        _label = label;
        _onDisposeMessage = onDisposeMessage;
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Inicia un nuevo cronómetro.
    /// </summary>
    public static StopwatchHelper StartNew(string? label = null, Action<string>? onDisposeMessage = null)
        => new(label, onDisposeMessage);

    /// <summary>
    /// Devuelve el tiempo transcurrido como TimeSpan.
    /// </summary>
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    /// <summary>
    /// Devuelve el tiempo transcurrido en milisegundos.
    /// </summary>
    public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

    /// <summary>
    /// Devuelve el tiempo transcurrido en segundos como número decimal.
    /// </summary>
    public double ElapsedSeconds => _stopwatch.Elapsed.TotalSeconds;

    /// <summary>
    /// Reinicia el cronómetro.
    /// </summary>
    public void Restart() => _stopwatch.Restart();

    /// <summary>
    /// Detiene el cronómetro.
    /// </summary>
    public void Stop() => _stopwatch.Stop();

    /// <summary>
    /// Devuelve una representación legible del tiempo transcurrido, por ejemplo: "1.234 segundos".
    /// </summary>
    public string ToReadable()
        => $"{Elapsed.TotalSeconds:F3} segundos";

    /// <summary>
    /// Devuelve una cadena formateada con una etiqueta personalizada.
    /// </summary>
    public string ToLogFormat()
        => string.IsNullOrWhiteSpace(_label)
            ? $"Duración: {ToReadable()}"
            : $"{_label} tomó {ToReadable()}";

    /// <summary>
    /// Detiene el cronómetro y devuelve el tiempo legible.
    /// </summary>
    public string StopAndGetReadable()
    {
        Stop();
        return ToReadable();
    }

    /// <summary>
    /// Detiene el cronómetro y devuelve la cadena de log con formato.
    /// </summary>
    public string StopAndGetLog()
    {
        Stop();
        return ToLogFormat();
    }

    /// <summary>
    /// Detiene y ejecuta la acción con el mensaje de log si se proporcionó.
    /// </summary>
    public void Dispose()
    {
        Stop();
        if (_onDisposeMessage != null)
        {
            _onDisposeMessage(ToLogFormat());
        }
    }
}
