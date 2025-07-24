using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Constructor de consultas SQL que combina múltiples SELECT mediante UNION o UNION ALL.
/// </summary>
public class UnionQueryBuilder
{
    private readonly List<(QueryResult Query, bool IsUnionAll)> _queries = [];

    /// <summary>
    /// Agrega la primera consulta SELECT base para la combinación.
    /// </summary>
    /// <param name="query">Consulta SELECT generada con SelectQueryBuilder.</param>
    /// <param name="unionAll">Si es true, se utilizará UNION ALL; de lo contrario, UNION.</param>
    public UnionQueryBuilder Add(QueryResult query, bool unionAll = false)
    {
        if (query == null || string.IsNullOrWhiteSpace(query.Sql))
            throw new ArgumentException("La consulta base no puede ser nula ni vacía.");

        _queries.Add((query, unionAll));
        return this;
    }

    /// <summary>
    /// Combina todas las consultas agregadas mediante UNION o UNION ALL.
    /// </summary>
    /// <returns>Resultado final con la consulta SQL compuesta.</returns>
    public QueryResult Build()
    {
        if (_queries.Count == 0)
            throw new InvalidOperationException("Debe agregar al menos una consulta para combinar.");

        var sb = new StringBuilder();
        for (int i = 0; i < _queries.Count; i++)
        {
            if (i > 0)
                sb.Append(_queries[i].IsUnionAll ? " UNION ALL " : " UNION ");

            sb.Append("(");
            sb.Append(_queries[i].Query.Sql);
            sb.Append(")");
        }

        return new QueryResult { Sql = sb.ToString() };
    }
}


var q1 = new SelectQueryBuilder("CLIENTES").Select("ID", "NOMBRE").Build();
var q2 = new SelectQueryBuilder("USUARIOS").Select("ID", "NOMBRE").Build();

var union = new UnionQueryBuilder()
    .Add(q1)                 // UNION
    .Add(q2, unionAll: true) // UNION ALL
    .Build();

Console.WriteLine(union.Sql);
