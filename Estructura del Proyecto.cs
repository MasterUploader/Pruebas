using System.Threading.Tasks;

public interface IUsuarioService
{
    // ...lo que ya tenías

    /// <summary>
    /// Verifica si ya existe un usuario (insensible a mayúsculas/minúsculas).
    /// </summary>
    /// <param name="usuario">Nombre de usuario a verificar.</param>
    /// <returns>true si existe; false en caso contrario.</returns>
    Task<bool> ExisteUsuarioAsync(string usuario);

    /// <summary>
    /// Crea un usuario nuevo en USUADMIN.
    /// </summary>
    /// <param name="usuario">Objeto con Usuario, TipoUsu y Estado.</param>
    /// <param name="clavePlano">Clave en texto plano (se cifra en el servicio).</param>
    /// <returns>true si se insertó; false si no.</returns>
    Task<bool> CrearUsuarioAsync(UsuarioModel usuario, string clavePlano);
}


using System;
using System.Data;
using System.Threading.Tasks;
using RestUtilities.Connections;
using RestUtilities.QueryBuilder;

public class UsuarioService : IUsuarioService
{
    private readonly IConnections _connections;     // RestUtilities.Connections
    private readonly string _connName;              // nombre de conexión (appsettings)
    private readonly IPasswordCipher? _cipher;      // opcional: tu servicio de cifrado si lo tienes

    public UsuarioService(IConnections connections, IConfiguration cfg, IPasswordCipher? cipher = null)
    {
        _connections = connections;
        _cipher = cipher;
        _connName = cfg.GetConnectionString("AS400") ?? "AS400"; // ajusta el nombre
    }

    /// <summary>
    /// Cifra la clave con el mecanismo disponible.
    /// - Si se inyectó un servicio de cifrado (IPasswordCipher), lo usa.
    /// - Si no, aplica SHA-256 como fallback (para que compile). 
    ///   Reemplázalo si necesitas compatibilidad 100% con tu algoritmo anterior.
    /// </summary>
    private static string FallbackSha256(string plain)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(plain);
        var hash = sha.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    private string Encriptar(string plain) => _cipher != null ? _cipher.Encriptar(plain) : FallbackSha256(plain);

    /// <summary>
    /// Verifica si un usuario ya existe en BCAH96DTA.USUADMIN.
    /// Emplea WHERE RAW para hacer la comparación insensible a mayúsculas/minúsculas.
    /// </summary>
    public async Task<bool> ExisteUsuarioAsync(string usuario)
    {
        if (string.IsNullOrWhiteSpace(usuario)) return false;

        await using var cn = await _connections.OpenAsync(_connName);

        // SELECT COUNT(1) FROM BCAH96DTA.USUADMIN WHERE UPPER(USUARIO)=UPPER(@usuario)
        var qb = SqlBuilder
            .Select("COUNT(1)")
            .From("BCAH96DTA.USUADMIN")
            .WhereRaw("UPPER(USUARIO) = UPPER(@usuario)");

        var (sql, parameters) = qb.Build();

        await using var cmd = cn.CreateCommand();
        cmd.CommandText = sql;

        var pUsuario = cmd.CreateParameter();
        pUsuario.ParameterName = "@usuario";
        pUsuario.Value = usuario;
        cmd.Parameters.Add(pUsuario);

        var scalar = await cmd.ExecuteScalarAsync();
        var count = Convert.ToInt64(scalar ?? 0);
        return count > 0;
    }

    /// <summary>
    /// Inserta un registro en BCAH96DTA.USUADMIN validando duplicados.
    /// </summary>
    /// <param name="usuario">Modelo con Usuario, TipoUsu (1..3), Estado ('A'/'I').</param>
    /// <param name="clavePlano">Clave en texto plano (se cifra aquí).</param>
    public async Task<bool> CrearUsuarioAsync(UsuarioModel usuario, string clavePlano)
    {
        if (usuario == null) throw new ArgumentNullException(nameof(usuario));
        if (string.IsNullOrWhiteSpace(usuario.Usuario)) throw new ArgumentException("Usuario requerido.", nameof(usuario));
        if (usuario.TipoUsu < 1 || usuario.TipoUsu > 3) throw new ArgumentException("Tipo de usuario inválido.", nameof(usuario.TipoUsu));
        if (usuario.Estado is not ("A" or "I")) throw new ArgumentException("Estado inválido.", nameof(usuario.Estado));
        if (string.IsNullOrWhiteSpace(clavePlano)) throw new ArgumentException("Clave requerida.", nameof(clavePlano));

        // Validar duplicado primero
        if (await ExisteUsuarioAsync(usuario.Usuario))
            return false;

        var pass = Encriptar(clavePlano);

        await using var cn = await _connections.OpenAsync(_connName);
        await using var tx = await cn.BeginTransactionAsync();

        try
        {
            // INSERT INTO BCAH96DTA.USUADMIN (USUARIO, PASS, TIPUSU, ESTADO)
            // VALUES (@usuario, @pass, @tipo, @estado)
            var qb = SqlBuilder
                .InsertInto("BCAH96DTA.USUADMIN")
                .Columns("USUARIO", "PASS", "TIPUSU", "ESTADO")
                .Values("@usuario", "@pass", "@tipo", "@estado");

            var (sql, _) = qb.Build();

            await using var cmd = cn.CreateCommand();
            cmd.Transaction = (IDbTransaction)tx;
            cmd.CommandText = sql;

            var pUsuario = cmd.CreateParameter(); pUsuario.ParameterName = "@usuario"; pUsuario.Value = usuario.Usuario; cmd.Parameters.Add(pUsuario);
            var pPass    = cmd.CreateParameter(); pPass.ParameterName    = "@pass";    pPass.Value    = pass;            cmd.Parameters.Add(pPass);
            var pTipo    = cmd.CreateParameter(); pTipo.ParameterName    = "@tipo";    pTipo.Value    = usuario.TipoUsu;  cmd.Parameters.Add(pTipo);
            var pEstado  = cmd.CreateParameter(); pEstado.ParameterName  = "@estado";  pEstado.Value  = usuario.Estado;   cmd.Parameters.Add(pEstado);

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
