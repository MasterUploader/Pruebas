using System;

namespace Logging.Attributes;

/// <summary>
/// Atributo personalizado que permite marcar una propiedad de un modelo para ser utilizada
/// como parte del nombre del archivo de log.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class LogFileNameAttribute : Attribute
{
    /// <summary>
    /// Etiqueta opcional que se antepondrá al valor de la propiedad en el nombre del archivo de log.
    /// Por ejemplo: si Label = "id" y el valor es "123", se genera "id-123".
    /// </summary>
    public string? Label { get; }

    /// <summary>
    /// Inicializa una nueva instancia del atributo <see cref="LogFileNameAttribute"/>.
    /// </summary>
    /// <param name="label">Etiqueta opcional para anteponer al valor al generar el nombre del archivo de log.</param>
    public LogFileNameAttribute(string? label = null)
    {
        Label = label;
    }
}
