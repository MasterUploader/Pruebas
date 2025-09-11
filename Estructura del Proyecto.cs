// ================================================================
// IRegistraImpresionService.cs
// ================================================================
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.RegistraImpresion;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Services.RegistraImpresion
{
    /// <summary>
    /// Servicio para registrar eventos de impresión de tarjetas.
    /// </summary>
    public interface IRegistraImpresionService
    {
        /// <summary>
        /// Registra la impresión en las tablas correspondientes.
        /// </summary>
        /// <param name="postRegistraImpresionDto">Datos necesarios para el registro.</param>
        /// <returns><see langword="true"/> si el registro fue exitoso; en caso contrario <see langword="false"/>.</returns>
        Task<bool> RegistraImpresion(PostRegistraImpresionDto postRegistraImpresionDto);
    }
}



// ================================================================
// RegistraImpresionService.cs
// ================================================================
using Connections.Abstractions;
using Microsoft.AspNetCore.Http;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.RegistraImpresion;
using MS_BAN_43_Embosado_Tarjetas_Debito.Repository.IRepository.RegistraImpresion;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Services.RegistraImpresion
{
    /// <summary>
    /// Implementación del servicio de registro de impresión.
    /// </summary>
    /// <param name="_connection">Conexión a base de datos (AS/400).</param>
    /// <param name="_contextAccessor">Accessor del contexto HTTP (opcional para logging/trazabilidad).</param>
    public class RegistraImpresionService(IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor)
        : IRegistraImpresionService
    {
        /// <inheritdoc />
        public async Task<bool> RegistraImpresion(PostRegistraImpresionDto postRegistraImpresionDto)
        {
            var repo = new RegistraImpresionRepository(_connection, _contextAccessor);
            // No hay I/O previo aquí; mantenemos la firma async por consistencia.
            await Task.Yield();

            repo.GuardaImpresionUNI5400(postRegistraImpresionDto, out bool exito);
            return exito;
        }
    }
}




// ================================================================
// RegistraImpresionRepository.cs
// ================================================================
using Connections.Abstractions;
using Microsoft.AspNetCore.Http;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.RegistraImpresion;
using QueryBuilder.Builders;
using QueryBuilder.Enums;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Repository.IRepository.RegistraImpresion
{
    /// <summary>
    /// Repositorio para persistir el registro de impresión en AS/400.
    /// </summary>
    /// <param name="_connection">Conexión a base de datos.</param>
    /// <param name="_contextAccessor">Accessor del contexto HTTP (opcional para logging/trazabilidad).</param>
    public class RegistraImpresionRepository(IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor)
    {
        /// <summary>
        /// Actualiza <c>S38FILEBA.UNI5400</c> con la fecha, hora y usuario que realizó la impresión.
        /// Además, si el <paramref name="postRegistraImpresionDto"/> trae el nombre para la tarjeta,
        /// actualiza <c>S38FILEBA.UNI00MTA.MTNET</c>.
        /// </summary>
        /// <param name="postRegistraImpresionDto">DTO con los datos de impresión.</param>
        /// <param name="exito">Indica si la operación fue exitosa.</param>
        public void GuardaImpresionUNI5400(PostRegistraImpresionDto postRegistraImpresionDto, out bool exito)
        {
            _connection.Open();

            var now = DateTime.Now;
            // Formatos que usa la tabla (numéricos YYYYMMDD y HHMMSS)
            int fechaImpresion = now.Year * 10000 + now.Month * 100 + now.Day;
            int horaImpresion  = now.Hour * 10000 + now.Minute * 100 + now.Second;

            string numeroTarjeta   = postRegistraImpresionDto.NumeroTarjeta?.Trim() ?? string.Empty;
            string usuarioImpresor = postRegistraImpresionDto.UsuarioICBS?.Trim() ?? string.Empty;

            // ========================= UPDATE S38FILEBA.UNI5400 =========================
            // UPDATE S38FILEBA.UNI5400
            //    SET ST_FECHA_IMPRESION = ?,
            //        ST_HORA_IMPRESION  = ?,
            //        ST_USUARIO_IMPRESION = ?
            //  WHERE ST_CODIGO_TARJETA = ?
            var updUni5400 = new UpdateQueryBuilder("UNI5400", "S38FILEBA", SqlDialect.Db2i)
                .Set("ST_FECHA_IMPRESION",  fechaImpresion)
                .Set("ST_HORA_IMPRESION",   horaImpresion)
                .Set("ST_USUARIO_IMPRESION",usuarioImpresor)
                .WhereEq("ST_CODIGO_TARJETA", numeroTarjeta)
                .Build();

            using (var cmd = _connection.GetDbCommand(updUni5400, _contextAccessor.HttpContext))
            {
                var changed = cmd.ExecuteNonQuery();
                exito = changed > 0;
            }

            // Si actualizó UNI5400, actualizar también el nombre impreso en UNI00MTA (si viene).
            if (exito && !string.IsNullOrWhiteSpace(postRegistraImpresionDto.NombreEnTarjeta))
            {
                GuardaNombreUNI00MTA(numeroTarjeta, postRegistraImpresionDto.NombreEnTarjeta!);
            }
        }

        /// <summary>
        /// Actualiza el nombre impreso en <c>S38FILEBA.UNI00MTA</c>.
        /// </summary>
        /// <param name="codigoTarjeta">Número/código de la tarjeta (MTCTJ).</param>
        /// <param name="nombreEnTarjeta">Nombre a imprimir (MTNET).</param>
        private void GuardaNombreUNI00MTA(string codigoTarjeta, string nombreEnTarjeta)
        {
            // ========================= UPDATE S38FILEBA.UNI00MTA =========================
            // UPDATE S38FILEBA.UNI00MTA
            //    SET MTNET = ?
            //  WHERE MTCTJ = ?
            var updUni00mta = new UpdateQueryBuilder("UNI00MTA", "S38FILEBA", SqlDialect.Db2i)
                .Set("MTNET", nombreEnTarjeta?.Trim() ?? string.Empty)
                .WhereEq("MTCTJ", codigoTarjeta?.Trim() ?? string.Empty)
                .Build();

            using var cmd = _connection.GetDbCommand(updUni00mta, _contextAccessor.HttpContext);
            cmd.ExecuteNonQuery();
        }
    }
}



