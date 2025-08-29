Si quiero optimizar más el insert y si empleo por ejemplo la clase así:

[TableName("Customers"), Library("BCAH96DTA")]
public class Clientes
{
    [JsonProperty("codigoPostal")]
	[ColumnName("PostalCode"), ParameterType.Int, Size(50), Precision(10)]
    public int CodigoPostal { get; set; }

    [JsonProperty("nombre")]
	[ColumnName("CustomerName"), ParameterType.String, Size(50)]
    public string Nombre { get; set; } = string.Empty;
	
	[JsonProperty("direccion")]
	[ColumnName("Address"), ParameterType.String, Size(500)]
    public string Direccion { get; set; } = string.Empty;

}
Para hacer un insert tipo así, si quiero que inserte toda la data del objeto, solo si tiene el parametro ColumnName

var q1 = new InsertQueryBuilder(Clientes)
    .Build();
	
Para hacer un insert tipo así si solo quiero insertar ciertas columnas, o si necesito hacer otras validaciones como hiciste con InsertarEnIbtSactaAsync y ciertos parametros que se adaptaron.

var q1 = new InsertQueryBuilder(Clientes)
    .IntoColumns("CustomerName" "Address")
    .RowsFromObjects(Clientes)
    .Build();
	
