public async Task<RespuestaListarComercioDto> ListarComercioAsync(ListarComerciosDto listarComercio)
{
    try
    {
        connection.Open();

        var pageSize = string.IsNullOrWhiteSpace(listarComercio.Size) || listarComercio.Size == "0" ? 1 : int.Parse(listarComercio.Size);
        var currentPage = string.IsNullOrWhiteSpace(listarComercio.Page) ? 0 : int.Parse(listarComercio.Page);
        var skip = currentPage * pageSize;

        string table = "PQR01L01";
        string library = "IS4TECHDTA";

        // COUNT
        var countQuery = new SelectQueryBuilder(table, library)
            .Select("COUNT(*)")
            .WhereRaw("1=1");

        // DATA
        var dataQuery = new SelectQueryBuilder(table, library)
            .Select("CLINOM", "CLINRO", "CLISTS")
            .WhereRaw("1=1");

        if (!string.IsNullOrWhiteSpace(listarComercio.Search))
        {
            if (int.TryParse(listarComercio.Search, out _))
            {
                countQuery.WhereRaw($"LOWER(CLINRO) LIKE LOWER('%{listarComercio.Search}%')");
                dataQuery.WhereRaw($"LOWER(CLINRO) LIKE LOWER('%{listarComercio.Search}%')")
                         .OrderBy("CLINRO")
                         .Offset(skip)
                         .FetchNext(pageSize);
            }
            else
            {
                countQuery.WhereRaw($"LOWER(CLINOM) LIKE LOWER('%{listarComercio.Search}%')");
                dataQuery.WhereRaw($"LOWER(CLINOM) LIKE LOWER('%{listarComercio.Search}%')")
                         .OrderBy("CLINOM")
                         .Offset(skip)
                         .FetchNext(pageSize);
            }
        }
        else
        {
            dataQuery.Offset(skip).FetchNext(pageSize);
        }

        if (!string.IsNullOrWhiteSpace(listarComercio.Status))
        {
            countQuery.WhereRaw($"CLISTS = '{listarComercio.Status}'");
            dataQuery.WhereRaw($"CLISTS = '{listarComercio.Status}'");
        }

        // Ejecutar COUNT
        var countResult = countQuery.Build();
        using var countCommand = connection.GetDbCommand(countResult, _httpContextAccessor.HttpContext!);
        var totalElements = Convert.ToInt32(await countCommand.ExecuteScalarAsync());

        if (totalElements == 0)
        {
            return new RespuestaListarComercioDto
            {
                ResponseCode = 1,
                ResponseDescription = "Busqueda no obtuvo datos"
            };
        }

        var totalPages = (int)Math.Ceiling((double)totalElements / pageSize);
        if (pageSize >= totalElements)
        {
            currentPage = 0;
            totalPages = 1;
        }
        else if (currentPage < 0 || currentPage >= totalPages)
        {
            currentPage = totalPages - 1;
        }

        // Ejecutar DATA
        var dataResult = dataQuery.Build();
        using var dataCommand = connection.GetDbCommand(dataResult, _httpContextAccessor.HttpContext!);
        using var reader = await dataCommand.ExecuteReaderAsync();

        var listaDeComercios = new List<RespuestaListarComercioDto.Content>();

        while (reader.Read())
        {
            listaDeComercios.Add(new RespuestaListarComercioDto.Content
            {
                Name = reader["CLINOM"].ToString(),
                Cif = reader["CLINRO"].ToString(),
                Status = reader["CLISTS"].ToString()
            });
        }

        return new RespuestaListarComercioDto
        {
            Contents = listaDeComercios.ToArray(),
            FirstPage = currentPage == 0,
            LastPage = currentPage == totalPages - 1,
            Page = currentPage,
            TotalPages = totalPages,
            TotalElements = totalElements,
            ResponseCode = 0,
            ResponseDescription = "Comercios Listados Correctamente"
        };
    }
    catch (Exception ex)
    {
        return new RespuestaListarComercioDto
        {
            ResponseCode = 666,
            ResponseDescription = ex.Message
        };
    }
}
