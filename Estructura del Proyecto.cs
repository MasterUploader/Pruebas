using System;
using System.Linq.Expressions;
using QueryBuilder.Helpers;
using QueryBuilder.Builders;

namespace QueryBuilder.Translators;

/// <summary>
/// Traductor de expresiones lambda para cláusulas WHERE.
/// </summary>
public static class LambdaWhereTranslator
{
    /// <summary>
    /// Traduce una expresión lambda a SQL y la agrega al builder.
    /// </summary>
    /// <typeparam name="T">Tipo de la entidad.</typeparam>
    /// <param name="builder">Instancia de SelectQueryBuilder.</param>
    /// <param name="expression">Expresión lambda booleana.</param>
    public static void Translate<T>(SelectQueryBuilder builder, Expression<Func<T, bool>> expression)
    {
        string condition = ExpressionToSqlConverter.Convert(expression);

        if (string.IsNullOrWhiteSpace(builder.WhereClause))
            builder.WhereClause = condition;
        else
            builder.WhereClause += $" AND {condition}";
    }
}

using System;
using System.Linq.Expressions;
using QueryBuilder.Translators;

namespace QueryBuilder.Builders;

/// <summary>
/// Constructor de consultas SELECT SQL.
/// </summary>
public class SelectQueryBuilder
{
    /// <summary>
    /// Cláusula WHERE acumulada.
    /// </summary>
    internal string? WhereClause { get; set; }

    // ...otras propiedades internas como Tabla, Columnas, Orden, etc.

    /// <summary>
    /// Agrega una condición WHERE a la consulta.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad (para acceso tipado).</typeparam>
    /// <param name="expression">Expresión lambda booleana a traducir.</param>
    /// <returns>Instancia del builder para encadenamiento fluido.</returns>
    public SelectQueryBuilder Where<T>(Expression<Func<T, bool>> expression)
    {
        LambdaWhereTranslator.Translate(this, expression);
        return this;
    }
}
