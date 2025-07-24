using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace QueryBuilder.Helpers;

/// <summary>
/// Builder para generar expresiones CASE WHEN en SQL.
/// Soporta condiciones como cadenas o expresiones lambda.
/// </summary>
public class CaseWhenBuilder
{
    private readonly List<(string ConditionSql, string ResultSql)> _cases = [];
    private string? _elseValue;

    /// <summary>
    /// Agrega una condición WHEN con expresión SQL directa.
    /// </summary>
    /// <param name="conditionSql">Condición en SQL.</param>
    public CaseWhenBuilder When(string conditionSql)
    {
        _cases.Add((conditionSql, ""));
        return this;
    }

    /// <summary>
    /// Agrega una condición WHEN utilizando una expresión lambda booleana.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad.</typeparam>
    /// <param name="condition">Expresión booleana.</param>
    public CaseWhenBuilder When<T>(Expression<Func<T, bool>> condition)
    {
        var sql = Helpers.ExpressionToSqlConverter.Convert(condition);
        _cases.Add((sql, ""));
        return this;
    }

    /// <summary>
    /// Asocia un resultado THEN con la última condición WHEN.
    /// </summary>
    /// <param name="result">Valor que se devolverá si la condición se cumple.</param>
    public CaseWhenBuilder Then(string result)
    {
        if (_cases.Count == 0)
            throw new InvalidOperationException("Debe llamarse a When() antes de Then().");

        var last = _cases[^1];
        _cases[^1] = (last.ConditionSql, $"'{result}'");
        return this;
    }

    /// <summary>
    /// Define un valor ELSE para el CASE.
    /// </summary>
    /// <param name="result">Valor por defecto si ninguna condición se cumple.</param>
    public CaseWhenBuilder Else(string result)
    {
        _elseValue = $"'{result}'";
        return this;
    }

    /// <summary>
    /// Construye la expresión SQL CASE WHEN completa.
    /// </summary>
    public string Build()
    {
        if (_cases.Count == 0)
            throw new InvalidOperationException("Debe haber al menos una condición WHEN.");

        var parts = new List<string> { "CASE" };
        foreach (var (condition, result) in _cases)
        {
            parts.Add($"WHEN {condition} THEN {result}");
        }

        if (_elseValue is not null)
            parts.Add($"ELSE {_elseValue}");

        parts.Add("END");
        return string.Join(" ", parts);
    }
/// <summary>
/// Agrega una o varias expresiones CASE WHEN al SELECT, con alias.
/// </summary>
/// <param name="cases">Tuplas con el builder CASE y su alias.</param>
public SelectQueryBuilder SelectCase(params (CaseWhenBuilder Case, string Alias)[] cases)
{
    foreach (var (builder, alias) in cases)
    {
        var expression = builder.Build();
        _columns.Add((expression, alias));
    }
    return this;
}



    var query = QueryBuilder.Core.QueryBuilder
    .From("USUARIOS", "MI_LIBRERIA")
    .Select("ID", "NOMBRE")
    .SelectCase(
        (
            new CaseWhenBuilder()
                .When<USUARIO>(x => x.TIPO == "A").Then("Administrador")
                .When<USUARIO>(x => x.TIPO == "U").Then("Usuario")
                .Else("Otro"),
            "TIPO_DESC"
        )
    )
    .Build();
