Necesito optimizar este código, porque por el momento realiza una búsqueda y según el resultado va elemento a elemento obteniendo el resulto para cada fila en otras consultas, quiero que apliques la librería RestUtilities.QueryBuilder si es posible, para poder utilizar un join u otro para realizar una búsqueda instantánea que devuelva dicha lista, te dejo el código.

  
using API_1_TERCEROS_REMESADORAS.Utilities;
using Connections.Abstractions;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.DetalleTarjetaImprimir;
using MS_BAN_43_Embosado_Tarjetas_Debito.Utils;
using System.Data.Common;
using System.Data.OleDb;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Repository.IRepository.DetalleTarjetaImprimir;

/// <summary>
/// Clase repositorio DetalleTarjetasImprimir.
/// </summary>
/// <param name="_connection">IInstancia de IDatabaseConnection.</param>
/// <param name="_contextAccessor">Instancia de IHttpContextAccessor.</param>
public class DetalleTarjetasImprimirRepository(IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor)
{

    /// <inheritdoc />
    protected GetDetallesTarjetasImprimirResponseDto _getDetalleTarjetasImprimirResponseDto = new();

    /// <summary>
    /// Busca datos en tabla S38FILEBA.UNI5400
    /// </summary>
    /// <param name="bin">Parametro de código BIN único, exclusivo para cada producto.</param>
    /// <param name="agenciaImprime">Código de la agencia en la que se imprimira la tarjeta.</param>
    /// <param name="agenciaApertura">Código de la Agencia donde se imprimira la tarjeta.</param>
    /// <returns>Retorna una respuesta Http de tipo GetDetallesTarjetasImprimirResponseDto.</returns>
    public async Task<GetDetallesTarjetasImprimirResponseDto> BusquedaUNI5400(string bin, int agenciaImprime, int agenciaApertura)
    {
        int bandera1 = 0;
        try
        {
            FieldsQueryL param = new();
            int dia = Convert.ToInt32(GlobalConnection.Current.DiasConsultaTarjeta);

            DateTime today = DateTime.Today;
            DateTime daysAgo = today.AddDays(-dia);
            int numericDate = int.Parse(daysAgo.ToString("yyyyMMdd"));

            //Tabla S38FILEBA.UNI5400, extrae la lista de tarjetas que estan disponibles a imprimir para la combinación de agencia de apertura y agencia que imprime.
            string sqlQuery = "SELECT * FROM S38FILEBA.UNI54L07 WHERE ST_BIN_TARJETA = ? AND ST_CENTRO_COSTO_IMPR_TARJETA = ? AND ST_CENTRO_COSTO_APERTURA = ? AND ST_FECHA_IMPRESION = ? AND ST_HORA_IMPRESION = ? AND ST_USUARIO_IMPRESION = ? AND ST_FECHA_APERTURA >= ? ORDER BY ST_CODIGO_TARJETA , ST_CENTRO_COSTO_IMPR_TARJETA , ST_CENTRO_COSTO_APERTURA , ST_FECHA_IMPRESION , ST_HORA_IMPRESION , ST_USUARIO_IMPRESION";

            using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            command.CommandText = sqlQuery;
            command.CommandType = System.Data.CommandType.Text;
            command.CommandTimeout = 0;

            param.AddOleDbParameter(command, "ST_BIN_TARJETA", OleDbType.Char, bin);
            param.AddOleDbParameter(command, "ST_CENTRO_COSTO_IMPR_TARJETA", OleDbType.Numeric, agenciaImprime);
            param.AddOleDbParameter(command, "ST_CENTRO_COSTO_APERTURA", OleDbType.Numeric, agenciaApertura);
            param.AddOleDbParameter(command, "ST_FECHA_IMPRESION", OleDbType.Numeric, 0);
            param.AddOleDbParameter(command, "ST_HORA_IMPRESION", OleDbType.Numeric, 0);
            param.AddOleDbParameter(command, "ST_USUARIO_IMPRESION", OleDbType.Char, "");
            param.AddOleDbParameter(command, "ST_FECHA_APERTURA", OleDbType.Numeric, numericDate);

            using DbDataReader reader = await command.ExecuteReaderAsync();

            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    string codigoAgenciaImprime = Convert.ToString(reader.GetValue(reader.GetOrdinal("ST_CENTRO_COSTO_IMPR_TARJETA")))!;
                    string codigoAgenciaApertura = Convert.ToString(reader.GetValue(reader.GetOrdinal("ST_CENTRO_COSTO_APERTURA")))!;
                    int estatusProcesoTarjeta = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("ST_ESTATUS_PROCESO_TARJETA")));
                    string codigoTarjeta = reader.GetString(reader.GetOrdinal("ST_CODIGO_TARJETA"));

