// File: Engines/ISqlEngine.cs
namespace QueryBuilder.Engines;

/// <summary>
/// Contrato para definir métodos que generan sentencias SQL específicas para cada motor.
/// </summary>
public interface ISqlEngine
{
    string GenerateSelectQuery<TModel>(System.Linq.Expressions.Expression<System.Func<TModel, bool>>? filter = null);
    string GenerateInsertQuery<TModel>(TModel insertValues);
    string GenerateUpdateQuery<TModel>(TModel updateValues, System.Linq.Expressions.Expression<System.Func<TModel, bool>> filter);
}


string BuildSelectQuery<TModel>(Expression<Func<TModel, bool>>? filter = null);
string BuildUpdateQuery<TModel>(TModel updateValues, Expression<Func<TModel, bool>> filter);


public string BuildSelectQuery<TModel>(Expression<Func<TModel, bool>>? filter = null)
{
    return _engine.GenerateSelectQuery(filter);
}

