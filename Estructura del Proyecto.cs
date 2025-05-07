Task<AgenciaModel?> ObtenerAgenciaPorIdAsync(int codcco);


public async Task<AgenciaModel?> ObtenerAgenciaPorIdAsync(int codcco)
{
    try
    {
        using var command = _connection.GetDbCommand();
        command.CommandText = @"SELECT CODCCO, NOMAGE, ZONA, MARQUESINA, RSTBRANCH, NOMBD, NOMSER, IPSER 
                                FROM BCAH96DTA.RSAGE01 
                                WHERE CODCCO = ?";
        command.Parameters.Add(_connection.CreateParameter("CODCCO", codcco));

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new AgenciaModel
            {
                Codcco = reader.GetInt32(0),
                NomAge = reader.GetString(1),
                Zona = reader.GetInt32(2),
                Marquesina = reader.GetString(3) == "SI",
                RstBranch = reader.GetString(4) == "SI",
                NomBD = reader.GetString(5),
                NomSer = reader.GetString(6),
                IpSer = reader.GetString(7)
            };
        }

        return null; // No encontrada
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al obtener la agencia con CODCCO = {codcco}", codcco);
        return null;
    }
}
