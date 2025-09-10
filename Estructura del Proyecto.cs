Es posible optimizar el siguiente código empleando RestUtilities.QueryBuilder, y donde se pueda realizar una sola consulta:

using API_1_TERCEROS_REMESADORAS.Utilities;
using Connections.Abstractions;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.Auth;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.MachineInformationService;
using System.Data.Common;
using System.Data.OleDb;
using System.Globalization;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Repository.IRepository.Auth;

/// <summary>
/// Clase Repositorio AuthServiceRepository, registra los intentos de Log.
/// </summary>
/// <param name="_machineInfoService">Instancia de IMachineInfoService.</param>
/// <param name="_connection">Instancia de IDatabaseConnection.</param>
/// <param name="_contextAccessor">Instancia de IHttpContextAccessor.</param>
public class AuthServiceRepository(IMachineInfoService _machineInfoService, IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor)
{
    /// <summary>
    /// Método que registra en tablas los intentos de Log.
    /// </summary>
    /// <param name="_getAuthResponseDto">Objeto de respuesta GetAuthResponseDto, se pasa entre métodos.</param>
    /// <param name="userID">El user ID del usuario que intenta loguearse.</param>
    /// <param name="motivo">Motivo del registro del log.</param>
    /// <param name="exitoso">Estado del logueo.</param>
    /// <param name="idSesion">Id de sesión para registrar en log.</param>
    /// <param name="success">Estado del proceso de logueo.</param>
    /// <returns>Retorna un objeto GetAuthResponseDto</returns>
    public GetAuthResponseDto RegistraLogsUsuario(GetAuthResponseDto _getAuthResponseDto, string userID, string motivo, string exitoso, string idSesion, string success)
    {
        try
        {
            //Consulta el ultimo Correlativo
            PreConsultaCorrelativo(out int correlativo);

            //Registra el Log General
            RegistraLogLoginGeneral(correlativo, userID, motivo, exitoso, idSesion, out DateTime fecha, out MachineInfo machineInfo, out bool succes1);

            //Registra el Log Personal
            RegistraLogPersonal(userID, fecha, exitoso, machineInfo, idSesion, out bool succes2);

            string statusCode = success switch
            {
                "success" => "200",
                "Unauthorized" => "401",
                _ => "400"
            };

            if (succes1 && succes2)
            {
                _getAuthResponseDto.Codigo.Message = motivo;
                _getAuthResponseDto.Codigo.Error = statusCode;
                _getAuthResponseDto.Codigo.Status = success;
                _getAuthResponseDto.Codigo.TimeStamp = string.Format("{0:HH:mm:ss tt}", DateTime.Now);

                return _getAuthResponseDto;
            }
            else
            {
                GetAuthResponseDto getAuthResponseDto = new GetAuthResponseDto();
                getAuthResponseDto.Codigo.Message = "No se pudo guardar datos de log";
                getAuthResponseDto.Codigo.Error = "400";
                getAuthResponseDto.Codigo.Status = "BadRequest";
                getAuthResponseDto.Codigo.TimeStamp = string.Format("{0:HH:mm:ss tt}", DateTime.Now);

                return getAuthResponseDto;
            }
        }
        catch (Exception ex)
        {
            GetAuthResponseDto getAuthResponseDto = new GetAuthResponseDto();
            getAuthResponseDto.Codigo.Message = ex.Message;
            getAuthResponseDto.Codigo.Error = "400";
            getAuthResponseDto.Codigo.Status = "BadRequest";
            getAuthResponseDto.Codigo.TimeStamp = string.Format("{0:HH:mm:ss tt}", DateTime.Now);

            return getAuthResponseDto;
        }
    }

    private void PreConsultaCorrelativo(out int correlativo)
    {
        string sqlQuery = "SELECT * FROM BCAH96DTA.IETD01LOG ORDER BY LOGA01AID DESC";
        using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
        command.CommandText = sqlQuery;
        command.CommandType = System.Data.CommandType.Text;

        using DbDataReader reader = command.ExecuteReader();
        if (reader.Read())
        {
            object correlativo2 = reader.GetValue(reader.GetOrdinal("LOGA01AID"));
            correlativo = Convert.ToInt32(correlativo2);
            correlativo++;
        }
        else
        {
            correlativo = 0;
        }

    }

