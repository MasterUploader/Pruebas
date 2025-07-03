El codigo del insert que sea similar a este o en su defecto usando CreateParameter

int correlativo = 0;
FieldsQuery param = new();

string sqlQuery = "INSERT INTO BCAH96DTA.tabla (campos) VALUES(?)";
using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
command.CommandText = sqlQuery;

command.CommandType = System.Data.CommandType.Text;

param.AddOleDbParameter(command, "APU00GUID", OleDbType.Char, getPaymentsDto.CamposObligatoriosModel.Guid);
param.AddOleDbParameter(command, "APU01CORR", OleDbType.Numeric, correlativo);
param.AddOleDbParameter(command, "APU02FECH", OleDbType.Char, getPaymentsDto.CamposObligatoriosModel.Fecha);
param.AddOleDbParameter(command, "APU03HORA", OleDbType.Char, getPaymentsDto.CamposObligatoriosModel.Hora);
param.AddOleDbParameter(command, "APU04CAJE", OleDbType.Char, getPaymentsDto.CamposObligatoriosModel.Cajero);
param.AddOleDbParameter(command, "APU05BANC", OleDbType.Char, getPaymentsDto.CamposObligatoriosModel.Banco);
param.AddOleDbParameter(command, "APU06SUCU", OleDbType.Char, getPaymentsDto.CamposObligatoriosModel.Sucursal);
param.AddOleDbParameter(command, "APU07TERM", OleDbType.Char, getPaymentsDto.CamposObligatoriosModel.Terminal);
param.AddOleDbParameter(command, "APU08STAT", OleDbType.Char, getPaymentsResponseDto.Status);
param.AddOleDbParameter(command, "APU09MSSG", OleDbType.Char, getPaymentsResponseDto.Message);
param.AddOleDbParameter(command, "APU10DTID", OleDbType.Char, getPaymentsResponseDto.Data.Id);
param.AddOleDbParameter(command, "APU11DTNA", OleDbType.Char, getPaymentsResponseDto.Data.Name);
param.AddOleDbParameter(command, "APU12CUID", OleDbType.Char, getPaymentsResponseDto.Data.Customer.Id);
param.AddOleDbParameter(command, "APU13CUNA", OleDbType.Char, getPaymentsResponseDto.Data.Customer.Name);
param.AddOleDbParameter(command, "APU14COID", OleDbType.Char, getPaymentsResponseDto.Data.Company.Id);
param.AddOleDbParameter(command, "APU15CONA", OleDbType.Char, getPaymentsResponseDto.Data.Company.Name);
param.AddOleDbParameter(command, "APU16DREF", OleDbType.Char, getPaymentsResponseDto.Data.DocumentReference);
param.AddOleDbParameter(command, "APU17REFE", OleDbType.Char, getPaymentsResponseDto.Data.ReferenceId);
param.AddOleDbParameter(command, "APU18AMVA", OleDbType.Char, getPaymentsResponseDto.Data.Amount.Value);
param.AddOleDbParameter(command, "APU19AMCU", OleDbType.Char, getPaymentsResponseDto.Data.Amount.Currency);
param.AddOleDbParameter(command, "APU20SUTO", OleDbType.Char, getPaymentsResponseDto.Data.Amount.Breakdown.Subtotal);
param.AddOleDbParameter(command, "APU19PFEE", OleDbType.Char, getPaymentsResponseDto.Data.Amount.Breakdown.ProcessingFee);
param.AddOleDbParameter(command, "APU20SCHA", OleDbType.Char, getPaymentsResponseDto.Data.Amount.Breakdown.Surcharge);
param.AddOleDbParameter(command, "APU21DICO", OleDbType.Char, getPaymentsResponseDto.Data.Amount.Breakdown.Discount);
param.AddOleDbParameter(command, "APU22BTAX", OleDbType.Char, getPaymentsResponseDto.Data.Amount.Breakdown.Tax);
param.AddOleDbParameter(command, "APU23TOTA", OleDbType.Char, getPaymentsResponseDto.Data.Amount.Breakdown.Total);
param.AddOleDbParameter(command, "APU24CRAT", OleDbType.Char, getPaymentsResponseDto.Data.CreatedAt);
param.AddOleDbParameter(command, "APU25TIST", OleDbType.Char, getPaymentsResponseDto.TimeStamp);
param.AddOleDbParameter(command, "APU26COVA", OleDbType.Char, getPaymentsResponseDto.Code.Value);
param.AddOleDbParameter(command, "APU27CONA", OleDbType.Char, getPaymentsResponseDto.Code.Name);
param.AddOleDbParameter(command, "APU28ERRO", OleDbType.Char, getPaymentsResponseDto.Error);
param.AddOleDbParameter(command, "APU29MENS", OleDbType.Char, getPaymentsResponseDto.Mensaje);

command.ExecuteNonQuery();

REP00UID es unico
REP01DCOR es de 1 a M
REP13DCOR es de 1 a M

Pueden haber muchos REP01DCOR y dentro de este muchos REP13DCOR.
