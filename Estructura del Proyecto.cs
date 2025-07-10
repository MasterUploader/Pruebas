/// <summary>
/// Genera una consulta SQL para obtener información de las columnas de una tabla en AS400.
/// Utiliza la vista QSYS2.SYSCOLUMNS.
/// </summary>
/// <param name="tableName">Nombre completo de la tabla (puede incluir biblioteca).</param>
/// <returns>Consulta SQL para recuperar metadatos de columnas.</returns>
public string GenerateMetadataQuery(string tableName)
{
    // Separar biblioteca y tabla si viene como LIBRERIA.TABLA
    var parts = tableName.Split('.');
    string schema = parts.Length == 2 ? parts[0] : "*LIBL";
    string table = parts.Length == 2 ? parts[1] : parts[0];

    return $@"
        SELECT COLUMN_NAME, DATA_TYPE, LENGTH, NUMERIC_SCALE, IS_NULLABLE
        FROM QSYS2.SYSCOLUMNS
        WHERE TABLE_SCHEMA = '{schema}'
          AND TABLE_NAME = '{table}'
        ORDER BY ORDINAL_POSITION";
}
