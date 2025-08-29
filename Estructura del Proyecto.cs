/// <summary>
/// Define la lista de columnas para el INSERT. El orden debe coincidir con los valores que se agreguen.
/// </summary>
/// <param name="columns">Columnas de la tabla, en el orden deseado.</param>
/// <exception cref="ArgumentException">Si no se especifica ninguna columna.</exception>
public InsertQueryBuilder IntoColumns(params string[] columns)
{
    if (columns == null || columns.Length == 0)
        throw new ArgumentException("Debe especificar al menos una columna.", nameof(columns));

    _columns.Clear();
    _columns.AddRange(columns);
    return this;
}
