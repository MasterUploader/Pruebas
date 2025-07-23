using System;
using System.Linq.Expressions;
using QueryBuilder.Helpers;
using QueryBuilder.Models;

namespace QueryBuilder.Translators;

/// <summary>
/// Traductor de expresiones lambda para cláusulas WHERE.
/// </summary>
public static class LambdaWhereTranslator
{
    /// <summary>
    /// Traduce una expresión lambda a SQL y la agrega al contexto.
    /// </summary>
    /// <typeparam name="T">Tipo de la entidad.</typeparam>
    /// <param name="context">Contexto de traducción de consulta.</param>
    /// <param name="expression">Expresión lambda booleana.</param>
    public static void Translate<T>(QueryTranslationContext context, Expression<Func<T, bool>> expression)
    {
        string condition = ExpressionToSqlConverter.Convert(expression);

        if (string.IsNullOrWhiteSpace(context.WhereClause))
            context.WhereClause = condition;
        else
            context.WhereClause += $" AND {condition}";
    }
}
