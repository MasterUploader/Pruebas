using System;
using System.Collections.Generic;
using System.Linq;

namespace QueryBuilder.Builders;

/// <summary>
/// Parte de <see cref="MergeQueryBuilder"/> que agrega las cláusulas
/// <c>WHEN MATCHED THEN UPDATE</c> y <c>WHEN NOT MATCHED THEN INSERT</c>.
/// </summary>
public partial class MergeQueryBuilder
{
    // NOTA: Estas listas se asumen ya declaradas en la otra parte de la clase.
    // Si aún no las tienes, decláralas allí:
    // private readonly List<string> _updateAssignments = [];
    // private readonly List<string> _insertColumns = [];
    // private readonly List<string> _insertValues = [];

    /// <summary>
    /// Agrega asignaciones para <c>WHEN MATCHED THEN UPDATE SET ...</c>.
    /// Cada item debe venir como una asignación SQL válida, por ejemplo:
    /// <c>"T.COL1 = S.COL1"</c>, <c>"T.COUNT = T.COUNT + 1"</c>, etc.
    /// </summary>
    /// <param name="assignments">
    /// Lista de asignaciones ya formateadas. Se validará que cada una contenga un <c>=</c>.
    /// </param>
    /// <returns>El mismo <see cref="MergeQueryBuilder"/> para encadenamiento.</returns>
    /// <exception cref="ArgumentException">Si alguna asignación no contiene <c>=</c>.</exception>
    public MergeQueryBuilder WhenMatchedUpdate(params string[] assignments)
    {
        if (assignments is null || assignments.Length == 0) return this;

        foreach (var raw in assignments)
        {
            var a = raw?.Trim();
            if (string.IsNullOrWhiteSpace(a)) continue;

            if (!a.Contains('='))
                throw new ArgumentException($"La asignación '{a}' no contiene '='.", nameof(assignments));

            _updateAssignments.Add(a);
        }

        return this;
    }

    /// <summary>
    /// Variante tipada: recibe un diccionario <c>columna → expresión</c> y arma
    /// <c>T.columna = expresión</c>. Si la columna ya viene calificada (ej. <c>X.COL</c>)
    /// no se antepone alias.
    /// </summary>
    /// <param name="setMap">Mapa de columna destino a expresión SQL (sin comillas extras).</param>
    /// <param name="targetAlias">Alias del target; por defecto <c>T</c>.</param>
    /// <returns>El mismo <see cref="MergeQueryBuilder"/>.</returns>
    public MergeQueryBuilder WhenMatchedUpdate(Dictionary<string, string> setMap, string targetAlias = "T")
    {
        if (setMap is null || setMap.Count == 0) return this;

        foreach (var kvp in setMap)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
                throw new ArgumentException("Nombre de columna vacío en WhenMatchedUpdate(map).", nameof(setMap));

            var col = kvp.Key.Trim();
            var expr = (kvp.Value ?? "NULL").Trim();
            var lhs = col.Contains('.') ? col : $"{targetAlias}.{col}";

            _updateAssignments.Add($"{lhs} = {expr}");
        }

        return this;
    }

    /// <summary>
    /// Define las columnas y los valores para <c>WHEN NOT MATCHED THEN INSERT (... ) VALUES (...)</c>.
    /// La cantidad de columnas y valores debe coincidir.
    /// </summary>
    /// <param name="columns">Lista de columnas de destino.</param>
    /// <param name="values">
    /// Lista de valores/expresiones SQL correspondientes (por ejemplo <c>"S.UID"</c>, <c>"CURRENT_TIMESTAMP"</c>,
    /// <c>"CASE WHEN ... END"</c>, etc.).
    /// </param>
    /// <returns>El mismo <see cref="MergeQueryBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException">Si <paramref name="columns"/> o <paramref name="values"/> son nulos.</exception>
    /// <exception cref="InvalidOperationException">Si las cantidades no coinciden o no hay columnas.</exception>
    public MergeQueryBuilder WhenNotMatchedInsert(IEnumerable<string> columns, IEnumerable<string> values)
    {
        if (columns is null) throw new ArgumentNullException(nameof(columns));
        if (values  is null) throw new ArgumentNullException(nameof(values));

        _insertColumns.Clear();
        _insertValues.Clear();

        _insertColumns.AddRange(columns.Select(c => c?.Trim()).Where(c => !string.IsNullOrWhiteSpace(c))!);
        _insertValues.AddRange(values.Select(v => v?.Trim()).Where(v => !string.IsNullOrWhiteSpace(v))!);

        if (_insertColumns.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una columna para INSERT.");
        if (_insertColumns.Count != _insertValues.Count)
            throw new InvalidOperationException(
                $"La cantidad de columnas ({_insertColumns.Count}) no coincide con los valores ({_insertValues.Count}).");

        return this;
    }

    /// <summary>
    /// Atajo: recibe un diccionario <c>columna → valor/expresión</c> y construye
    /// el par de listas para <c>INSERT (... ) VALUES (...)</c>.
    /// </summary>
    /// <param name="colValues">Mapa de columnas a valores/expresiones SQL.</param>
    /// <returns>El mismo <see cref="MergeQueryBuilder"/>.</returns>
    /// <exception cref="ArgumentException">Si el diccionario viene vacío.</exception>
    public MergeQueryBuilder WhenNotMatchedInsert(Dictionary<string, string> colValues)
    {
        if (colValues is null || colValues.Count == 0)
            throw new ArgumentException("Debe especificar al menos una columna/valor para INSERT.", nameof(colValues));

        return WhenNotMatchedInsert(colValues.Keys, colValues.Values);
    }

    /// <summary>
    /// Atajo con tuplas: define <c>INSERT</c> pasando pares <c>(Columna, Valor)</c>.
    /// </summary>
    /// <param name="pairs">Pares de columna y su valor/expresión SQL.</param>
    /// <returns>El mismo <see cref="MergeQueryBuilder"/>.</returns>
    public MergeQueryBuilder WhenNotMatchedInsert(params (string Column, string Value)[] pairs)
        => pairs is null || pairs.Length == 0
            ? this
            : WhenNotMatchedInsert(pairs.Select(p => p.Column), pairs.Select(p => p.Value));
}
}
