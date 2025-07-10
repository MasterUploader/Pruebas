using QueryBuilder.Interfaces;
using QueryBuilder.Models;
using System;
using System.Linq.Expressions;

namespace QueryBuilder.Engines;

/// <summary>
/// Motor SQL específico para AS400 (DB2), basado en un traductor genérico.
/// </summary>
public class As400SqlEngine : ISqlEngine
{
    private readonly IQueryTranslator _translator;

    public As400SqlEngine(IQueryTranslator translator)
    {
        _translator = translator;
    }

    public string GenerateSelectQuery<TModel>(Expression<Func<TModel, bool>>? filter = null)
    {
        var context = QueryTranslationContextBuilder.BuildSelectContext(filter);
        return _translator.Translate(context);
    }

    public string GenerateInsertQuery<TModel>(TModel model)
    {
        var context = QueryTranslationContextBuilder.BuildInsertContext(model);
        return _translator.Translate(context);
    }

    public string GenerateUpdateQuery<TModel>(TModel model, Expression<Func<TModel, bool>> filter)
    {
        var context = QueryTranslationContextBuilder.BuildUpdateContext(model, filter);
        return _translator.Translate(context);
    }

    public string GenerateMetadataQuery(string tableName)
    {
        var context = QueryTranslationContextBuilder.BuildMetadataContext(tableName);
        return _translator.Translate(context);
    }

    public SqlParameterMetadata[] ExtractParameterMetadata<TModel>(TModel model)
    {
        return QueryTranslationContextBuilder.ExtractParameterMetadata(model);
    }
}
