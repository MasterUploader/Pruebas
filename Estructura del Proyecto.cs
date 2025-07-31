Ahora convierte este método y entregalo completo:

public async Task<RespuestaListarQRAplicadosDto> ListarQRAplicadosAsync(ListarQRAplicadosDto listarQRAplicadosDto)
{
    try
    {
        int? codigoRespuesta = 0;
        string? descripcionRespuesta = "";
        DateTime fechaInicial = DateTime.ParseExact(listarQRAplicadosDto.DateInit, "yyyy-MM-dd HH:mm:ss", null);
        DateTime fechaFinal = DateTime.ParseExact(listarQRAplicadosDto.DateEnd, "yyyy-MM-dd HH:mm:ss", null);
        var respuestDto = new RespuestaListarQRAplicadosDto();

        connection.Open();

        string dataQry = "SELECT * FROM IS4TECHDTA.PQR02QRG INNER JOIN IS4TECHDTA.PQR01CLI ON IS4TECHDTA.PQR02QRG.PQRCIF = IS4TECHDTA.PQR01CLI.CLINRO INNER JOIN ACH.ENTIDAD ON IS4TECHDTA.PQR02QRG.PQRABA = ACH.ENTIDAD.CODENT WHERE 1=1";

        List<OleDbParameter> parameters = new List<OleDbParameter>();

        if (!string.IsNullOrEmpty(listarQRAplicadosDto.Cif))
        {
            dataQry += " AND LOWER (PQRCIF) LIKE ?";
            parameters.Add(new OleDbParameter("PQRCIF", listarQRAplicadosDto.Cif.ToString()));
        }
        if (!string.IsNullOrEmpty(listarQRAplicadosDto.PointOfSaleId))
        {
            dataQry += " AND PQRPVI IN (" + string.Join(",", listarQRAplicadosDto.PointOfSaleId) + ")";

        }
        if (!string.IsNullOrEmpty(listarQRAplicadosDto.CashierID))
        {
            dataQry += " AND PQRCAI IN (" + string.Join(",", listarQRAplicadosDto.CashierID) + ")";

        }
        if (!string.IsNullOrEmpty(listarQRAplicadosDto.Reference))
        {
            dataQry += " AND (LOWER(PQRREG) LIKE LOWER(?))";
            parameters.Add(new OleDbParameter("PQRREG", '%' + listarQRAplicadosDto.Reference.ToString() + '%'));
        }
        if (!string.IsNullOrEmpty(listarQRAplicadosDto.Type))
        {
            dataQry += " AND PQRQTI = ?";
            parameters.Add(new OleDbParameter("PQRQTI", "="));
        }

        dataQry += " AND PQRSTA = ?";
        parameters.Add(new OleDbParameter("PQRSTA", "APL"));


        var resultados = new List<Dictionary<string, object>>();
        using (OleDbCommand command = new OleDbCommand(dataQry, connection.Connect.OleDbConnection))
        {
            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }
            using (OleDbDataReader reader = command.ExecuteReader())
            {
                while (await reader.ReadAsync())
                {
                    var fila = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        fila[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    resultados.Add(fila);
                }
            }
        }

        var resultadosFiltrados = resultados

        .Where(r =>
        {
            //Aplicados
            int dia = int.TryParse(r.GetValueOrDefault("PQRADI")?.ToString(), out var tempDia) ? tempDia : 0;
            int mes = int.TryParse(r.GetValueOrDefault("PQRAME")?.ToString(), out var tempMes) ? tempMes : 0;
            int año = int.TryParse(r.GetValueOrDefault("PQRAAN")?.ToString(), out var tempAño) ? tempAño : 0;
            int horacompleta = int.TryParse(r.GetValueOrDefault("PQRAHO")?.ToString(), out var tempHora) ? tempHora : 0;

            int hora = horacompleta / 10000;
            int minuto = (horacompleta / 100) % 100;
            int segundo = horacompleta % 100;

            var fecha = new DateTime(año, mes, dia, hora, minuto, segundo);
            return fecha >= fechaInicial && fecha <= fechaFinal;


        });

        if (listarQRAplicadosDto.Sort == "")
        {
            listarQRAplicadosDto.Sort = "Asc";
        }

        if (listarQRAplicadosDto.Sort == "Desc")
        {
            resultadosFiltrados = resultadosFiltrados.OrderByDescending(r =>
            {
                int dia = int.TryParse(r.GetValueOrDefault("PQRADI")?.ToString(), out var tempDia) ? tempDia : 0;
                int mes = int.TryParse(r.GetValueOrDefault("PQRAME")?.ToString(), out var tempMes) ? tempMes : 0;
                int año = int.TryParse(r.GetValueOrDefault("PQRAAN")?.ToString(), out var tempAño) ? tempAño : 0;
                int horacompleta = int.TryParse(r.GetValueOrDefault("PQRAHO")?.ToString(), out var tempHora) ? tempHora : 0;

                int hora = horacompleta / 10000;
                int minuto = (horacompleta / 100) % 100;
                int segundo = horacompleta % 100;

                return new DateTime(año, mes, dia, hora, minuto, segundo);
            }).ToList();
        }
        else if (listarQRAplicadosDto.Sort == "Asc")
        {
            resultadosFiltrados = resultadosFiltrados.OrderBy(r =>
            {
                int dia = int.TryParse(r.GetValueOrDefault("PQRADI")?.ToString(), out var tempDia) ? tempDia : 0;
                int mes = int.TryParse(r.GetValueOrDefault("PQRAME")?.ToString(), out var tempMes) ? tempMes : 0;
                int año = int.TryParse(r.GetValueOrDefault("PQRAAN")?.ToString(), out var tempAño) ? tempAño : 0;
                int horacompleta = int.TryParse(r.GetValueOrDefault("PQRAHO")?.ToString(), out var tempHora) ? tempHora : 0;

                int hora = horacompleta / 10000;
                int minuto = (horacompleta / 100) % 100;
                int segundo = horacompleta % 100;

                return new DateTime(año, mes, dia, hora, minuto, segundo);
            }).ToList();
        }



        decimal TotalMonto = resultadosFiltrados.Sum(r =>
        {
            var valorMonto = r.GetValueOrDefault("PQRMTO")?.ToString();

            return decimal.TryParse(valorMonto, out var monto) ? monto : 0;
        });



        /*Paginado*/
        int totalRegistros = resultadosFiltrados.Count();
        if (listarQRAplicadosDto.Size == "")
        {
            listarQRAplicadosDto.Size = "1";
        }
        else if (listarQRAplicadosDto.Size == "0")
        {
            listarQRAplicadosDto.Size = "1";
        }
        if (listarQRAplicadosDto.Page == "")
        {
            listarQRAplicadosDto.Page = "0";
        }

        int totalPaginas = (int)Math.Ceiling(totalRegistros / Convert.ToDouble(listarQRAplicadosDto.Size));

        if (!resultadosFiltrados.Any())
        {

            respuestDto.ResponseCode = 1;
            respuestDto.ResponseDescription = "busqueda no obtuvo datos";


            return respuestDto;
        };

        if (Convert.ToInt32(listarQRAplicadosDto.Size) >= totalRegistros)
        {
            listarQRAplicadosDto.Page = "0";
            totalPaginas = 1;

        }
        int paginalActual = Convert.ToInt32(listarQRAplicadosDto.Page);

        if (paginalActual < 0)
        {
            paginalActual = 0;
        }
        else if (paginalActual >= totalPaginas)
        {
            paginalActual = totalPaginas - 1;
        }

        int inicio = paginalActual * Convert.ToInt32(listarQRAplicadosDto.Size);
        int fin = Math.Min(inicio + Convert.ToInt32(listarQRAplicadosDto.Size), totalRegistros);
        var paginaDeResultados = resultadosFiltrados.Skip(inicio).Take(Convert.ToInt32(listarQRAplicadosDto.Size)).ToList();
        listarQRAplicadosDto.Page = paginalActual.ToString();

        respuestDto.content = new RespuestaListarQRAplicadosDto.Content[paginaDeResultados.Count];



        for (int i = 0; i < paginaDeResultados.Count; i++)
        {
            var result = paginaDeResultados[i];
            int diacreacion = int.TryParse(result.GetValueOrDefault("PQRCDI")?.ToString(), out var tempDia) ? tempDia : 0;
            int mescreacion = int.TryParse(result.GetValueOrDefault("PQRCME")?.ToString(), out var tempMes) ? tempMes : 0;
            int añocreacion = int.TryParse(result.GetValueOrDefault("PQRCAN")?.ToString(), out var tempAño) ? tempAño : 0;
            int horacreacion = int.TryParse(result.GetValueOrDefault("PQRCHO")?.ToString(), out var tempHora) ? tempHora : 0;

            int hora = horacreacion / 10000;
            int minuto = (horacreacion / 100) % 100;
            int segundo = horacreacion % 100;


            int dia = int.TryParse(result.GetValueOrDefault("PQRADI")?.ToString(), out var tempDiaApl) ? tempDiaApl : 0;
            int mes = int.TryParse(result.GetValueOrDefault("PQRAME")?.ToString(), out var tempMesApl) ? tempMesApl : 0;
            int año = int.TryParse(result.GetValueOrDefault("PQRAAN")?.ToString(), out var tempAñoApl) ? tempAñoApl : 0;
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
