Ahora convierte este método para que utilice RestUtilities.QueryBuilder

using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.RegistraImpresion;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Services.RegistraImpresion;

/// <summary>
/// Interfaz IRegistraImpresionService
/// </summary>
public interface IRegistraImpresionService
{
    /// <summary>
    /// Método que registra la impresión.
    /// </summary>
    /// <param name="postRegistraImpresionDto"></param>
    /// <returns>Retorna si el registro fue exitoso o no.</returns>
    Task<bool> RegistraImpresion(PostRegistraImpresionDto postRegistraImpresionDto);
}


using Connections.Abstractions;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.RegistraImpresion;
using MS_BAN_43_Embosado_Tarjetas_Debito.Repository.IRepository.RegistraImpresion;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Services.RegistraImpresion;

/// <summary>
/// Clase de Servicio RegistraImpresionService.
/// </summary>
/// <param name="_connection">Instancia de IDatabaseConnection.</param>
/// <param name="_contextAccessor">Instancia de IHttpContextAccessor.</param>
public class RegistraImpresionService(IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor) : IRegistraImpresionService
{
    /// <inheritdoc />
    public async Task<bool> RegistraImpresion(PostRegistraImpresionDto postRegistraImpresionDto)
    {
        RegistraImpresionRepository _registraImpresionRepository = new(_connection, _contextAccessor);

        await Task.Delay(1);

        _registraImpresionRepository.GuardaImpresionUNI5400(postRegistraImpresionDto, out bool exito);

        return exito;
    }
}


using API_1_TERCEROS_REMESADORAS.Utilities;
using Connections.Abstractions;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.RegistraImpresion;
using System.Data.OleDb;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Repository.IRepository.RegistraImpresion;

/// <summary>
/// Clase RegistraImpresionRepository.
/// </summary>
/// <param name="_connection">IInstancia de IDatabaseConnection.</param>
/// <param name="_contextAccessor">Instancia de IHttpContextAccessor.</param>
public class RegistraImpresionRepository(IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor)
{

    /// <summary>
    /// Actualiza datos en tabla S38FILEBA.UNI5400
    /// </summary>
    /// <param name="postRegistraImpresionDto">Objeto Dto.</param>
    /// <param name="exito">Resultado del guardo de la impresión.</param>
    public void GuardaImpresionUNI5400(PostRegistraImpresionDto postRegistraImpresionDto, out bool exito)
    {
        _connection.Open();
        FieldsQueryL param = new();

        DateTime now = DateTime.Now;

        string numeroTarjeta = postRegistraImpresionDto.NumeroTarjeta;
        string usuarioImpresion = postRegistraImpresionDto.UsuarioICBS;
        int fechaImpresion = now.Year * 10000 + now.Month * 100 + now.Day;
        int horaImpresion = now.Hour * 10000 + now.Minute * 100 + now.Second;

        string sqlQuery = "UPDATE S38FILEBA.UNI5400 SET ST_FECHA_IMPRESION = ?, ST_HORA_IMPRESION = ?,  ST_USUARIO_IMPRESION = ? WHERE ST_CODIGO_TARJETA = ?";
        using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
        command.CommandText = sqlQuery;
        command.CommandType = System.Data.CommandType.Text;


        param.AddOleDbParameter(command, "ST_FECHA_IMPRESION", OleDbType.Numeric, fechaImpresion);
        param.AddOleDbParameter(command, "ST_HORA_IMPRESION", OleDbType.Numeric, horaImpresion);
        param.AddOleDbParameter(command, "ST_USUARIO_IMPRESION", OleDbType.Char, usuarioImpresion);
        param.AddOleDbParameter(command, "ST_CODIGO_TARJETA", OleDbType.Char, numeroTarjeta);

        int update = command.ExecuteNonQuery();

        if (update > 0)
        {
            exito = true;

            GuardaNombreUNI00MTA(postRegistraImpresionDto.NumeroTarjeta, postRegistraImpresionDto.NombreEnTarjeta);
        }
        else
        {
            exito = false;
        }
    }

    /*Buscamos datos de cuenta en  S38FILEBA.UNI00MTA*/
    private void GuardaNombreUNI00MTA(string codigoTarjeta, string nombreEnTarjeta)
    {
        FieldsQueryL param = new();

        string sqlQuery = "UPDATE S38FILEBA.UNI00MTA SET MTNET = ? WHERE MTCTJ = ?";
        using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
        command.CommandText = sqlQuery;
        command.CommandType = System.Data.CommandType.Text;

        param.AddOleDbParameter(command, "MTNET", OleDbType.Char,nombreEnTarjeta);
        param.AddOleDbParameter(command, "MTCTJ", OleDbType.Char, codigoTarjeta);

        command.ExecuteNonQuery();
    }
}

Entregamelo completo para copiar y pegar
