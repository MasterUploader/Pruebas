using Connections.Abstractions;
using Microsoft.AspNetCore.Http;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Models.Dtos.AutenticacionDtos;
using QueryBuilder.Builders;
using QueryBuilder.Enums;
using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Data.Common;
using System.Globalization;

namespace MS_BAN_38_UTH_RECAUDACION_PAGOS.Utils
{
    /// <summary>
    /// Clase utilitaria para manipular tokens de Ginih sobre AS/400 (DB2 for i),
    /// utilizando RestUtilities.QueryBuilder para construir y ejecutar SQL de forma segura.
    /// </summary>
    public class Token
    {
        private readonly IDatabaseConnection _connection;
        private readonly IHttpContextAccessor _contextAccessor;

        // Tabla/Library destino (AS/400)
        private readonly string _tableName = "TOKENUTH";
        private readonly string _library = "BCAH96DTA";

        // Campos que llegan del DTO (con valores por defecto para evitar nulls en INSERT/UPDATE)
        private readonly string _status = string.Empty;
        private readonly string _message = string.Empty;
        private readonly string _rToken = string.Empty;
        private readonly string _createdAt = string.Empty;
        private readonly string _timeStamp = string.Empty;
        private readonly string _value = string.Empty;
        private readonly string _name = string.Empty;

        // Campos calculados/recuperados
        private string _vence = string.Empty;
        private string _creado = string.Empty;
        private string _token = string.Empty;

        /// <summary>
        /// Constructor principal: inyecta DTO base, conexión y contexto.
        /// </summary>
        /// <param name="tokenStructure">Respuesta de login con datos de token.</param>
        /// <param name="connection">Conexión a base de datos.</param>
        /// <param name="contextAccessor">Accessor de HttpContext (para logging/decorators).</param>
        public Token(PostLoginResponseDto tokenStructure, IDatabaseConnection connection, IHttpContextAccessor contextAccessor)
        {
            _status = tokenStructure?.Status ?? string.Empty;
            _message = tokenStructure?.Message ?? string.Empty;
            _rToken = tokenStructure?.Data?.RefreshToken ?? string.Empty;
            _createdAt = tokenStructure?.Data?.CreatedAt ?? string.Empty;
            _timeStamp = tokenStructure?.TimeStamp ?? string.Empty;
            _value = tokenStructure?.Code?.Value ?? string.Empty;
            _name = tokenStructure?.Code?.Name ?? string.Empty;

            _connection = connection;
            _contextAccessor = contextAccessor;
        }

        /// <summary>
        /// Constructor alterno: solo inyecta conexión y contexto (para lecturas).
        /// </summary>
        public Token(IDatabaseConnection connection, IHttpContextAccessor contextAccessor)
        {
            _connection = connection;
            _contextAccessor = contextAccessor;
        }

