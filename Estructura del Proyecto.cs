/// <summary>
/// Agrega una cláusula WHERE basada en una expresión CASE WHEN (como string).
/// </summary>
/// <param name="caseWhenCondition">Expresión CASE WHEN seguida de condición, e.g., "CASE WHEN ... END = 'X'"</param>
/// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
public SelectQueryBuilder WhereCase(string caseWhenCondition)
{
    if (string.IsNullOrWhiteSpace(WhereClause))
        WhereClause = caseWhenCondition;
    else
        WhereClause += $" AND {caseWhenCondition}";
    return this;
}

/// <summary>
/// Agrega una cláusula WHERE basada en una expresión CASE WHEN generada desde <see cref="CaseWhenBuilder"/>.
/// </summary>
/// <typeparam name="T">Tipo de entidad usado en la expresión.</typeparam>
/// <param name="caseWhenBuilder">Constructor CASE WHEN.</param>
/// <param name="condition">Condición a aplicar sobre la expresión CASE WHEN. Ej: " = 'X'"</param>
/// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
public SelectQueryBuilder WhereCase<T>(CaseWhenBuilder<T> caseWhenBuilder, string condition)
{
    return WhereCase($"{caseWhenBuilder.Build()} {condition}");
}

/// <summary>
/// Agrega una cláusula HAVING basada en una expresión CASE WHEN (como string).
/// </summary>
/// <param name="caseWhenCondition">Expresión CASE WHEN seguida de condición.</param>
/// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
public SelectQueryBuilder HavingCase(string caseWhenCondition)
{
    if (string.IsNullOrWhiteSpace(HavingClause))
        HavingClause = caseWhenCondition;
    else
        HavingClause += $" AND {caseWhenCondition}";
    return this;
}

/// <summary>
/// Agrega una cláusula HAVING basada en un CASE WHEN generado desde <see cref="CaseWhenBuilder"/>.
/// </summary>
/// <typeparam name="T">Tipo de entidad usado en la expresión.</typeparam>
/// <param name="caseWhenBuilder">Constructor CASE WHEN.</param>
/// <param name="condition">Condición a aplicar sobre la expresión CASE WHEN.</param>
/// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
public SelectQueryBuilder HavingCase<T>(CaseWhenBuilder<T> caseWhenBuilder, string condition)
{
    return HavingCase($"{caseWhenBuilder.Build()} {condition}");
}
