Convierte esta clase para que utilice RestUtilities.QueryBuilder:

using Connections.Abstractions;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Models.Dtos.AutenticacionDtos;
using SUNITP.LIB.ManagerProcedures;
using SUNITP.LIB.ManagerProcedures.Concrete;
using SUNITP.LIB.QueryStringGenerator;
using System.Data.OleDb;
using System.Globalization;

namespace MS_BAN_38_UTH_RECAUDACION_PAGOS.Utils;
/// <summary>
/// Clase Utilitaria Token, contiene multiples métodos requeridos para manipular los tokens de Ginih.
/// </summary>
public class Token 
{
    private readonly IDatabaseConnection _connection;
    private readonly IHttpContextAccessor _contextAccessor;
    private EasyMappingTool response = new();
    private readonly string? _tableName = "TOKENUTH";
    private readonly string? _library = "BCAH96DTA";
    private readonly string _status = string.Empty;
    private readonly string _message = string.Empty;
    private readonly string _rToken = string.Empty;
    private readonly string _createdAt = string.Empty;
    private readonly string _timeStamp = string.Empty;
    private readonly string _value = string.Empty;
    private readonly string _name = string.Empty;
    private string _vence = string.Empty;
    private string _creado = string.Empty;
    private string _token = string.Empty;

    /// <summary>
    /// Constructor de la Clase Token.
    /// </summary>
    /// <param name="tokenStructure">Instancia de PostLoginResponseDto</param>
    /// <param name="connection">Instancia de IDatabaseConnection. </param>
    /// <param name="contextAccessor"></param>
    public Token(PostLoginResponseDto tokenStructure, IDatabaseConnection connection, IHttpContextAccessor contextAccessor)
    {
        _status = tokenStructure.Status;
        _message = tokenStructure.Message;
        _rToken = tokenStructure.Data.RefreshToken;
        _createdAt = tokenStructure.Data.CreatedAt;
        _timeStamp = tokenStructure.TimeStamp;
        _value = tokenStructure.Code.Value;
        _name = tokenStructure.Code.Name;
        _connection = connection;
        _contextAccessor = contextAccessor;
    }

    /// <summary>
    /// Constructo de Clase Token sin parametros de ingreso, inicializa campos.
    /// </summary>
    public Token(IDatabaseConnection connection, IHttpContextAccessor contextAccessor)
    {
        _connection = connection;
        _contextAccessor = contextAccessor;
    }

