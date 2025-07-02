using RestUtilities.QueryBuilder.Interfaces;
using RestUtilities.QueryBuilder.Enums;
using System.Collections.Generic;
using System.Text;

namespace RestUtilities.QueryBuilder.Builders
{
    /// <summary>
    /// Constructor de consultas SQL del tipo SELECT.
    /// Permite construir dinámicamente sentencias SELECT con soporte para filtros, ordenamientos, paginación, joins, etc.
    /// </summary>
    public class SelectQueryBuilder : IQueryBuilder
    {
        private string _table;
        private readonly List<string> _columns = new();
        private readonly List<string> _whereConditions = new();
        private readonly List<string> _orderBy = new();
        private int? _offset;
        private int? _fetch;

        /// <inheritdoc/>
        public IQueryBuilder From(string tableName)
        {
            _table = tableName;
            return this;
        }

        /// <inheritdoc/>
        public IQueryBuilder Select(params string[] columns)
        {
            _columns.AddRange(columns);
            return this;
        }

        /// <inheritdoc/>
        public IQueryBuilder Where(string condition)
        {
            _whereConditions.Add(condition);
            return this;
        }

        /// <inheritdoc/>
        public IQueryBuilder OrderBy(string column, SqlSortDirection direction)
        {
            _orderBy.Add($"{column} {(direction == SqlSortDirection.Ascending ? "ASC" : "DESC")}");
            return this;
        }

        /// <inheritdoc/>
        public IQueryBuilder Offset(int offset)
        {
            _offset = offset;
            return this;
        }

        /// <inheritdoc/>
        public IQueryBuilder FetchNext(int size)
        {
            _fetch = size;
            return this;
        }

        /// <inheritdoc/>
        public string Build()
        {
            var sb = new StringBuilder();
            sb.Append("SELECT ");
            sb.Append(_columns.Count > 0 ? string.Join(", ", _columns) : "*");
            sb.Append(" FROM ").Append(_table);

            if (_whereConditions.Count > 0)
                sb.Append(" WHERE ").Append(string.Join(" AND ", _whereConditions));

            if (_orderBy.Count > 0)
                sb.Append(" ORDER BY ").Append(string.Join(", ", _orderBy));

            if (_offset.HasValue && _fetch.HasValue)
                sb.Append($" OFFSET {_offset.Value} ROWS FETCH NEXT {_fetch.Value} ROWS ONLY");

            return sb.ToString();
        }
    }
}

using System.Collections.Generic;
using System.Text;

namespace RestUtilities.QueryBuilder.Builders
{
    /// <summary>
    /// Constructor de consultas SQL del tipo INSERT.
    /// Permite construir dinámicamente sentencias INSERT INTO con columnas y valores parametrizados.
    /// </summary>
    public class InsertQueryBuilder
    {
        /// <summary>Nombre de la tabla destino.</summary>
        public string Table { get; set; }

        /// <summary>Lista de columnas a insertar.</summary>
        public List<string> Columns { get; set; } = new();

        /// <summary>Lista de valores asociados a las columnas.</summary>
        public List<string> Values { get; set; } = new();

        /// <summary>
        /// Construye la consulta SQL INSERT basada en los valores proporcionados.
        /// </summary>
        /// <returns>Consulta SQL generada.</returns>
        public string Build()
        {
            var sb = new StringBuilder();
            sb.Append($"INSERT INTO {Table} ({string.Join(", ", Columns)}) ");
            sb.Append($"VALUES ({string.Join(", ", Values)})");
            return sb.ToString();
        }
    }
}
