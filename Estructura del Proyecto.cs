// Dentro de MergeQueryBuilder
public MergeQueryBuilder UsingValuesTyped(params (string Column, Db2Typed Typed)[] row)
{
    if (row == null || row.Length == 0)
        throw new ArgumentException("Debe especificar al menos una columna/valor.");

    _sourceKind = MergeSourceKind.Values;       // <- marca que hay USING VALUES
    _sourceColumns = new List<string>();
    var rendered = new List<string>();

    foreach (var (col, typed) in row)
    {
        _sourceColumns.Add(col);
        rendered.Add(typed.SqlFragment);        // ej. "CAST(? AS VARCHAR(20))" o "TIMESTAMP(?)"
        _parameters.Add(typed.Value);           // añade el valor para el placeholder
    }

    _valuesRows.Clear();
    _valuesRows.Add(rendered);                  // al menos UNA fila en USING(VALUES)
    return this;
}
