/// <summary>
/// Obtiene la lista completa de agencias desde la tabla RSAGE01 en AS400.
/// </summary>
/// <returns>Una lista de objetos AgenciaModel.</returns>
public async Task<List<AgenciaModel>> ObtenerAgenciasAsync()
{
    var agencias = new List<AgenciaModel>();

    try
    {
        _as400.Open();
        using var command = _as400.GetDbCommand();

        command.CommandText = @"
            SELECT CODCCO, NOMAGE, NOMBD, NOMSER, IPSER, ZONA, MARQUESINA, RSTBRANCH
            FROM BCAH96DTA.RSAGE01
            ORDER BY CODCCO";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            agencias.Add(new AgenciaModel
            {
                Codcco = reader["CODCCO"].ToString(),
                Nombre = reader["NOMAGE"].ToString(),
                NombreBD = reader["NOMBD"].ToString(),
                NombreServidor = reader["NOMSER"].ToString(),
                IpServidor = reader["IPSER"].ToString(),
                Zona = Convert.ToInt32(reader["ZONA"]),
                Marquesina = reader["MARQUESINA"].ToString(),
                RstBranch = reader["RSTBRANCH"].ToString()
            });
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error al obtener agencias: " + ex.Message);
    }
    finally
    {
        _as400.Close();
    }

    return agencias;
}

/// <summary>
/// Verifica si ya existe una agencia registrada con el centro de costo especificado.
/// </summary>
/// <param name="codcco">Centro de costo a verificar</param>
/// <returns>True si existe, False si no existe o ocurre un error</returns>
public bool ExisteCentroCosto(string codcco)
{
    try
    {
        _as400.Open();
        using var command = _as400.GetDbCommand();

        command.CommandText = $@"
            SELECT 1 
            FROM BCAH96DTA.RSAGE01 
            WHERE CODCCO = '{codcco}'";

        var result = command.ExecuteScalar();
        return result != null;
    }
    catch
    {
        return false; // En caso de error asumimos que no existe
    }
    finally
    {
        _as400.Close();
    }
}