                    //Buscamos el nombre de la agencia que aperturo y la que imprime utilizando el codigo de agencia
                    if (bandera1 == 0)
                    {
                        BusquedaCFP10201(codigoAgenciaImprime, codigoAgenciaImprime, 0, out int branchNumber1, out string branchName1);
                        BusquedaCFP10201(codigoAgenciaApertura, codigoAgenciaApertura, 0, out int branchNumber2, out string branchName2);

                        _getDetalleTarjetasImprimirResponseDto.Agencia.AgenciaAperturaCodigo = branchNumber2.ToString();
                        _getDetalleTarjetasImprimirResponseDto.Agencia.AgenciaAperturaNombre = branchName1;
                        _getDetalleTarjetasImprimirResponseDto.Agencia.AgenciaImprimeCodigo = branchNumber1.ToString();
                        _getDetalleTarjetasImprimirResponseDto.Agencia.AgenciaImprimeNombre = branchName2;
                        bandera1 = 1;
                    }
                    /*Posteriormente buscamos la información detallada de cada tarjeta*/

                    //Buscamos Estado de impresion
                    BusquedaUNI5500(estatusProcesoTarjeta, out string esNombreEstatus);

                    //Buscamos Nombre en Tarjeta
                    BusquedaUNI00MTA(codigoTarjeta, out string nombreEnTarjeta);

                    //Buscamos Numero de Cuenta
                    BusquedaUNI01CAS(codigoTarjeta, out string numeroCuenta);