        /// <summary>
        /// Guarda/actualiza el token en la tabla AS/400.
        /// Si no existe la fila ID=1, la inserta; caso contrario, actualiza sus campos.
        /// </summary>
        /// <remarks>
        /// Se calcula <c>_vence</c> sumando 2 años a <c>_createdAt</c> (formato ISO-UTC "yyyy-MM-ddTHH:mm:ss.fffZ").
        /// </remarks>
        /// <returns><c>true</c> si la operación fue exitosa; en caso contrario <c>false</c>.</returns>
        public bool SavenTokenUTH()
        {
            try
            {
                // 1) Calcular fecha de vencimiento a partir de CreatedAt (+2 años)
                if (DateTime.TryParseExact(_createdAt, "yyyy-MM-ddTHH:mm:ss.fffZ",
                                            CultureInfo.InvariantCulture,
                                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                            out var createdAtDt))
                {
                    var venceDt = createdAtDt.AddYears(2);
                    _vence = venceDt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
                }
                else
                {
                    // Si viene en formato inesperado, persiste lo recibido para no romper flujo
                    _vence = _createdAt;
                }

                _connection.Open();
                if (!_connection.IsConnected) return false;

                // 2) Verificar si existe fila ID = 1
                var existsQuery = new SelectQueryBuilder(_tableName, _library)
                    .Select("1")
                    .WhereRaw("ID = 1")
                    .Limit(1)
                    .Build();

                using (var cmdExists = _connection.GetDbCommand(_contextAccessor.HttpContext!))
                {
                    cmdExists.CommandText = existsQuery.Sql;
                    cmdExists.CommandType = System.Data.CommandType.Text;

                    using var rdr = cmdExists.ExecuteReader();
                    var exists = rdr.HasRows;

                    // 3) Insertar o Actualizar según exista
                    if (!exists)
                    {
                        // INSERT parametrizado (DB2 i → placeholders ? + Parameters)
                        var insert = new InsertQueryBuilder(_tableName, _library, SqlDialect.Db2i)
                            .IntoColumns("ID", "STATUS", "MESSAGE", "RTOKEN", "CREATEDAT", "TIMESTAMP", "VALUE", "NAME", "VENCE")
                            .Row(
                                1,
                                _status ?? string.Empty,
                                _message ?? string.Empty,
                                _rToken ?? string.Empty,
                                _createdAt ?? string.Empty,
                                _timeStamp ?? string.Empty,
                                _value ?? string.Empty,
                                _name ?? string.Empty,
                                _vence ?? string.Empty
                            )
                            .Build();

                        using var cmdIns = _connection.GetDbCommand(insert, _contextAccessor.HttpContext!);
                        var aff = cmdIns.ExecuteNonQuery();
                        return aff > 0;
                    }
                    else
                    {
                        // UPDATE parametrizado
                        var update = new UpdateQueryBuilder(_tableName, _library, SqlDialect.Db2i)
                            .Set("STATUS", _status ?? string.Empty)
                            .Set("MESSAGE", _message ?? string.Empty)
                            .Set("RTOKEN", _rToken ?? string.Empty)
                            .Set("CREATEDAT", _createdAt ?? string.Empty)
                            .Set("TIMESTAMP", _timeStamp ?? string.Empty)
                            .Set("VALUE", _value ?? string.Empty)
                            .Set("NAME", _name ?? string.Empty)
                            .Set("VENCE", _vence ?? string.Empty)
                            .WhereRaw("ID = 1")
                            .Build();

                        using var cmdUpd = _connection.GetDbCommand(update, _contextAccessor.HttpContext!);
                        var aff = cmdUpd.ExecuteNonQuery();
                        return aff > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO: integrar con tu servicio de logging estructurado
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                _connection.Close();
            }
        }

        /// <summary>
        /// Obtiene el token registrado (fila ID=1) y valida vigencia según la diferencia entre VENCE y CREATEDAT.
        /// </summary>
        /// <param name="rToken">Token de salida cuando la condición de vigencia se cumple.</param>
        /// <returns><c>true</c> si hay token válido; en caso contrario <c>false</c>.</returns>
        public bool GetToken(out string rToken)
        {
            rToken = string.Empty;

            try
            {
                _connection.Open();
                if (!_connection.IsConnected) return false;

                // SELECT VENCE, CREATEDAT, RTOKEN FROM BCAH96DTA.TOKENUTH WHERE ID = 1 FETCH FIRST 1 ROW ONLY
                var select = new SelectQueryBuilder(_tableName, _library)
                    .Select("VENCE", "CREATEDAT", "RTOKEN")
                    .WhereRaw("ID = 1")
                    .Limit(1)
                    .Build();

                using var cmd = _connection.GetDbCommand(_contextAccessor.HttpContext!);
                cmd.CommandText = select.Sql;
                cmd.CommandType = System.Data.CommandType.Text;

                using DbDataReader reader = cmd.ExecuteReader();
                if (!reader.HasRows) return false;

                while (reader.Read())
                {
                    _vence = reader.IsDBNull(reader.GetOrdinal("VENCE")) ? string.Empty : reader.GetString(reader.GetOrdinal("VENCE"));
                    _creado = reader.IsDBNull(reader.GetOrdinal("CREATEDAT")) ? string.Empty : reader.GetString(reader.GetOrdinal("CREATEDAT"));
                    _token = reader.IsDBNull(reader.GetOrdinal("RTOKEN")) ? string.Empty : reader.GetString(reader.GetOrdinal("RTOKEN"));
                }

                // Parseo de fechas con el mismo formato ISO-UTC usado en SavenTokenUTH()
                if (DateTime.TryParseExact(_vence, "yyyy-MM-ddTHH:mm:ss.fffZ",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out var venceDt)
                    &&
                    DateTime.TryParseExact(_creado, "yyyy-MM-ddTHH:mm:ss.fffZ",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out var creadoDt))
                {
                    var diferencia = venceDt - creadoDt;

                    // Mantengo tu regla original (Days > 0). Si quieres validar contra DateTime.UtcNow:
                    // if (DateTime.UtcNow <= venceDt) ...
                    if (diferencia.Days > 0 && !string.IsNullOrWhiteSpace(_token))
                    {
                        rToken = _token;
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                // TODO: integrar con tu servicio de logging estructurado
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                _connection.Close();
            }
        }
    }
}
