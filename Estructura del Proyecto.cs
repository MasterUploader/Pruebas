// Services/Usuarios/UsuarioService.cs (extracto)
using System;
using System.Data;
using System.Threading.Tasks;
using CAUAdministracion.Models;
using Microsoft.Extensions.Configuration;
using RestUtilities.Connections;
using RestUtilities.QueryBuilder;

namespace CAUAdministracion.Services.Usuarios
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IConnections _connections;
        private readonly IPasswordCipher _cipher;
        private readonly string _connName;

        public UsuarioService(IConnections connections, IPasswordCipher cipher, IConfiguration cfg)
        {
            _connections = connections;
            _cipher = cipher;
            _connName = cfg.GetConnectionString("AS400") ?? "AS400";
        }

        // ... ExisteUsuarioAsync (con WhereRaw) se queda igual ...

        /// <summary>
        /// Inserta un registro en BCAH96DTA.USUADMIN cifrando la clave con OperacionesVarias.
        /// </summary>
        public async Task<bool> CrearUsuarioAsync(UsuarioModel usuario, string clavePlano)
        {
            if (usuario == null) throw new ArgumentNullException(nameof(usuario));
            if (string.IsNullOrWhiteSpace(usuario.Usuario)) throw new ArgumentException("Usuario requerido.", nameof(usuario));
            if (usuario.TipoUsu is < 1 or > 3) throw new ArgumentException("Tipo de usuario inválido.", nameof(usuario));
            if (usuario.Estado is not ("A" or "I")) throw new ArgumentException("Estado inválido.", nameof(usuario));
            if (string.IsNullOrWhiteSpace(clavePlano)) throw new ArgumentException("Clave requerida.", nameof(clavePlano));

            // ====> Cifrado con tu clase OperacionesVarias (Legacy por defecto o AES si config lo indica)
            var claveCifrada = _cipher.Encriptar(clavePlano);

            await using var cn = await _connections.OpenAsync(_connName);
            await using var tx = await cn.BeginTransactionAsync();

            try
            {
                var qb = SqlBuilder
                    .InsertInto("BCAH96DTA.USUADMIN")
                    .Columns("USUARIO", "PASS", "TIPUSU", "ESTADO")
                    .Values("@usuario", "@pass", "@tipo", "@estado");

                var (sql, _) = qb.Build();

                await using var cmd = cn.CreateCommand();
                cmd.Transaction = (IDbTransaction)tx;
                cmd.CommandText = sql;

                var pUsuario = cmd.CreateParameter(); pUsuario.ParameterName = "@usuario"; pUsuario.Value = usuario.Usuario; cmd.Parameters.Add(pUsuario);
                var pPass    = cmd.CreateParameter(); pPass.ParameterName    = "@pass";    pPass.Value    = claveCifrada;   cmd.Parameters.Add(pPass);
                var pTipo    = cmd.CreateParameter(); pTipo.ParameterName    = "@tipo";    pTipo.Value    = usuario.TipoUsu; cmd.Parameters.Add(pTipo);
                var pEstado  = cmd.CreateParameter(); pEstado.ParameterName  = "@estado";  pEstado.Value  = usuario.Estado;  cmd.Parameters.Add(pEstado);

                var rows = await cmd.ExecuteNonQueryAsync();
                await tx.CommitAsync();
                return rows > 0;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
