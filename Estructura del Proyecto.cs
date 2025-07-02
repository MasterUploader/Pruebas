/// <summary>
/// Genera una consulta SQL que devuelve metadatos de la tabla (nombres y tipos de columnas).
/// </summary>
/// <param name="tableName">Nombre de la tabla.</param>
/// <returns>Consulta SQL para obtener metadatos.</returns>
string BuildMetadataQuery(string tableName);

/// <summary>
/// Genera una sentencia SQL que consulta los metadatos de una tabla.
/// </summary>
/// <param name="tableName">Nombre de la tabla.</param>
/// <returns>Consulta SQL para obtener metadata (campos, tipos, etc.).</returns>
string GenerateMetadataQuery(string tableName);

public string BuildMetadataQuery(string tableName)
{
    return _sqlEngine.GenerateMetadataQuery(tableName);
}

public string GenerateMetadataQuery(string tableName)
{
    return $@"
        SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = '{tableName.Replace("'", "''")}'
    ";
    }

/// <summary>
/// Genera una consulta SQL que obtiene los metadatos de la tabla especificada.
/// </summary>
/// <param name="tableName">Nombre de la tabla.</param>
/// <returns>Cadena con la consulta SQL que obtiene la metadata.</returns>
public string BuildMetadataQuery(string tableName)
{
    return _queryBuilder.BuildMetadataQuery(tableName);
}
