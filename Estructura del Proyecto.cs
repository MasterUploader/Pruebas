// ====== WHERE / HAVING (RAW) ===============================================

/// <summary>
/// Agrega una condición en SQL crudo a la cláusula <c>WHERE</c>.
/// Útil cuando necesitas invocar funciones del motor o escribir predicados
/// que no están cubiertos por los helpers del builder.
/// </summary>
/// <param name="rawSql">
/// Predicado SQL válido, por ejemplo:
/// <code>"UPPER(NOMBRE) LIKE 'A%'"</code>,
/// <code>"COALESCE(IMPORTE,0) &gt; 0"</code>,
/// <code>"(FECHA BETWEEN '2025-01-01' AND '2025-12-31')"</code>.
/// </param>
/// <param name="logicalOperator">
/// Operador lógico para encadenar cuando ya existe un WHERE previo:
/// <c>"AND"</c> (por defecto) o <c>"OR"</c>. Se ignora si es el primer predicado.
/// </param>
/// <param name="wrapWithParentheses">
/// Si es <c>true</c>, envuelve <paramref name="rawSql"/> entre paréntesis
/// para preservar la precedencia (<c>( ... )</c>).
/// </param>
/// <remarks>
/// <para>
/// ⚠️ <b>Seguridad:</b> este método concatena SQL literal. No inyectes valores
/// proporcionados por el usuario sin sanitizarlos/parametrizarlos.
/// Si trabajas con <c>RestUtilities.QueryBuilder</c> y tu capa de conexión admite
/// parámetros, prefiere placeholders o helpers que formateen valores.
/// </para>
/// <para>
/// <b>Ejemplos</b>
/// <code>
/// new SelectQueryBuilder("BCAH96DTA.USUADMIN")
///     .Select("*")
///     .WhereRaw("TRIM(USUARIO) &lt;&gt; ''")                  // primer predicado
///     .WhereRaw("UPPER(ESTADO) = 'A'")                     // AND por defecto
///     .WhereRaw("EXISTS (SELECT 1 FROM T WHERE T.ID = X)", "OR", true);
/// </code>
/// </para>
/// </remarks>
public SelectQueryBuilder WhereRaw(string rawSql, string logicalOperator = "AND", bool wrapWithParentheses = false)
{
    if (string.IsNullOrWhiteSpace(rawSql))
        return this;

    var predicate = wrapWithParentheses ? $"({rawSql})" : rawSql;

    if (string.IsNullOrWhiteSpace(WhereClause))
    {
        WhereClause = predicate;
    }
    else
    {
        var op = string.Equals(logicalOperator, "OR", StringComparison.OrdinalIgnoreCase) ? "OR" : "AND";
        WhereClause = $"{WhereClause} {op} {predicate}";
    }

    return this;
}

/// <summary>
/// Atajo para <see cref="WhereRaw(string, string, bool)"/> con operador <c>AND</c>.
/// </summary>
public SelectQueryBuilder AndWhereRaw(string rawSql, bool wrapWithParentheses = false)
    => WhereRaw(rawSql, "AND", wrapWithParentheses);

/// <summary>
/// Atajo para <see cref="WhereRaw(string, string, bool)"/> con operador <c>OR</c>.
/// </summary>
public SelectQueryBuilder OrWhereRaw(string rawSql, bool wrapWithParentheses = false)
    => WhereRaw(rawSql, "OR", wrapWithParentheses);

/// <summary>
/// Agrega una condición en SQL crudo a la cláusula <c>HAVING</c>.
/// Útil para filtros sobre agregados cuando necesitas escribir la expresión literal.
/// </summary>
/// <param name="rawSql">Predicado SQL válido, p.ej. <c>"SUM(MONTO) &gt; 0"</c>.</param>
/// <param name="logicalOperator">
/// Operador lógico para encadenar cuando ya existe un HAVING previo:
/// <c>"AND"</c> (por defecto) o <c>"OR"</c>.
/// </param>
/// <param name="wrapWithParentheses">
/// Si es <c>true</c>, envuelve <paramref name="rawSql"/> entre paréntesis.
/// </param>
/// <remarks>
/// <b>Ejemplo:</b>
/// <code>
/// new SelectQueryBuilder("VENTAS")
///     .Select("CODIGO", "SUM(MONTO) AS TOTAL")
///     .GroupBy("CODIGO")
///     .HavingRaw("SUM(MONTO) &gt; 1000");
/// </code>
/// </remarks>
public SelectQueryBuilder HavingRaw(string rawSql, string logicalOperator = "AND", bool wrapWithParentheses = false)
{
    if (string.IsNullOrWhiteSpace(rawSql))
        return this;

    var predicate = wrapWithParentheses ? $"({rawSql})" : rawSql;

    if (string.IsNullOrWhiteSpace(HavingClause))
    {
        HavingClause = predicate;
    }
    else
    {
        var op = string.Equals(logicalOperator, "OR", StringComparison.OrdinalIgnoreCase) ? "OR" : "AND";
        HavingClause = $"{HavingClause} {op} {predicate}";
    }

    return this;
}

/// <summary>
/// Atajo para <see cref="HavingRaw(string, string, bool)"/> con operador <c>AND</c>.
/// </summary>
public SelectQueryBuilder AndHavingRaw(string rawSql, bool wrapWithParentheses = false)
    => HavingRaw(rawSql, "AND", wrapWithParentheses);

/// <summary>
/// Atajo para <see cref="HavingRaw(string, string, bool)"/> con operador <c>OR</c>.
/// </summary>
public SelectQueryBuilder OrHavingRaw(string rawSql, bool wrapWithParentheses = false)
    => HavingRaw(rawSql, "OR", wrapWithParentheses);
