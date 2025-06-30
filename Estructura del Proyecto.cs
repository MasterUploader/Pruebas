string sqlQuery = @"
INSERT INTO BCAH96DTA.UTH01CCC (
    CCC00GUID, CCC01CORR, CCC02FECH, CCC03HORA, CCC04CAJE, CCC05BANC, CCC06SUCU, CCC07TERM, CCC08LIMI, CCC09NTOK, 
    CCC10STAT, CCC11MESS, CCC12DTID, CCC12DTNA, CCC13DTPO, CCC13TIST, CCC14MDTI, CCC15MDHM, CCC16MDNT, CCC17COVA, 
    CCC18CONA, CCC19ERRO, CCC20MENS
) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
    
using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
command.CommandText = sqlQuery;
command.CommandType = CommandType.Text;

AddOleDbParameter(command, "CCC00GUID", OleDbType.Char, getCompaniesDto.CamposObligatoriosModel.Guid);
AddOleDbParameter(command, "CCC01CORR", OleDbType.Char, correlativo.ToString());
AddOleDbParameter(command, "CCC02FECH", OleDbType.Char, getCompaniesDto.CamposObligatoriosModel.Fecha);
AddOleDbParameter(command, "CCC03HORA", OleDbType.Char, getCompaniesDto.CamposObligatoriosModel.Hora);
AddOleDbParameter(command, "CCC04CAJE", OleDbType.Char, getCompaniesDto.CamposObligatoriosModel.Cajero);
AddOleDbParameter(command, "CCC05BANC", OleDbType.Char, getCompaniesDto.CamposObligatoriosModel.Banco);
AddOleDbParameter(command, "CCC06SUCU", OleDbType.Char, getCompaniesDto.CamposObligatoriosModel.Sucursal);
AddOleDbParameter(command, "CCC07TERM", OleDbType.Char, getCompaniesDto.CamposObligatoriosModel.Terminal);
AddOleDbParameter(command, "CCC08LIMI", OleDbType.Char, getCompaniesDto.Limit);
AddOleDbParameter(command, "CCC09NTOK", OleDbType.Char, getCompaniesDto.NextToken);
AddOleDbParameter(command, "CCC10STAT", OleDbType.Char, list2.Status);
AddOleDbParameter(command, "CCC11MESS", OleDbType.Char, list2.Message);
AddOleDbParameter(command, "CCC12DTID", OleDbType.Char, list3.Id);
AddOleDbParameter(command, "CCC12DTNA", OleDbType.Char, list3.Name);
AddOleDbParameter(command, "CCC13DTPO", OleDbType.Char, list3.PayableOptions == null ? "Nulo" : string.Join(",", list3.PayableOptions));
AddOleDbParameter(command, "CCC13TIST", OleDbType.Char, list2.Timestamp.ToString());
AddOleDbParameter(command, "CCC14MDTI", OleDbType.Char, list2.Metadata.Items);
AddOleDbParameter(command, "CCC15MDHM", OleDbType.Char, list2.Metadata.HasMore?.ToString() ?? "NO");
AddOleDbParameter(command, "CCC16MDNT", OleDbType.Char, list2.Metadata.NextToken ?? "");
AddOleDbParameter(command, "CCC17COVA", OleDbType.Char, list2.Code.Value?.ToString() ?? "124");
AddOleDbParameter(command, "CCC18CONA", OleDbType.Char, list2.Code.Name ?? "");
AddOleDbParameter(command, "CCC19ERRO", OleDbType.Char, getCompaniesResponseDto.Error);
AddOleDbParameter(command, "CCC20MENS", OleDbType.Char, getCompaniesResponseDto.Mensaje ?? "Proceso ejecutado Satisfactoriamente");

command.ExecuteNonQuery();

/// <summary>
/// Agrega un parámetro a un DbCommand, compatible con OleDbCommand y decoradores como LoggingDatabaseCommand.
/// </summary>
/// <param name="cmd">Comando al cual se agregará el parámetro.</param>
/// <param name="name">Nombre del parámetro.</param>
/// <param name="type">Tipo OleDb del parámetro.</param>
/// <param name="value">Valor del parámetro.</param>
private void AddOleDbParameter(DbCommand cmd, string name, OleDbType type, object? value)
{
    var param = cmd.CreateParameter();
    param.ParameterName = name;
    if (param is OleDbParameter oleParam)
        oleParam.OleDbType = type;
    param.Value = value ?? DBNull.Value;
    cmd.Parameters.Add(param);
}