    private void RegistraLogLoginGeneral(int correlativo, string userID, string motivo, string exitoso, string idSesion, out DateTime as400, out MachineInfo machineInfo, out bool wasSuccessful)
    {
        DateTime nowUTC = DateTime.Now;
        as400 = new(nowUTC.Year, nowUTC.Month, nowUTC.Day, nowUTC.Hour, nowUTC.Minute, nowUTC.Second, DateTimeKind.Local);

        machineInfo = _machineInfoService.GetMachineInfo();

        FieldsQueryL param = new();

        string sqlQuery = "INSERT INTO BCAH96DTA.ETD01LOG (LOGA01AID, LOGA02UID, LOGA03TST,  LOGA04SUC,  LOGA05IPA,  LOGA06MNA,  LOGA07SID,  LOGA08FRE,  LOGA09ACO,  LOGA10UAG,  LOGA11BRO,  LOGA12SOP,  LOGA13DIS) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
        using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
        command.CommandText = sqlQuery;
        command.CommandType = System.Data.CommandType.Text;

        param.AddOleDbParameter(command, "LOGA01AID", OleDbType.Numeric, correlativo); //Numero de Intento
        param.AddOleDbParameter(command, "LOGA02UID", OleDbType.Char, userID); //UserID
        param.AddOleDbParameter(command, "LOGA03TST", OleDbType.DBTimeStamp, as400); //Hora y Fecha
        param.AddOleDbParameter(command, "LOGA04SUC", OleDbType.Char, exitoso); //Exitoso o no
        param.AddOleDbParameter(command, "LOGA05IPA", OleDbType.Char, machineInfo.ClientIPAddress); //Ip Cliente
        param.AddOleDbParameter(command, "LOGA06MNA", OleDbType.Char, machineInfo.HostName); //Nombre Maquina
        param.AddOleDbParameter(command, "LOGA07SID", OleDbType.Char, idSesion); //ID Sesion
        param.AddOleDbParameter(command, "LOGA08FRE", OleDbType.Char, motivo); //Razon Fallo
        param.AddOleDbParameter(command, "LOGA09ACO", OleDbType.Numeric, 0); //Conteo Intentos
        param.AddOleDbParameter(command, "LOGA10UAG", OleDbType.Char, machineInfo.UserAgent); //User Agent
        param.AddOleDbParameter(command, "LOGA11BRO", OleDbType.Char, machineInfo.Browser); //Navegador
        param.AddOleDbParameter(command, "LOGA12SOP", OleDbType.Char, machineInfo.OS); //Sistema Operativo
        param.AddOleDbParameter(command, "LOGA13DIS", OleDbType.Char, machineInfo.Device);//Dispositivo  modificar luego

        int resultado = command.ExecuteNonQuery();
        wasSuccessful = resultado > 0;
    }

