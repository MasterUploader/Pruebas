using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace RestUtilities.Common.Helpers;

/// <summary>
/// Métodos auxiliares para trabajar con enumeraciones.
/// </summary>
public static class EnumHelper
{
    /// <summary>
    /// Obtiene todos los valores definidos en una enumeración.
    /// </summary>
    public static IEnumerable<TEnum> GetValues<TEnum>() where TEnum : Enum
        => Enum.GetValues(typeof(TEnum)).Cast<TEnum>();

    /// <summary>
    /// Verifica si un valor (entero o cadena) está definido en la enumeración.
    /// </summary>
    public static bool IsDefined<TEnum>(object value) where TEnum : Enum
        => Enum.IsDefined(typeof(TEnum), value);

    /// <summary>
    /// Convierte una cadena al valor del enum (lanzando excepción si falla).
    /// </summary>
    public static TEnum Parse<TEnum>(string value) where TEnum : struct, Enum
        => Enum.Parse<TEnum>(value, ignoreCase: true);

    /// <summary>
    /// Intenta convertir una cadena al valor del enum, devolviendo false si no se puede.
    /// </summary>
    public static bool TryParse<TEnum>(string? value, out TEnum result) where TEnum : struct, Enum
        => Enum.TryParse(value, ignoreCase: true, out result);

    /// <summary>
    /// Obtiene la descripción asociada a un valor de enumeración.
    /// </summary>
    public static string GetDescription(Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field == null) return value.ToString();

        var display = field.GetCustomAttribute<DisplayAttribute>();
        if (!string.IsNullOrWhiteSpace(display?.Name))
            return display.Name!;

        var description = field.GetCustomAttribute<DescriptionAttribute>();
        if (!string.IsNullOrWhiteSpace(description?.Description))
            return description.Description!;

        return value.ToString();
    }

    /// <summary>
    /// Devuelve un diccionario con los valores del enum como int y sus descripciones.
    /// </summary>
    public static Dictionary<int, string> GetValueDescriptionMap<TEnum>() where TEnum : Enum
        => GetValues<TEnum>().ToDictionary(
            e => Convert.ToInt32(e),
            e => GetDescription(e)
        );

    /// <summary>
    /// Devuelve un diccionario con los nombres del enum como string y sus descripciones.
    /// </summary>
    public static Dictionary<string, string> GetNameDescriptionMap<TEnum>() where TEnum : Enum
        => GetValues<TEnum>().ToDictionary(
            e => e.ToString(),
            e => GetDescription(e)
        );

    /// <summary>
    /// Obtiene el atributo Display completo si está presente en el valor del enum.
    /// </summary>
    public static DisplayAttribute? GetDisplayAttribute(Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        return field?.GetCustomAttribute<DisplayAttribute>();
    }

    /// <summary>
    /// Obtiene el atributo Description si está presente en el valor del enum.
    /// </summary>
    public static DescriptionAttribute? GetDescriptionAttribute(Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        return field?.GetCustomAttribute<DescriptionAttribute>();
    }

    /// <summary>
    /// Devuelve una lista de objetos { Value, Label } útil para listas desplegables.
    /// </summary>
    public static List<EnumItem> ToList<TEnum>() where TEnum : Enum
        => GetValues<TEnum>()
            .Select(e => new EnumItem
            {
                Value = Convert.ToInt32(e),
                Label = GetDescription(e)
            })
            .ToList();

    /// <summary>
    /// Devuelve una lista de descripciones separadas de un enum con flags.
    /// </summary>
    public static List<string> GetFlagsDescriptions(Enum value)
    {
        return Enum.GetValues(value.GetType())
            .Cast<Enum>()
            .Where(flag => value.HasFlag(flag) && Convert.ToInt32(flag) != 0)
            .Select(GetDescription)
            .ToList();
    }

    /// <summary>
    /// Devuelve una lista de valores numéricos individuales para un enum con flags.
    /// </summary>
    public static List<int> GetFlagsValues(Enum value)
    {
        return Enum.GetValues(value.GetType())
            .Cast<Enum>()
            .Where(flag => value.HasFlag(flag) && Convert.ToInt32(flag) != 0)
            .Select(flag => Convert.ToInt32(flag))
            .ToList();
    }

    /// <summary>
    /// Devuelve si un enum tiene definido el atributo [Flags].
    /// </summary>
    public static bool IsFlagsEnum<TEnum>() where TEnum : Enum
        => typeof(TEnum).GetCustomAttribute<FlagsAttribute>() != null;

    /// <summary>
    /// Modelo auxiliar para representar elementos visuales.
    /// </summary>
    public class EnumItem
    {
        public int Value { get; set; }
        public string Label { get; set; } = string.Empty;
    }
}
