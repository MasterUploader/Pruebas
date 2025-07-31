public async Task<RespuestaListarQRAplicadosDto> ListarQRAplicadosAsync(ListarQRAplicadosDto listarQRAplicadosDto)
{
    try
    {
        connection.Open();
        var fechaInicial = DateTime.ParseExact(listarQRAplicadosDto.DateInit, "yyyy-MM-dd HH:mm:ss", null);
        var fechaFinal = DateTime.ParseExact(listarQRAplicadosDto.DateEnd, "yyyy-MM-dd HH:mm:ss", null);

        var query = new SelectQueryBuilder("PQR02QRG", "IS4TECHDTA")
            .Join("PQR01CLI", "IS4TECHDTA", "C", "PQR02QRG.PQRCIF", "C.CLINRO")
            .Join("ENTIDAD", "ACH", "E", "PQR02QRG.PQRABA", "E.CODENT")
            .Select("*")
            .WhereRaw("1=1")
            .Where("PQRSTA = 'APL'");

        if (!string.IsNullOrEmpty(listarQRAplicadosDto.Cif))
            query.WhereRaw($"LOWER(PQRCIF) LIKE LOWER('%{listarQRAplicadosDto.Cif}%')");

        if (!string.IsNullOrEmpty(listarQRAplicadosDto.PointOfSaleId))
            query.WhereRaw($"PQRPVI IN ({listarQRAplicadosDto.PointOfSaleId})");

        if (!string.IsNullOrEmpty(listarQRAplicadosDto.CashierID))
            query.WhereRaw($"PQRCAI IN ({listarQRAplicadosDto.CashierID})");

        if (!string.IsNullOrEmpty(listarQRAplicadosDto.Reference))
            query.WhereRaw($"LOWER(PQRREG) LIKE LOWER('%{listarQRAplicadosDto.Reference}%')");

        if (!string.IsNullOrEmpty(listarQRAplicadosDto.Type))
            query.WhereRaw($"PQRQTI = '{listarQRAplicadosDto.Type}'");

        var result = query.Build();
        using var command = connection.GetDbCommand(result, _httpContextAccessor.HttpContext!);
        using var reader = await command.ExecuteReaderAsync();

        var resultados = new List<Dictionary<string, object>>();
        while (await reader.ReadAsync())
        {
            var fila = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                fila[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            resultados.Add(fila);
        }

        // Filtrado por fecha (aplicación)
        var resultadosFiltrados = resultados.Where(r =>
        {
            int dia = int.TryParse(r.GetValueOrDefault("PQRADI")?.ToString(), out var d) ? d : 0;
            int mes = int.TryParse(r.GetValueOrDefault("PQRAME")?.ToString(), out var m) ? m : 0;
            int anio = int.TryParse(r.GetValueOrDefault("PQRAAN")?.ToString(), out var a) ? a : 0;
            int horaRaw = int.TryParse(r.GetValueOrDefault("PQRAHO")?.ToString(), out var h) ? h : 0;

            var fecha = new DateTime(anio, mes, dia,
                horaRaw / 10000, (horaRaw / 100) % 100, horaRaw % 100);

            return fecha >= fechaInicial && fecha <= fechaFinal;
        });

        // Ordenamiento por fecha aplicada
        listarQRAplicadosDto.Sort ??= "Asc";
        resultadosFiltrados = listarQRAplicadosDto.Sort == "Desc"
            ? resultadosFiltrados.OrderByDescending(r =>
            {
                int dia = int.TryParse(r.GetValueOrDefault("PQRADI")?.ToString(), out var d) ? d : 0;
                int mes = int.TryParse(r.GetValueOrDefault("PQRAME")?.ToString(), out var m) ? m : 0;
                int anio = int.TryParse(r.GetValueOrDefault("PQRAAN")?.ToString(), out var a) ? a : 0;
                int horaRaw = int.TryParse(r.GetValueOrDefault("PQRAHO")?.ToString(), out var h) ? h : 0;
                return new DateTime(anio, mes, dia, horaRaw / 10000, (horaRaw / 100) % 100, horaRaw % 100);
            }).ToList()
            : resultadosFiltrados.OrderBy(r =>
            {
                int dia = int.TryParse(r.GetValueOrDefault("PQRADI")?.ToString(), out var d) ? d : 0;
                int mes = int.TryParse(r.GetValueOrDefault("PQRAME")?.ToString(), out var m) ? m : 0;
                int anio = int.TryParse(r.GetValueOrDefault("PQRAAN")?.ToString(), out var a) ? a : 0;
                int horaRaw = int.TryParse(r.GetValueOrDefault("PQRAHO")?.ToString(), out var h) ? h : 0;
                return new DateTime(anio, mes, dia, horaRaw / 10000, (horaRaw / 100) % 100, horaRaw % 100);
            }).ToList();

        var totalMonto = resultadosFiltrados.Sum(r =>
        {
            var montoRaw = r.GetValueOrDefault("PQRMTO")?.ToString();
            return decimal.TryParse(montoRaw, out var monto) ? monto : 0;
        });

        int totalRegistros = resultadosFiltrados.Count();
        int pageSize = string.IsNullOrEmpty(listarQRAplicadosDto.Size) || listarQRAplicadosDto.Size == "0" ? 1 : int.Parse(listarQRAplicadosDto.Size);
        int totalPaginas = (int)Math.Ceiling((double)totalRegistros / pageSize);
        int currentPage = string.IsNullOrEmpty(listarQRAplicadosDto.Page) ? 0 : int.Parse(listarQRAplicadosDto.Page);

        if (pageSize >= totalRegistros)
        {
            currentPage = 0;
            totalPaginas = 1;
        }
        else if (currentPage >= totalPaginas)
        {
            currentPage = totalPaginas - 1;
        }

        int inicio = currentPage * pageSize;
        int fin = Math.Min(inicio + pageSize, totalRegistros);
        var paginaDeResultados = resultadosFiltrados.Skip(inicio).Take(pageSize).ToList();

        var content = paginaDeResultados.Select(r =>
        {
            int diaC = int.TryParse(r.GetValueOrDefault("PQRCDI")?.ToString(), out var d) ? d : 0;
            int mesC = int.TryParse(r.GetValueOrDefault("PQRCME")?.ToString(), out var m) ? m : 0;
            int anioC = int.TryParse(r.GetValueOrDefault("PQRCAN")?.ToString(), out var a) ? a : 0;
            int horaC = int.TryParse(r.GetValueOrDefault("PQRCHO")?.ToString(), out var h) ? h : 0;

            int diaA = int.TryParse(r.GetValueOrDefault("PQRADI")?.ToString(), out var da) ? da : 0;
            int mesA = int.TryParse(r.GetValueOrDefault("PQRAME")?.ToString(), out var ma) ? ma : 0;
            int anioA = int.TryParse(r.GetValueOrDefault("PQRAAN")?.ToString(), out var aa) ? aa : 0;
            int horaA = int.TryParse(r.GetValueOrDefault("PQRAHO")?.ToString(), out var ha) ? ha : 0;

            string cuenta = r.GetValueOrDefault("PQRCTA")?.ToString() ?? "";

            return new RespuestaListarQRAplicadosDto.Content
            {
                CreationDate = $"{diaC:D2}/{mesC:D2}/{anioC} {horaC / 10000:D2}:{(horaC / 100) % 100:D2}:{horaC % 100:D2}",
                QrId = r.GetValueOrDefault("PQRQID")?.ToString() ?? "",
                Type = r.GetValueOrDefault("PQRQTI")?.ToString() ?? "",
                Cif = r.GetValueOrDefault("PQRCIF")?.ToString() ?? "",
                CifName = r.GetValueOrDefault("CLINOM")?.ToString() ?? "",
                CreationUser = r.GetValueOrDefault("PQRUMA")?.ToString() ?? "",
                PointOfSaleID = Convert.ToDecimal(r.GetValueOrDefault("PQRPVI") ?? 0),
                PointOfSaleDes = r.GetValueOrDefault("PQRPVD")?.ToString() ?? "",
                CashierId = Convert.ToDecimal(r.GetValueOrDefault("PQRCAI") ?? 0),
                CashierDes = r.GetValueOrDefault("PQRCAD")?.ToString() ?? "",
                Account = cuenta.Length > 6 ? new string('*', 6) + cuenta.Substring(6) : new string('*', cuenta.Length),
                Reference = r.GetValueOrDefault("PQRREG")?.ToString() ?? "",
                AppliedReference = r.GetValueOrDefault("PQRARE")?.ToString() ?? "",
                BankName = r.GetValueOrDefault("DESENT")?.ToString() ?? "",
                OriginatingAccountName = r.GetValueOrDefault("PQRNCO")?.ToString() ?? "",
                Currency = Convert.ToDecimal(r.GetValueOrDefault("PQRMON") ?? 0),
                Amount = Convert.ToDecimal(r.GetValueOrDefault("PQRMTO") ?? 0),
                AppliedDate = $"{diaA:D2}/{mesA:D2}/{anioA} {horaA / 10000:D2}:{(horaA / 100) % 100:D2}:{horaA % 100:D2}",
                Status = "APL"
            };
        }).ToArray();

        return new RespuestaListarQRAplicadosDto
        {
            content = content,
            FirstPage = currentPage == 0,
            LastPage = currentPage == totalPaginas - 1,
            Page = currentPage,
            TotalPages = totalPaginas,
            TotalElements = totalRegistros,
            TotalAmount = totalMonto.ToString(),
            ResponseCode = 0,
            ResponseDescription = "Se obtuvo la lista de QR generados"
        };
    }
    catch (Exception ex)
    {
        return new RespuestaListarQRAplicadosDto
        {
            ResponseCode = 1,
            ResponseDescription = ex.Message,
            content = Array.Empty<RespuestaListarQRAplicadosDto.Content>()
        };
    }
}            int año = int.TryParse(result.GetValueOrDefault("PQRAAN")?.ToString(), out var tempAñoApl) ? tempAñoApl : 0;
            int horaApplied = int.TryParse(result.GetValueOrDefault("PQRAHO")?.ToString(), out var tempHoraApl) ? tempHoraApl : 0;

            int horaAPL = horaApplied / 10000;
            int minutoAPL = (horaApplied / 100) % 100;
            int segundoAPL = horaApplied % 100;

            string cuenta = result.GetValueOrDefault("PQRCTA")?.ToString() ?? string.Empty;

            var contenidoitems = new RespuestaListarQRAplicadosDto.Content
            {

                CreationDate = $"{diacreacion:D2}/{mescreacion:D2}/{añocreacion} {hora:D2}:{minuto:D2}:{segundo:D2}",
                QrId = result.GetValueOrDefault("PQRQID")?.ToString() ?? string.Empty,
                Type = result.GetValueOrDefault("PQRQTI")?.ToString() ?? string.Empty,
                Cif = result.GetValueOrDefault("PQRCIF")?.ToString() ?? string.Empty,
                CifName = result.GetValueOrDefault("CLINOM")?.ToString() ?? string.Empty,
                CreationUser = result.GetValueOrDefault("PQRUMA")?.ToString() ?? string.Empty,
                PointOfSaleID = Convert.ToDecimal(result.GetValueOrDefault("PQRPVI")?.ToString() ?? string.Empty),
                PointOfSaleDes = result.GetValueOrDefault("PQRPVD")?.ToString() ?? string.Empty,
                CashierId = Convert.ToDecimal(result.GetValueOrDefault("PQRCAI")?.ToString() ?? string.Empty),
                CashierDes = result.GetValueOrDefault("PQRCAD")?.ToString() ?? string.Empty,
                Account = cuenta.Length > 6 ? new string('*', 6) + cuenta.Substring(6) : new string('*', cuenta.Length),
                Reference = result.GetValueOrDefault("PQRREG")?.ToString() ?? string.Empty,
                AppliedReference = result.GetValueOrDefault("PQRARE")?.ToString() ?? string.Empty,
                BankName = result.GetValueOrDefault("DESENT")?.ToString() ?? string.Empty,
                OriginatingAccountName = result.GetValueOrDefault("PQRNCO")?.ToString() ?? string.Empty,
                Currency = Convert.ToDecimal(result.GetValueOrDefault("PQRMON")?.ToString() ?? string.Empty),
                Amount = decimal.Parse(result.GetValueOrDefault("PQRMTO")?.ToString() ?? string.Empty),
                AppliedDate = $"{dia:D2}/{mes:D2}/{año} {horaAPL:D2}:{minutoAPL:D2}:{segundoAPL:D2}",
                Status = "APL"
            };


            respuestDto.content[i] = contenidoitems;
            respuestDto.FirstPage = Convert.ToInt32(listarQRAplicadosDto.Page) == 0;
            respuestDto.LastPage = Convert.ToInt32(listarQRAplicadosDto.Page) == totalPaginas - 1;
            respuestDto.Page = Convert.ToInt32(listarQRAplicadosDto.Page);
            respuestDto.TotalPages = totalPaginas;
            respuestDto.TotalElements = totalRegistros;
            respuestDto.ResponseCode = 0;
            respuestDto.ResponseDescription = "Se obtuvo la lista de QR generados";
            respuestDto.TotalAmount = TotalMonto.ToString();

        }

        return respuestDto;

    }
    catch (Exception ex)
    {
        return new RespuestaListarQRAplicadosDto
        {
            ResponseCode = 1,
            ResponseDescription = ex.Message,
            content = Array.Empty<RespuestaListarQRAplicadosDto.Content>(),

        };

    }
}