    private void RegistraLogPersonal(string userID, DateTime fecha, string exitoso, MachineInfo machineInfo, string token, out bool wasSuccessful)
    {
        bool bandera = false;
        int intentos = 0;
        DateTime fechaAnterior = DateTime.MinValue;

        /*PreConsulta Registro Usuario*/
        FieldsQueryL param = new();

        string sqlQuery = "SELECT * FROM BCAH96DTA.IETD02LOG WHERE LOGB01UID = ?";

        using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
        command.CommandText = sqlQuery;
        command.CommandType = System.Data.CommandType.Text;

        param.AddOleDbParameter(command, "LOGB01UID", OleDbType.Char, userID);

        using DbDataReader reader = command.ExecuteReader();

        if (reader.Read())
        {
            string fecha2 = reader["LOGB02UIL"].ToString()!;
            if (DateTime.TryParseExact(fecha2, "yyyy-MM-dd-HH.mm.ss.ffffff", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out fechaAnterior)) { }

            object intentos1 = reader.GetValue(reader.GetOrdinal("LOGB03TIL"));
            intentos = Convert.ToInt32(intentos1);
            bandera = true;
        }

        reader.Close();

        /*Inserta Registro Usuario si no existe*/
        if (exitoso.Equals("1"))
        {
            intentos++;
        }
        else if (exitoso.Equals("0"))
        {
            intentos = 0;
        }

        if (bandera)
        {
            //Actualiza

            sqlQuery = "UPDATE BCAH96DTA.IETD02LOG SET LOGB02UIL = ?, LOGB03TIL = ?, LOGB04SEA = ?,  LOGB05UDI = ?, LOGB06UTD = ?, LOGB07UNA = ?, LOGB09UIF = ?, LOGB10TOK = ?  WHERE LOGB01UID = ?";
            using var command2 = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            command2.CommandText = sqlQuery;
            command2.CommandType = System.Data.CommandType.Text;

            param.AddOleDbParameter(command2, "LOGB02UIL", OleDbType.DBTimeStamp, fecha);
            param.AddOleDbParameter(command2, "LOGB03TIL", OleDbType.Numeric, intentos); //Cantidad Intentos
            param.AddOleDbParameter(command2, "LOGB04SEA", OleDbType.Char, exitoso); //Sesion Activa
            param.AddOleDbParameter(command2, "LOGB05UDI", OleDbType.Char, machineInfo.ClientIPAddress);//Ultima Dirección IP
            param.AddOleDbParameter(command2, "LOGB06UTD", OleDbType.Char, machineInfo.Device);//Ultimo Dispositivo
            param.AddOleDbParameter(command2, "LOGB07UNA", OleDbType.Char, machineInfo.Browser);//Ultimo Navegador
            param.AddOleDbParameter(command2, "LOGB09UIF", OleDbType.DBTimeStamp, fechaAnterior); //Ultimo intento
            param.AddOleDbParameter(command2, "LOGB10TOK", OleDbType.Char, token); //Token/idSession
            param.AddOleDbParameter(command2, "LOGB01UID", OleDbType.Char, userID);

            int resultado = command2.ExecuteNonQuery();
            wasSuccessful = resultado > 0;
        }
        else
        {
            //Inserta
            sqlQuery = "INSERT INTO BCAH96DTA.ETD02LOG (LOGB01UID, LOGB02UIL, LOGB03TIL,  LOGB04SEA,  LOGB05UDI,  LOGB06UTD,  LOGB07UNA,  LOGB08CBI,  LOGB09UIF,  LOGB10TOK) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
            using var command2 = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            command2.CommandText = sqlQuery;
            command2.CommandType = System.Data.CommandType.Text;

            param.AddOleDbParameter(command2, "LOGB01UID", OleDbType.Char, userID);
            param.AddOleDbParameter(command2, "LOGB02UIL", OleDbType.DBTimeStamp, fecha);
            param.AddOleDbParameter(command2, "LOGB03TIL", OleDbType.Numeric, intentos); //Cantidad Intentos
            param.AddOleDbParameter(command2, "LOGB04SEA", OleDbType.Char, exitoso); //Sesion Activa
            param.AddOleDbParameter(command2, "LOGB05UDI", OleDbType.Char, machineInfo.ClientIPAddress);//Ultima Dirección IP
            param.AddOleDbParameter(command2, "LOGB06UTD", OleDbType.Char, machineInfo.Device);//Ultimo Dispositivo
            param.AddOleDbParameter(command2, "LOGB07UNA", OleDbType.Char, machineInfo.Browser);//Ultimo Navegador
            param.AddOleDbParameter(command2, "LOGB08CBI", OleDbType.Char, "");// Bloqueo Por intento
            param.AddOleDbParameter(command2, "LOGB09UIF", OleDbType.DBTimeStamp, fecha); //Ultimo intento
            param.AddOleDbParameter(command2, "LOGB10TOK", OleDbType.Char, token); //Token/idSession

            int resultado = command2.ExecuteNonQuery();
            wasSuccessful = resultado > 0;
        }
    }
}
               
