Lo que tengo es esto

    /// <summary>
    /// Define la fila de <c>USING (VALUES ...)</c> con **placeholders tipados para DB2 for i**.
    /// <para>
    /// Ejemplo:
    /// <code>
    /// .UsingValuesTyped(
    ///     ("UID",  Db2iTyped.VarChar(userId, 20)),
    ///     ("NOWTS",Db2iTyped.Timestamp(now)),
    ///     ("EXI",  Db2iTyped.Char(exitoso, 1)),
    ///     ("IP",   Db2iTyped.VarChar(ip, 64)),
    ///     ("DEV",  Db2iTyped.VarChar(device, 64)),
    ///     ("BRO",  Db2iTyped.VarChar(browser, 64)),
    ///     ("TOK",  Db2iTyped.VarChar(token, 512))
    /// )
    /// </code>
    /// Esto genera:
    /// <c>USING (VALUES(CAST(? AS VARCHAR(20)), TIMESTAMP(?), CAST(? AS CHAR(1)), ...)) AS S(UID, NOWTS, EXI, ...)</c>
    /// y agrega los valores a <see cref="QueryResult.Parameters"/> en el mismo orden.
    /// </para>
    /// </summary>
    /// <param name="values">
    /// Pares (NombreDeColumna, ValorTipado) para la tabla fuente <c>S</c>.
    /// El orden define el orden de columnas y de parámetros.
    /// </param>
    /// <returns>El propio <see cref="MergeQueryBuilder"/> para encadenamiento.</returns>
    /// <exception cref="ArgumentException">Si no se envía ningún valor.</exception>
    public MergeQueryBuilder UsingValuesTyped(params (string Column, Db2ITyped Value)[] values)
    {
        if (values == null || values.Length == 0)
            throw new ArgumentException("Debe especificar al menos un valor para USING (VALUES ...).", nameof(values));

        _usingSourceColumns.Clear();
        _usingValueSql.Clear();

        foreach (var (col, val) in values)
        {
            if (string.IsNullOrWhiteSpace(col))
                throw new ArgumentException("El nombre de columna en USING no puede ser vacío.", nameof(values));

            _usingSourceColumns.Add(col);
            _usingValueSql.Add(val.Sql);     // ej. CAST(? AS VARCHAR(20)) / TIMESTAMP(?)
            _parameters.Add(val.Value);      // valor para el marcador ?
        }

        return this;
    }
