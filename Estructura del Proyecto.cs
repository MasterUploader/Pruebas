using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using QueryBuilder.Helpers;

namespace QueryBuilder.Models;

/// <summary>
/// Permite construir expresiones CASE WHEN para usarse en SELECT, WHERE, HAVING y subconsultas.
/// </summary>
public class CaseWhenBuilder
{
    private readonly List<(string ConditionSql, string ResultSql)> _whenThens = [];
    private string? _elseSql;

    /// <summary>
    /// Agrega una cláusula WHEN usando una condición como texto SQL.
    /// </summary>
    /// <param name="condition">Condición SQL (por ejemplo: "TIPO = 'A'").</param>
    public CaseWhenBuilder When(string condition)
    {
        _whenThens.Add((condition, string.Empty));
        return this;
    }

    /// <summary>
    /// Agrega una cláusula WHEN usando una expresión lambda.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad.</typeparam>
    /// <param name="condition">Expresión booleana (por ejemplo: x => x.TIPO == "A").</param>
    public CaseWhenBuilder When<T>(Expression<Func<T, bool>> condition)
    {
        var sql = ExpressionToSqlConverter.Convert(condition);
        _whenThens.Add((sql, string.Empty));
        return this;
    }

    /// <summary>
    /// Define el resultado para la última cláusula WHEN agregada.
    /// </summary>
    /// <param name="result">Valor SQL a retornar si la condición se cumple (por ejemplo: "'Administrador'").</param>
    public CaseWhenBuilder Then(string result)
    {
        if (_whenThens.Count == 0)
            throw new InvalidOperationException("Debe agregarse un WHEN antes de THEN.");

        var last = _whenThens[^1];
        _whenThens[^1] = (last.ConditionSql, result);
        return this;
    }

    /// <summary>
    /// Define el resultado para el caso ELSE.
    /// </summary>
    /// <param name="result">Valor SQL si ninguna condición se cumple (por ejemplo: "'Otro'").</param>
    public CaseWhenBuilder Else(string result)
    {
        _elseSql = result;
        return this;
    }

    /// <summary>
    /// Genera la expresión CASE WHEN completa en formato SQL.
    /// </summary>
    public string Build()
    {
        if (_whenThens.Count == 0)
            throw new InvalidOperationException("Debe contener al menos un WHEN.");

        var parts = new List<string> { "CASE" };

        foreach (var (condition, result) in _whenThens)
        {
            if (string.IsNullOrWhiteSpace(condition) || string.IsNullOrWhiteSpace(result))
                throw new InvalidOperationException("Cada WHEN debe tener su correspondiente THEN.");
            parts.Add($"WHEN {condition} THEN {result}");
        }

        if (!string.IsNullOrWhiteSpace(_elseSql))
            parts.Add($"ELSE {_elseSql}");

        parts.Add("END");

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Permite construir la expresión CASE WHEN directamente.
    /// </summary>
    /// <param name="config">Acción que recibe un constructor de CASE WHEN.</param>
    /// <param name="alias">Alias opcional para la columna resultante.</param>
    public static (string ColumnSql, string? Alias) Build(Action<CaseWhenBuilder> config, string? alias = null)
    {
        var builder = new CaseWhenBuilder();
        config(builder);
        return (builder.Build(), alias);
    }
}
.SelectCase(
    CaseWhenBuilder.Build(cb => cb
        .When<USUADMIN>(x => x.TIPUSU == "A").Then("'Administrador'")
        .When<USUADMIN>(x => x.TIPUSU == "U").Then("'Usuario'")
        .Else("'Otro'"),
    "DESCRIPCION")
)