                    _getDetalleTarjetasImprimirResponseDto.Tarjetas.Add(new Tarjetas()
                    {
                        Nombre = nombreEnTarjeta,
                        Numero = codigoTarjeta,
                        FechaEmision = "",
                        FechaVencimiento = "",
                        Motivo = esNombreEstatus,
                        NumeroCuenta = numeroCuenta

                    });
                }
            }

            _getDetalleTarjetasImprimirResponseDto.Codigo.Message = "Exitoso";
            _getDetalleTarjetasImprimirResponseDto.Codigo.Status = "success";
            _getDetalleTarjetasImprimirResponseDto.Codigo.Error = "200";
            _getDetalleTarjetasImprimirResponseDto.Codigo.TimeStamp = string.Format("{0:HH:mm:ss tt}", DateTime.Now);

            return _getDetalleTarjetasImprimirResponseDto;
        }
        catch (Exception ex)
        {
            GetDetallesTarjetasImprimirResponseDto getDetalleTarjetasImprimirResponseDto = new();

            getDetalleTarjetasImprimirResponseDto.Codigo.Message = ex.Message;
            getDetalleTarjetasImprimirResponseDto.Codigo.Status = "BadRequest";
            getDetalleTarjetasImprimirResponseDto.Codigo.Error = "400";
            getDetalleTarjetasImprimirResponseDto.Codigo.TimeStamp = string.Format("{0:HH:mm:ss tt}", DateTime.Now);
            return getDetalleTarjetasImprimirResponseDto;
        }
    }

    /*Buscar Datos en tabla BNKPRD01.CFP10201*/
    private void BusquedaCFP10201(string agenciaImprime, string agenciaApertura, int bandera, out int branchNumber, out string branchName)
    {
        try
        {
            FieldsQueryL param = new();

            branchNumber = 0;
            branchName = "";

            string sqlQuery = "SELECT * FROM BNKPRD01.CFP10201 WHERE CFBANK = ? AND CFBRCH = ? ORDER BY CFBANK, CFBRCH";

            using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            command.CommandText = sqlQuery;
            command.CommandType = System.Data.CommandType.Text;

            param.AddOleDbParameter(command, "CFBANK", OleDbType.Numeric, 1);
            param.AddOleDbParameter(command, "CFBRCH", OleDbType.Numeric, bandera == 1 || bandera == 0 ? int.Parse(agenciaImprime) : int.Parse(agenciaApertura));

            using DbDataReader reader = command.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read() && bandera == 0)
                {
                    branchNumber = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("CFBRCH")));
                    branchName = reader.GetString(reader.GetOrdinal("CFBRNM"));
                    bandera = 1;
                }
            }
        }
        catch (Exception ex)
        {
            branchNumber = 0;
            branchName = ex.Message;
        }
    }

    /*Buscamos datos de cuenta en  S38FILEBA.UNI00MTA*/
    private void BusquedaUNI00MTA(string codigoTarjeta, out string nombreEnTarjeta)
    {
        try
        {
            FieldsQueryL param = new();

            nombreEnTarjeta = "";

            //Tabla S38FILEBA.UNI00MTA
            string sqlQuery = "SELECT * FROM S38FILEBA.UNI00L33 WHERE MT_CTJ_COD_TARJETA = ? ORDER BY  MT_CTJ_COD_TARJETA";

            using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            command.CommandText = sqlQuery;
            command.CommandType = System.Data.CommandType.Text;

            param.AddOleDbParameter(command, "MT_CTJ_COD_TARJETA", OleDbType.Char, codigoTarjeta);

            using DbDataReader reader = command.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    nombreEnTarjeta = reader.GetString(reader.GetOrdinal("MT_NET_EMBOSE"));
                }
            }
        }
        catch (Exception ex)
        {
            nombreEnTarjeta = ex.Message;
        }
    }

    /*Buscamos Datos de numero de cuenta en S38FILEBA.UNI01CAS*/
    private void BusquedaUNI01CAS(string CA_CTJ_COD, out string numeroCuenta)
    {
        try
        {
            FieldsQueryL param = new();

            numeroCuenta = "";
            //Tabla S38FILEBA.UNI01CAS
            string sqlQuery = "SELECT * FROM S38FILEBA.UNI01L19 WHERE CA_CTJ_COD_TARJETA = ? AND ( CA_TIPO_CUENTA = ? OR CA_TIPO_CUENTA = ?) AND CA_IUE_USO_ESPECIAL = ? ORDER BY  CA_CTJ_COD_TARJETA";

            using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            command.CommandText = sqlQuery;
            command.CommandType = System.Data.CommandType.Text;
            command.CommandTimeout = 0;

            param.AddOleDbParameter(command, "CA_CTJ_COD_TARJETA", OleDbType.Char, CA_CTJ_COD.ToString());
            param.AddOleDbParameter(command, "CA_TIPO_CUENTA", OleDbType.Numeric, 10);
            param.AddOleDbParameter(command, "CA_TIPO_CUENTA", OleDbType.Numeric, 20);
            param.AddOleDbParameter(command, "CA_IUE_USO_ESPECIAL", OleDbType.Char, "R");

            using DbDataReader reader = command.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    numeroCuenta = reader.GetValue(reader.GetOrdinal("CA_CTA_COD_CUENTA")).ToString()!;
                }
            }
        }
        catch (Exception ex)
        {
            var txt = ex.Message;
            numeroCuenta = "";
        }
    }    

    /*Busca Datos de estatus de impresion en Tabla S38FILEBA.UNI5500*/
    private void BusquedaUNI5500(int estatusProcesoTarjeta, out string esNombreEstatus)
    {
        try
        {
            FieldsQueryL param = new();

            esNombreEstatus = "";
            string sqlQuery = "SELECT * FROM S38FILEBA.UNI5500 WHERE ES_ESTATUS_TARJETA = ? ORDER BY  ES_ESTATUS_TARJETA";

            using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            command.CommandText = sqlQuery;
            command.CommandType = System.Data.CommandType.Text;

            param.AddOleDbParameter(command, "ES_ESTATUS_TARJETA", OleDbType.Numeric, estatusProcesoTarjeta);

            using DbDataReader reader = command.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    esNombreEstatus = reader.GetString(reader.GetOrdinal("ES_NOMBRE_ESTATUS"));
                }
            }
        }
        catch (Exception ex)
        {
            esNombreEstatus = ex.Message;
        }
    }
}
