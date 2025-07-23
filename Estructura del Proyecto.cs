using QueryBuilder.Builders;
using QueryBuilder.Helpers;
using System;
using System.Linq.Expressions;

namespace QueryBuilder.Expressions;

/// <summary>
/// Traductor para expresiones lambda en cláusulas HAVING.
/// </summary>
public static class LambdaHavingTranslator
{
    /// <summary>
    /// Traduce la expresión y la agrega como HAVING.
    /// </summary>
    public static void Translate<T>(SelectQueryBuilder builder, Expression<Func<T, bool>> expression)
    {
        string condition = ExpressionToSqlConverter.Convert(expression);
        builder.HavingClause = condition;
    }
}