    /// <summary>
    /// Guarda el Token en la tabla en el as400
    /// </summary>
    /// <returns>Retorna un valor boleano segun sea exitoso o no el almacenamiento</returns>
    public bool SavenTokenUTH()
    {
        try
        {
            var temp = DateTime.ParseExact(_createdAt, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture).AddYears(2);
            _vence = temp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            _connection.Open();

            var oleDBCommand = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            if (oleDBCommand.Connection is not OleDbConnection oleDbConnection)
            {
                return false;
            }
            var oleDBConnection = (OleDbConnection)oleDBCommand.Connection;

            var sqsg = new ServiceQueryStringGenerator();
            EasyCrudDataModels ecdm = new(oleDBConnection);
            var fQuery = new FieldsQuery();
            var validacion = fQuery.FieldQuery("ID", "1", OleDbType.Integer, 1, "0");

            sqsg._iQueryStringGenerator.SelectAll();
            sqsg._iQueryStringGenerator.From(_library, _tableName);
            sqsg._iQueryStringGenerator.WhereAnd(validacion, "=");
            var responseS = ecdm.SelectExecute(sqsg);

            if (responseS.Count == 0)
            {
                sqsg = new ServiceQueryStringGenerator();
                sqsg._iQueryStringGenerator.InsertIntoFrom(_library, _tableName);
                sqsg._iQueryStringGenerator.InsertValue("ID", "1", OleDbType.Integer, 1, 0);
                sqsg._iQueryStringGenerator.InsertValue("STATUS", _status, OleDbType.VarChar, 50, 0);
                sqsg._iQueryStringGenerator.InsertValue("MESSAGE", _message, OleDbType.VarChar, 100, 0);
                sqsg._iQueryStringGenerator.InsertValue("RTOKEN", _rToken, OleDbType.VarChar, 2000, 0);
                sqsg._iQueryStringGenerator.InsertValue("CREATEDAT", _createdAt, OleDbType.VarChar, 100, 0);
                sqsg._iQueryStringGenerator.InsertValue("TIMESTAMP", _timeStamp, OleDbType.VarChar, 100, 0);
                sqsg._iQueryStringGenerator.InsertValue("VALUE", _value, OleDbType.VarChar, 3, 0);
                sqsg._iQueryStringGenerator.InsertValue("NAME", _name, OleDbType.VarChar, 100, 0);
                sqsg._iQueryStringGenerator.InsertValue("VENCE", _vence, OleDbType.VarChar, 100, 0);

                //respuesta del query
                response = ecdm.InsertExecute(sqsg);
            }
            else
            {
                sqsg = new ServiceQueryStringGenerator();
                sqsg._iQueryStringGenerator.UpdateFrom(_library, _tableName);
                sqsg._iQueryStringGenerator.UpdateSet("ID", "1", OleDbType.Integer, 1, 0);
                sqsg._iQueryStringGenerator.UpdateSet("STATUS", _status, OleDbType.VarChar, 50, 0);
                sqsg._iQueryStringGenerator.UpdateSet("MESSAGE", _message, OleDbType.VarChar, 100, 0);
                sqsg._iQueryStringGenerator.UpdateSet("RTOKEN", _rToken, OleDbType.VarChar, 2000, 0);
                sqsg._iQueryStringGenerator.UpdateSet("CREATEDAT", _createdAt, OleDbType.VarChar, 100, 0);
                sqsg._iQueryStringGenerator.UpdateSet("TIMESTAMP", _timeStamp, OleDbType.VarChar, 100, 0);
                sqsg._iQueryStringGenerator.UpdateSet("VALUE", _value, OleDbType.VarChar, 3, 0);
                sqsg._iQueryStringGenerator.UpdateSet("NAME", _name, OleDbType.VarChar, 100, 0);
                sqsg._iQueryStringGenerator.UpdateSet("VENCE", _vence, OleDbType.VarChar, 100, 0);
                sqsg._iQueryStringGenerator.WhereAnd(validacion, "=");

                //respuesta del query
                response = ecdm.UpdateExecute(sqsg);
            }

            if (response.GetEasyParameter("_defaultError").value.Equals(""))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            //Temporal hasta manejar los logs
            Console.Clear();
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Metodo que obtiene el token secreto de la tabla en el as400
    /// </summary>
    /// <param name="rToken">Token registrado.</param>
    /// <returns>Retorna un string de tipo out por parametro</returns>

    public bool GetToken(out string rToken)
    {
        try
        {
            _connection.Open();
            var oleDBCommand = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            if (oleDBCommand.Connection is not OleDbConnection oleDbConnection)
            {
                rToken = "";
                return false;
            }

            var oleDBConnection = (OleDbConnection)oleDBCommand.Connection;

            var sqsg = new ServiceQueryStringGenerator();
            EasyCrudDataModels ecdm = new(oleDBConnection);

            var fQuery = new FieldsQuery();
            var validacion = fQuery.FieldQuery("ID", "1", OleDbType.Integer, 1, "0");

            sqsg._iQueryStringGenerator.SelectAll();
            sqsg._iQueryStringGenerator.From(_library, _tableName);
            sqsg._iQueryStringGenerator.WhereAnd(validacion, "=");
            var responseS = ecdm.SelectExecute(sqsg);

            //Cerrar Conexión
            _connection.Close();

            if (responseS.Count == 0)
            {
                rToken = "";
                return false;
            }

            foreach (var item in responseS)
            {
                _vence = item.GetValue("VENCE");
                _creado = item.GetValue("CREATEDAT");
                _token = item.GetValue("RTOKEN");
            }

            DateTime date1 = DateTime.ParseExact(_vence, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
            DateTime date2 = DateTime.ParseExact(_creado, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

            var diferenceTime = date1 - date2;

            if (diferenceTime.Days > 0)
            {
                rToken = _token;
                return true;
            }
            rToken = "";
            return false;
        }
        catch (Exception ex)
        {
            //Temporal hasta manejar los logs
            Console.Clear();
            Console.WriteLine(ex.Message);
            _connection.Close();
            rToken = "";
            return false;
        }

    }
}
