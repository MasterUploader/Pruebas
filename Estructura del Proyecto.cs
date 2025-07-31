La consulta debe ser algo así:

var builder = new SelectQueryBuilder("PQR02QRG", "IS4TECHDTA")
.Select("*")
.Join("PQR01CLI", "IS4TECHDTA", "PQR02QRG.PQRCIF = PQR01CLI.CLINRO")
.WhereRaw("PQRSTA = 'APL'");

Pero tengo este error:
Severity	Code	Description	Project	File	Line	Suppression State
Error (active)	CS1501	No overload for method 'Join' takes 3 arguments	API_PARA_PRUEBAS	C:\Git\Librerias Davivienda\API_PARA_PRUEBAS\API_PARA_PRUEBAS\API_PARA_PRUEBAS\ServiceReference\SQL\SqlService.cs	145	

    Asi esta SQL SelectQueryBuilder:

using API_PARA_PRUEBAS.Models.Dtos.AccionesQr.ConsultasQR;
using API_PARA_PRUEBAS.Models.Dtos.ComerciosDto;
using API_PARA_PRUEBAS.ServiceReference.IServiceReference;
using Connections.Interfaces;
using QueryBuilder.Builders;
using QueryBuilder.Enums;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Globalization;

namespace API_PARA_PRUEBAS.ServiceReference.SQL;

public class SqlService(IHttpClientFactory _httpClientFactory, IDatabaseConnection _connection, IHttpContextAccessor _httpContextAccessor) : ISqlService
{
    public async Task<RespuestaListarComercioDto> ListarComercioAsync(ListarComerciosDto listarComercio)
    {
        try
        {
            _connection.Open();

            var pageSize = string.IsNullOrWhiteSpace(listarComercio.Size) || listarComercio.Size == "0" ? 1 : int.Parse(listarComercio.Size);
            var currentPage = string.IsNullOrWhiteSpace(listarComercio.Page) ? 0 : int.Parse(listarComercio.Page);
            var skip = currentPage * pageSize;

            string table = "PQR01L01";
            string library = "IS4TECHDTA";

            // COUNT
            var countQuery = QueryBuilder.Core.QueryBuilder
                .From(table, library)
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
                             .OrderBy(("CLINRO", SortDirection.Desc))
                             .Offset(skip)
                             .FetchNext(pageSize);
                }
                else
                {
                    countQuery.WhereRaw($"LOWER(CLINOM) LIKE LOWER('%{listarComercio.Search}%')");
                    dataQuery.WhereRaw($"LOWER(CLINOM) LIKE LOWER('%{listarComercio.Search}%')")
                             .OrderBy(("CLINOM", SortDirection.Desc))
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
            using var countCommand = _connection.GetDbCommand(_httpContextAccessor.HttpContext!);
            countCommand.CommandText = countResult.Sql;
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
            using var dataCommand = _connection.GetDbCommand(_httpContextAccessor.HttpContext!);
            dataCommand.CommandText = dataResult.Sql;
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

    public async Task<RespuestaConsultaResumenSemanalQRAplicadosDto> ConsultaResumenSemanalQRAplicadosAsync(ConsultarResumenSemanalAplicadosQRDto dto)
    {
        try
        {
            _connection.Open();

            DateTime fechaInicial = DateTime.ParseExact(dto.DateInit, "yyyy-MM-dd HH:mm:ss", null);
            DateTime fechaFinal = DateTime.ParseExact(dto.DateEnd, "yyyy-MM-dd HH:mm:ss", null);

            var builder = new SelectQueryBuilder("PQR02QRG", "IS4TECHDTA")
            .Select("*")
            .Join("PQR01CLI", "IS4TECHDTA", "PQR02QRG.PQRCIF = PQR01CLI.CLINRO")
            .WhereRaw("PQRSTA = 'APL'");


            if (!string.IsNullOrEmpty(dto.Cif))
                builder.WhereRaw($"LOWER(PQRCIF) LIKE LOWER('%{dto.Cif}%')");

            if (dto.PointOfSaleId?.Any() == true)
                builder.WhereRaw($"PQRPVI IN ({string.Join(",", dto.PointOfSaleId)})");

            var result = builder.Build();

            using var command = _connection.GetDbCommand(_httpContextAccessor?.HttpContext);
            command.CommandText = result.Sql;
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
}
