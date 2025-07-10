using QueryBuilder.Models;
using QueryBuilder.Enums;
using System.Collections.Generic;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Constructor de consultas SQL del tipo UPDATE.
/// Permite construir sentencias UPDATE con asignación de columnas y condiciones WHERE.
/// </summary>
public class UpdateQueryBuilder
{
    /// <summary>Nombre de la tabla a actualizar.</summary>
    public string Table { get; set; } = string.Empty;

    /// <summary>Columnas a actualizar con sus valores nuevos.</summary>
    public Dictionary<string, string> SetColumns { get; set; } = new();

    /// <summary>Condiciones para la cláusula WHERE.</summary>
    public List<string> WhereConditions { get; set; } = new();

    /// <summary>
    /// Construye la sentencia SQL UPDATE.
    /// </summary>
    /// <returns>Consulta SQL generada.</returns>
    public string Build()
    {
        var sb = new StringBuilder();
        sb.Append($"UPDATE {Table} SET ");

        var setParts = new List<string>();
        foreach (var kvp in SetColumns)
            setParts.Add($"{kvp.Key} = {kvp.Value}");

        sb.Append(string.Join(", ", setParts));

        if (WhereConditions.Count > 0)
            sb.Append(" WHERE ").Append(string.Join(" AND ", WhereConditions));

        return sb.ToString();
    }

    /// <summary>
    /// Genera una instancia de QueryTranslationContext con los datos del UPDATE configurado.
    /// </summary>
    /// <returns>Contexto de traducción para consulta UPDATE.</returns>
    public QueryTranslationContext BuildContext()
    {
        var updateValues = new Dictionary<string, object>();
        foreach (var kvp in SetColumns)
        {
            updateValues[kvp.Key] = kvp.Value;
        }

        return new QueryTranslationContext
        {
            TableName = Table,
            UpdateValues = updateValues,
            WhereClause = WhereConditions.Count > 0 ? string.Join(" AND ", WhereConditions) : null,
            Operation = QueryOperation.Update
        };
    }
}
