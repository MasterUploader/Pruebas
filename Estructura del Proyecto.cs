using QueryBuilder.Attributes;
using QueryBuilder.Enums;
using QueryBuilder.Models;
using QueryBuilder.Utils;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace QueryBuilder.Utils;

/// <summary>
/// Clase utilitaria que construye objetos <see cref="QueryTranslationContext"/> a partir de expresiones o modelos.
/// </summary>
public static class QueryTranslationContextBuilder
{
    public static QueryTranslationContext BuildSelectContext<TModel>(Expression<Func<TModel, bool>>? filter = null)
    {
        var props = typeof(TModel).GetProperties()
            .Where(p => p.GetCustomAttribute<SqlColumnDefinitionAttribute>() != null)
            .ToList();

        return new QueryTranslationContext
        {
            TableName = SqlMetadataHelper.GetFullTableName<TModel>(),
            SelectColumns = props.Select(p => p.GetCustomAttribute<SqlColumnDefinitionAttribute>()!.ColumnName).ToList(),
            WhereClause = filter != null ? ExpressionParser.Parse(filter) : null
        };
    }

    public static QueryTranslationContext BuildInsertContext<TModel>(TModel model)
    {
        var props = typeof(TModel).GetProperties()
            .Where(p => p.GetCustomAttribute<SqlColumnDefinitionAttribute>() != null)
            .ToList();

        return new QueryTranslationContext
        {
            TableName = SqlMetadataHelper.GetFullTableName<TModel>(),
            InsertColumns = props.Select(p => p.GetCustomAttribute<SqlColumnDefinitionAttribute>()!.ColumnName).ToList(),
            ParameterValues = props.Select(p => p.GetValue(model)).ToList()
        };
    }

    public static QueryTranslationContext BuildUpdateContext<TModel>(TModel model, Expression<Func<TModel, bool>> filter)
    {
        var props = typeof(TModel).GetProperties()
            .Where(p => p.GetCustomAttribute<SqlColumnDefinitionAttribute>() != null)
            .ToList();

        return new QueryTranslationContext
        {
            TableName = SqlMetadataHelper.GetFullTableName<TModel>(),
            UpdateColumns = props.Select(p => p.GetCustomAttribute<SqlColumnDefinitionAttribute>()!.ColumnName).ToList(),
            ParameterValues = props.Select(p => p.GetValue(model)).ToList(),
            WhereClause = ExpressionParser.Parse(filter)
        };
    }

    public static QueryTranslationContext BuildMetadataContext(string tableName)
    {
        return new QueryTranslationContext
        {
            TableName = tableName,
            MetadataOnly = true
        };
    }

    public static SqlParameterMetadata[] ExtractParameterMetadata<TModel>(TModel model)
    {
        return typeof(TModel).GetProperties()
            .Where(p => p.GetCustomAttribute<SqlColumnDefinitionAttribute>() != null)
            .Select(p =>
            {
                var attr = p.GetCustomAttribute<SqlColumnDefinitionAttribute>()!;
                return new SqlParameterMetadata
                {
                    Name = attr.ColumnName,
                    DataType = attr.DataType,
                    Length = attr.Length,
                    Value = p.GetValue(model)
                };
            })
            .ToArray();
    }
}
