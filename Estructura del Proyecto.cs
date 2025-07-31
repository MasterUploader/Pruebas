Necesito este método convertido a usar RestUtilities.QueryBuilder

 public async Task<RespuestaConsultaResumenSemanalQRAplicadosDto> ConsultaResumenSemanalQRAplicadosAsync(ConsultarResumenSemanalAplicadosQRDto consultarResumenSemanalAplicadosQRDto)
 {
     try
     {
         int? codigoRespuesta = 0;
         string? descripcionRespuesta = "";
         DateTime fechaInicial = DateTime.ParseExact(consultarResumenSemanalAplicadosQRDto.DateInit, "yyyy-MM-dd HH:mm:ss", null);
         DateTime fechaFinal = DateTime.ParseExact(consultarResumenSemanalAplicadosQRDto.DateEnd, "yyyy-MM-dd HH:mm:ss", null);
         var respuestDto = new RespuestaConsultaResumenSemanalQRAplicadosDto();
         connection.Open();

         string dataQry = "SELECT * FROM IS4TECHDTA.PQR02QRG INNER JOIN IS4TECHDTA.PQR01CLI ON IS4TECHDTA.PQR02QRG.PQRCIF = IS4TECHDTA.PQR01CLI.CLINRO WHERE 1=1";


         List<OleDbParameter> parameters = new List<OleDbParameter>();

         if (!string.IsNullOrEmpty(consultarResumenSemanalAplicadosQRDto.Cif))
         {
             dataQry += " AND LOWER(PQRCIF) LIKE ?";
             parameters.Add(new OleDbParameter("PQRCIF", consultarResumenSemanalAplicadosQRDto.Cif.ToString()));
         }
         if (!string.IsNullOrEmpty(consultarResumenSemanalAplicadosQRDto.PointOfSaleId))
         {
             dataQry += " AND PQRPVI IN (" + string.Join(",", consultarResumenSemanalAplicadosQRDto.PointOfSaleId) + ")";

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



         }).ToList();

         if (!resultadosFiltrados.Any())
         {

             respuestDto.ResponseCode = 1;
             respuestDto.ResponseDescription = "busqueda no obtuvo datos";
             respuestDto.Contents = new RespuestaConsultaResumenSemanalQRAplicadosDto.Content[resultadosFiltrados.Count];


             return respuestDto;
         };





         var resultAgrupados = new Dictionary<string, dynamic>();



         foreach (var result in resultadosFiltrados)
         {

             var cif = result.GetValueOrDefault("PQRCIF")?.ToString() ?? string.Empty;
             var pointOfsaleId = result.GetValueOrDefault("PQRPVI")?.ToString() ?? string.Empty;
             decimal amount = decimal.Parse(result.GetValueOrDefault("PQRMTO")?.ToString() ?? string.Empty);

             int diaApplied = int.TryParse(result.GetValueOrDefault("PQRADI")?.ToString(), out var tempDia) ? tempDia : 0;
             int mesApplied = int.TryParse(result.GetValueOrDefault("PQRAME")?.ToString(), out var tempMes) ? tempMes : 0;
             int añoApplied = int.TryParse(result.GetValueOrDefault("PQRAAN")?.ToString(), out var tempAño) ? tempAño : 0;

             var fecha = new DateTime(añoApplied, mesApplied, diaApplied);
             //Calculo de numero de semana
             CultureInfo cultura = CultureInfo.CurrentCulture;
             Calendar calendar = cultura.Calendar;
             CalendarWeekRule regla = cultura.DateTimeFormat.CalendarWeekRule;
             DayOfWeek primerDiaSemana = cultura.DateTimeFormat.FirstDayOfWeek;

             int numeroSemana = calendar.GetWeekOfYear(fecha, regla, primerDiaSemana);

             string clave = string.Concat(cif, pointOfsaleId, numeroSemana);

             if (resultAgrupados.ContainsKey(clave))
             {
                 resultAgrupados[clave] = new
                 {
                     CIF = resultAgrupados[clave].CIF,
                     CIFName = resultAgrupados[clave].CIFName,
                     WEEK = resultAgrupados[clave].WEEK,
                     POintOfSaleId = resultAgrupados[clave].POintOfSaleId,
                     POintOfSaleDes = resultAgrupados[clave].POintOfSaleDes,
                     AMount = resultAgrupados[clave].AMount + amount,
                     QUantity = resultAgrupados[clave].QUantity + 1,
                     STatus = resultAgrupados[clave].STatus
                 };

             }
             else
             {



                 resultAgrupados[clave] = new
                 {
                     CIF = cif,
                     CIFName = result.GetValueOrDefault("CLINOM")?.ToString() ?? string.Empty,
                     WEEK = $"{numeroSemana}",
                     POintOfSaleId = pointOfsaleId,
                     POintOfSaleDes = result.GetValueOrDefault("PQRPVD")?.ToString() ?? string.Empty,
                     AMount = amount,
                     QUantity = 1,
                     STatus = "APL",

                 };
             }
         }

         var listaCont = new List<RespuestaConsultaResumenSemanalQRAplicadosDto.Content>();
         foreach (var entrada in resultAgrupados.Values)
         {
             var contenidoItems = new RespuestaConsultaResumenSemanalQRAplicadosDto.Content
             {
                 Cif = entrada.CIF,
                 CifName = entrada.CIFName,
                 Week = entrada.WEEK,
                 PointOfSaleId = entrada.POintOfSaleId,
                 PointOfSaleDes = entrada.POintOfSaleDes,
                 Amount = entrada.AMount,
                 Quantity = entrada.QUantity,
                 Status = entrada.STatus,

             };

             listaCont.Add(contenidoItems);
         }


         decimal totalMonto = listaCont.Sum(item => item.Amount);
         respuestDto.ResponseCode = 0;
         respuestDto.TotalAmount = totalMonto.ToString();
         respuestDto.ResponseDescription = "Consulta exitosa";
         respuestDto.Contents = listaCont.ToArray();


         return respuestDto;


     }
     catch (Exception ex)
     {
         return new RespuestaConsultaResumenSemanalQRAplicadosDto
         {
             ResponseCode = 1,
             ResponseDescription = ex.Message,
             Contents = Array.Empty<RespuestaConsultaResumenSemanalQRAplicadosDto.Content>(),
         };

     }
 }
