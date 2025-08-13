public async Task<RespuestaListarQRAplicadosDto> ListarQRAplicadosAsync(ListarQRAplicadosDto listarQRAplicadosDto)
{
    try
    {
        _connection.Open();

        // Ventana de fechas (para filtrar en memoria como en tu versión)
        var fechaInicial = DateTime.ParseExact(listarQRAplicadosDto.DateInit, "yyyy-MM-dd HH:mm:ss", null);
        var fechaFinal   = DateTime.ParseExact(listarQRAplicadosDto.DateEnd,  "yyyy-MM-dd HH:mm:ss", null);

        // Builder con JOIN simplificado y condiciones PARAMETRIZADAS
        var builder = QueryBuilder.Core.QueryBuilder
            .From("PQR02QRG", "IS4TECHDTA")
            .Select("*")
            .Join("PQR01CLI", "PQR02QRG.PQRCIF = PQR01CLI.CLINRO")          // usa IS4TECHDTA por defecto
            .Join("ACH.ENTIDAD", "PQR02QRG.PQRABA = ENTIDAD.CODENT")         // esquema distinto
            .WhereEq("PQRSTA", "APL");                                       // parametrizado

        // Filtros opcionales (parametrizados)
        if (!string.IsNullOrWhiteSpace(listarQRAplicadosDto.Cif))
            builder.WhereLike("PQRCIF", $"%{listarQRAplicadosDto.Cif}%", lower: true);

        // PQRPVI puede venir como CSV o colección. Intentamos dividir en lista segura.
        var pviList = SplitCsvToList(listarQRAplicadosDto.PointOfSaleId);
        if (pviList.Count > 0)
            builder.WhereIn("PQRPVI", pviList);

        var cashierList = SplitCsvToList(listarQRAplicadosDto.CashierID);
        if (cashierList.Count > 0)
            builder.WhereIn("PQRCAI", cashierList);

        if (!string.IsNullOrWhiteSpace(listarQRAplicadosDto.Reference))
            builder.WhereLike("PQRREG", $"%{listarQRAplicadosDto.Reference}%", lower: true);

        if (!string.IsNullOrWhiteSpace(listarQRAplicadosDto.Type))
            builder.WhereEq("PQRQTI", listarQRAplicadosDto.Type);

        // Construir y ejecutar (SQL + parámetros)
        var result = builder.Build();
        using var command = _connection.GetDbCommand(result, _httpContextAccessor.HttpContext!);
        using var reader  = await command.ExecuteReaderAsync();

        // Leer a memoria (misma forma que ya usabas)
        var filas = new List<Dictionary<string, object>>();
        while (await reader.ReadAsync())
        {
            var fila = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
                fila[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            filas.Add(fila);
        }

        // ---- Filtrado por fecha aplicada (en memoria, igual que antes) ----
        static DateTime? BuildFechaAplicada(IDictionary<string, object> r)
        {
            int dia  = int.TryParse(r.GetValueOrDefault("PQRADI")?.ToString(), out var d) ? d : 0;
            int mes  = int.TryParse(r.GetValueOrDefault("PQRAME")?.ToString(), out var m) ? m : 0;
            int anio = int.TryParse(r.GetValueOrDefault("PQRAAN")?.ToString(), out var a) ? a : 0;
            int hRaw = int.TryParse(r.GetValueOrDefault("PQRAHO")?.ToString(), out var h) ? h : 0;
            if (dia == 0 || mes == 0 || anio == 0) return null;

            int hh = hRaw / 10000;
            int mm = (hRaw / 100) % 100;
            int ss = hRaw % 100;

            try { return new DateTime(anio, mes, dia, hh, mm, ss); }
            catch { return null; }
        }

        var resultadosFiltrados = filas
            .Select(r => new { Row = r, Fecha = BuildFechaAplicada(r) })
            .Where(x => x.Fecha.HasValue && x.Fecha.Value >= fechaInicial && x.Fecha.Value <= fechaFinal)
            .ToList();

        // ---- Ordenamiento por fecha aplicada (Asc/Desc) ----
        listarQRAplicadosDto.Sort ??= "Asc";
        resultadosFiltrados = listarQRAplicadosDto.Sort.Equals("Desc", StringComparison.OrdinalIgnoreCase)
            ? resultadosFiltrados.OrderByDescending(x => x.Fecha).ToList()
            : resultadosFiltrados.OrderBy(x => x.Fecha).ToList();

        // Total monto
        decimal totalMonto = resultadosFiltrados.Sum(x =>
        {
            var raw = x.Row.GetValueOrDefault("PQRMTO")?.ToString();
            return decimal.TryParse(raw, out var val) ? val : 0m;
        });

        // ---- Paginación (igual lógica que la tuya) ----
        int totalRegistros = resultadosFiltrados.Count;
        int pageSize = string.IsNullOrWhiteSpace(listarQRAplicadosDto.Size) || listarQRAplicadosDto.Size == "0"
            ? 1 : int.Parse(listarQRAplicadosDto.Size);
        int totalPaginas = (int)Math.Ceiling((double)totalRegistros / pageSize);
        int currentPage = string.IsNullOrWhiteSpace(listarQRAplicadosDto.Page) ? 0 : int.Parse(listarQRAplicadosDto.Page);

        if (pageSize >= totalRegistros) { currentPage = 0; totalPaginas = 1; }
        else if (currentPage >= totalPaginas) { currentPage = Math.Max(0, totalPaginas - 1); }

        int inicio = currentPage * pageSize;
        var pagina = resultadosFiltrados.Skip(inicio).Take(pageSize).ToList();

        // Helper para fecha de creación
        static string BuildFechaCreacionStr(IDictionary<string, object> r)
        {
            int dc = int.TryParse(r.GetValueOrDefault("PQRCDI")?.ToString(), out var v) ? v : 0;
            int mc = int.TryParse(r.GetValueOrDefault("PQRCME")?.ToString(), out v) ? v : 0;
            int ac = int.TryParse(r.GetValueOrDefault("PQRCAN")?.ToString(), out v) ? v : 0;
            int hc = int.TryParse(r.GetValueOrDefault("PQRCHO")?.ToString(), out v) ? v : 0;

            int hh = hc / 10000, mm = (hc / 100) % 100, ss = hc % 100;
            return $"{dc:D2}/{mc:D2}/{ac} {hh:D2}:{mm:D2}:{ss:D2}";
        }

        // Mapear a DTO de respuesta
        var content = pagina.Select(x =>
        {
            var r = x.Row;

            var fa = x.Fecha!.Value;
            string cuenta = r.GetValueOrDefault("PQRCTA")?.ToString() ?? string.Empty;
            string cuentaMask = cuenta.Length > 6 ? new string('*', 6) + cuenta[6..] : new string('*', cuenta.Length);

            return new RespuestaListarQRAplicadosDto.Content
            {
                CreationDate = BuildFechaCreacionStr(r),
                QrId = r.GetValueOrDefault("PQRQID")?.ToString() ?? string.Empty,
                Type = r.GetValueOrDefault("PQRQTI")?.ToString() ?? string.Empty,
                Cif = r.GetValueOrDefault("PQRCIF")?.ToString() ?? string.Empty,
                CifName = r.GetValueOrDefault("CLINOM")?.ToString() ?? string.Empty,
                CreationUser = r.GetValueOrDefault("PQRUMA")?.ToString() ?? string.Empty,
                PointOfSaleID = Convert.ToDecimal(r.GetValueOrDefault("PQRPVI") ?? 0),
                PointOfSaleDes = r.GetValueOrDefault("PQRPVD")?.ToString() ?? string.Empty,
                CashierId = Convert.ToDecimal(r.GetValueOrDefault("PQRCAI") ?? 0),
                CashierDes = r.GetValueOrDefault("PQRCAD")?.ToString() ?? string.Empty,
                Account = cuentaMask,
                Reference = r.GetValueOrDefault("PQRREG")?.ToString() ?? string.Empty,
                AppliedReference = r.GetValueOrDefault("PQRARE")?.ToString() ?? string.Empty,
                BankName = r.GetValueOrDefault("DESENT")?.ToString() ?? string.Empty,
                OriginatingAccountName = r.GetValueOrDefault("PQRNCO")?.ToString() ?? string.Empty,
                Currency = Convert.ToDecimal(r.GetValueOrDefault("PQRMON") ?? 0),
                Amount = Convert.ToDecimal(r.GetValueOrDefault("PQRMTO") ?? 0),
                AppliedDate = $"{fa:dd/MM/yyyy HH:mm:ss}",
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

    // --- Helpers locales ---
    static List<object> SplitCsvToList(string? csvOrSingle)
    {
        var list = new List<object>();
        if (string.IsNullOrWhiteSpace(csvOrSingle))
            return list;

        // Si viene "1,2,3" o "A,B,C"
        var parts = csvOrSingle.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var p in parts)
        {
            if (decimal.TryParse(p, out var n)) list.Add(n);
            else list.Add(p);
        }
        return list;
    }
}
