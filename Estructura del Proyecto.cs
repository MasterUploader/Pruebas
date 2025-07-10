using QueryBuilder.Interfaces;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace QueryBuilder.Engines
{
    /// <summary>
    /// Motor de generación SQL específico para AS400.
    /// </summary>
    public class As400SqlEngine : ISqlEngine
    {
        private readonly As400QueryTranslator _translator;

        public As400SqlEngine(As400QueryTranslator translator)
        {
            _translator = translator;
        }

        public string GenerateSelect<TModel>(Expression<Func<TModel, bool>>? filter = null)
        {
            return _translator.TranslateSelect(filter);
        }

        public string GenerateInsert<TModel>(TModel model)
        {
            return _translator.TranslateInsert(model);
        }

        public string GenerateUpdate<TModel>(TModel model, Expression<Func<TModel, bool>> filter)
        {
            return _translator.TranslateUpdate(model, filter);
        }

        public string GenerateMetadataQuery(string tableName)
        {
            return $"SELECT * FROM {tableName} FETCH FIRST 1 ROWS ONLY"; // o usa _translator si aplica
        }

        public IEnumerable<SqlParameterMetadata> GetParameterMetadata<TModel>(TModel model)
        {
            return _translator.ExtractParameterMetadata(model);
        }
    }
}
