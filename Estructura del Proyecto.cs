Actualmente el código de UpdateQueryBuilder esta así:


using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de sentencias UPDATE compatibles con AS400 y otros motores.
/// Permite actualizar registros con columnas específicas y condiciones.
/// </summary>
public class UpdateQueryBuilder(string _tableName, string? _library = null)
{
    private readonly Dictionary<string, object?> _setColumns = new();
    private string? _whereClause;
    private string? _comment;

    /// <summary>
    /// Agrega un comentario SQL al inicio del UPDATE para trazabilidad o debugging.
    /// </summary>
    /// <param name="comment">Texto del comentario.</param>
    /// <returns>Instancia modificada de <see cref="UpdateQueryBuilder"/>.</returns>
    public UpdateQueryBuilder WithComment(string comment)
    {
        if (!string.IsNullOrWhiteSpace(comment))
            _comment = $"-- {comment}";
        return this;
    }

    /// <summary>
    /// Define una columna y su nuevo valor a actualizar.
    /// </summary>
    /// <param name="column">Nombre de la columna.</param>
    /// <param name="value">Valor a establecer.</param>
    /// <returns>Instancia modificada del builder.</returns>
    public UpdateQueryBuilder Set(string column, object? value)
    {
        _setColumns[column] = value;
        return this;
    }

    /// <summary>
    /// Agrega una cláusula WHERE para el UPDATE usando SQL crudo.
    /// </summary>
    /// <param name="sql">Condición SQL como cadena.</param>
    /// <returns>Instancia modificada del builder.</returns>
    public UpdateQueryBuilder Where(string sql)
    {
        _whereClause = sql;
        return this;
    }

    /// <summary>
    /// Agrega una cláusula WHERE para el UPDATE utilizando expresiones lambda.
    /// </summary>
    /// <typeparam name="T">Tipo de objeto sobre el cual se basa la expresión.</typeparam>
    /// <param name="expression">Expresión lambda que representa la condición WHERE.</param>
    /// <returns>Instancia modificada del builder.</returns>
    public UpdateQueryBuilder Where<T>(Expression<Func<T, bool>> expression)
    {
        _whereClause = ExpressionToSqlConverter.Convert(expression);
        return this;
    }

    /// <summary>
    /// Construye y retorna la consulta UPDATE generada.
    /// </summary>
    public QueryResult Build()
    {
        if (string.IsNullOrWhiteSpace(_tableName))
            throw new InvalidOperationException("Debe especificarse el nombre de la tabla para UPDATE.");

        if (_setColumns.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una columna para actualizar.");

        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(_comment))
            sb.AppendLine(_comment);

        var fullTable = string.IsNullOrWhiteSpace(_library) ? _tableName : $"{_library}.{_tableName}";
        sb.Append($"UPDATE {fullTable} SET ");

        var sets = _setColumns
            .Select(pair => $"{pair.Key} = {SqlHelper.FormatValue(pair.Value)}");

        sb.Append(string.Join(",", sets));

        if (!string.IsNullOrWhiteSpace(_whereClause))
        {
            sb.Append(" WHERE ");
            sb.Append(_whereClause);
        }

        return new QueryResult
        {
            Sql = sb.ToString()
        };
    }
}

Por lo tanto me dan los siguientes errores:

Severity	Code	Description	Project	File	Line	Suppression State	Details
Error (active)	CS1729	'UpdateQueryBuilder' does not contain a constructor that takes 3 arguments	MS_BAN_43_Embosado_Tarjetas_Debito	C:\Git\MS_BAN_43_EmbosadoTarjetasDebito\BACKEND\MS_BAN_43_Embosado_Tarjetas_Debito\Repository\IRepository\RegistraImpresion\RegistraImpresionRepository.cs	42		
Severity	Code	Description	Project	File	Line	Suppression State	Details
Error (active)	CS1061	'UpdateQueryBuilder' does not contain a definition for 'WhereEq' and no accessible extension method 'WhereEq' accepting a first argument of type 'UpdateQueryBuilder' could be found (are you missing a using directive or an assembly reference?)	MS_BAN_43_Embosado_Tarjetas_Debito	C:\Git\MS_BAN_43_EmbosadoTarjetasDebito\BACKEND\MS_BAN_43_Embosado_Tarjetas_Debito\Repository\IRepository\RegistraImpresion\RegistraImpresionRepository.cs	75		

Entonces mejora la clase UpdateQueryBuilder, siempre considerando que SqlDialect.Db2i debe ser opcional, ademas crea el método WhereEq que hace falta.
    Y tambien un método WhereNot, cuando el Where sea distinto, y si hay alguna otra opción incluyela.

    Entregame la clase completa.
