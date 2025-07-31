public async Task<RespuestaConsultaResumenSemanalQRAplicadosDto> ConsultaResumenSemanalQRAplicadosAsync(ConsultarResumenSemanalAplicadosQRDto dto)
{
    try
    {
        connection.Open();

        DateTime fechaInicial = DateTime.ParseExact(dto.DateInit, "yyyy-MM-dd HH:mm:ss", null);
        DateTime fechaFinal = DateTime.ParseExact(dto.DateEnd, "yyyy-MM-dd HH:mm:ss", null);

        var builder = new SelectQueryBuilder("PQR02QRG", "IS4TECHDTA")
            .Select("*")
            .Join("PQR01CLI", "IS4TECHDTA", "PQR02QRG.PQRCIF = PQR01CLI.CLINRO")
            .Where("PQRSTA = 'APL'");

        if (!string.IsNullOrEmpty(dto.Cif))
            builder.Where($"LOWER(PQRCIF) LIKE LOWER('%{dto.Cif}%')");

        if (dto.PointOfSaleId?.Any() == true)
            builder.Where($"PQRPVI IN ({string.Join(",", dto.PointOfSaleId)})");

        var result = builder.Build();

        using var command = connection.GetDbCommand(result, _httpContextAccessor?.HttpContext);
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

        var resultadosFiltrados = resultados.Where(r =>
        {
            int dia = int.TryParse(r.GetValueOrDefault("PQRADI")?.ToString(), out var d) ? d : 0;
            int mes = int.TryParse(r.GetValueOrDefault("PQRAME")?.ToString(), out var m) ? m : 0;
            int año = int.TryParse(r.GetValueOrDefault("PQRAAN")?.ToString(), out var a) ? a : 0;
            int horaCompleta = int.TryParse(r.GetValueOrDefault("PQRAHO")?.ToString(), out var h) ? h : 0;

            int hora = horaCompleta / 10000;
            int minuto = (horaCompleta / 100) % 100;
            int segundo = horaCompleta % 100;

            try
            {
                var fecha = new DateTime(año, mes, dia, hora, minuto, segundo);
                return fecha >= fechaInicial && fecha <= fechaFinal;
            }
            catch
            {
                return false;
            }
        }).ToList();

        if (!resultadosFiltrados.Any())
        {
            return new RespuestaConsultaResumenSemanalQRAplicadosDto
            {
                ResponseCode = 1,
                ResponseDescription = "Búsqueda no obtuvo datos",
                Contents = []
            };
        }

        var cultura = CultureInfo.CurrentCulture;
        var calendar = cultura.Calendar;
        var regla = cultura.DateTimeFormat.CalendarWeekRule;
        var primerDiaSemana = cultura.DateTimeFormat.FirstDayOfWeek;

        var resultAgrupados = new Dictionary<string, dynamic>();

        foreach (var r in resultadosFiltrados)
        {
            string cif = r.GetValueOrDefault("PQRCIF")?.ToString() ?? "";
            string pointOfSaleId = r.GetValueOrDefault("PQRPVI")?.ToString() ?? "";
            decimal amount = decimal.TryParse(r.GetValueOrDefault("PQRMTO")?.ToString(), out var amt) ? amt : 0;

            int dia = int.TryParse(r.GetValueOrDefault("PQRADI")?.ToString(), out var d) ? d : 0;
            int mes = int.TryParse(r.GetValueOrDefault("PQRAME")?.ToString(), out var m) ? m : 0;
            int año = int.TryParse(r.GetValueOrDefault("PQRAAN")?.ToString(), out var a) ? a : 0;

            var fecha = new DateTime(año, mes, dia);
            int numeroSemana = calendar.GetWeekOfYear(fecha, regla, primerDiaSemana);

            string clave = $"{cif}{pointOfSaleId}{numeroSemana}";

            if (resultAgrupados.ContainsKey(clave))
            {
                resultAgrupados[clave] = new
                {
                    resultAgrupados[clave].CIF,
                    resultAgrupados[clave].CIFName,
                    resultAgrupados[clave].WEEK,
                    resultAgrupados[clave].PointOfSaleId,
                    resultAgrupados[clave].PointOfSaleDes,
                    AMount = resultAgrupados[clave].AMount + amount,
                    QUantity = resultAgrupados[clave].QUantity + 1,
                    resultAgrupados[clave].STatus
                };
            }
            else
            {
                resultAgrupados[clave] = new
                {
                    CIF = cif,
                    CIFName = r.GetValueOrDefault("CLINOM")?.ToString() ?? "",
                    WEEK = numeroSemana.ToString(),
                    PointOfSaleId = pointOfSaleId,
                    PointOfSaleDes = r.GetValueOrDefault("PQRPVD")?.ToString() ?? "",
                    AMount = amount,
                    QUantity = 1,
                    STatus = "APL"
                };
            }
        }

        var contenidos = resultAgrupados.Values.Select(x => new RespuestaConsultaResumenSemanalQRAplicadosDto.Content
        {
            Cif = x.CIF,
            CifName = x.CIFName,
            Week = x.WEEK,
            PointOfSaleId = x.PointOfSaleId,
            PointOfSaleDes = x.PointOfSaleDes,
            Amount = x.AMount,
            Quantity = x.QUantity,
            Status = x.STatus
        }).ToList();

        return new RespuestaConsultaResumenSemanalQRAplicadosDto
        {
            ResponseCode = 0,
            ResponseDescription = "Consulta exitosa",
            Contents = contenidos.ToArray(),
            TotalAmount = contenidos.Sum(x => x.Amount).ToString()
        };
    }
    catch (Exception ex)
    {
        return new RespuestaConsultaResumenSemanalQRAplicadosDto
        {
            ResponseCode = 1,
            ResponseDescription = ex.Message,
            Contents = []
        };
    }
}
