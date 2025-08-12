Convierte este codigo para usar la libreria


 _connection.Open();

 string clleProgramCall = "{CALL BCAH96.PQRIFZ04CL(?, ?, ?, ?, ?, ?, ?) }";
 await using (OleDbCommand command = new(clleProgramCall, connection.Connect.OleDbConnection))
 {
     //PARAMETROS DE ENTRADA
     command.Parameters.AddWithValue("EMAIL", iniciarSesionDto.Email);
     command.Parameters.AddWithValue("PASSWORD", iniciarSesionDto.Password);

     //PARAMETROS DE SALIDA

     OleDbParameter name = new("NAME", OleDbType.Char, 50);
     name.Direction = System.Data.ParameterDirection.Output;
     command.Parameters.Add(name);

     OleDbParameter type = new("TYPE", OleDbType.Char, 1);
     type.Direction = System.Data.ParameterDirection.Output;
     command.Parameters.Add(type);


     OleDbParameter roleId = new("ROLEDID", OleDbType.Decimal);
     roleId.Precision = 10;
     roleId.Scale = 0;
     roleId.Direction = System.Data.ParameterDirection.Output;
     command.Parameters.Add(roleId);

     OleDbParameter responseCode = new("RESPCODE", OleDbType.Decimal);
     responseCode.Precision = 3;
     responseCode.Scale = 0;
     responseCode.Direction = System.Data.ParameterDirection.Output;
     command.Parameters.Add(responseCode);

     OleDbParameter responseDescri = new("RESPDESCRI", OleDbType.Char, 80);
     responseDescri.Direction = System.Data.ParameterDirection.Output;
     command.Parameters.Add(responseDescri);

     await command.ExecuteNonQueryAsync();

     //Captura de Parametros devueltos
     string nombre = command.Parameters["NAME"].Value != DBNull.Value ? command.Parameters["NAME"].Value?.ToString() : null;
     string email = command.Parameters["EMAIL"].Value != DBNull.Value ? command.Parameters["EMAIL"].Value?.ToString() : null;
     string tipo = command.Parameters["TYPE"].Value != DBNull.Value ? command.Parameters["TYPE"].Value?.ToString() : null;
     decimal role = command.Parameters["ROLEDID"].Value as decimal? ?? 0;

     string descripcionRespuesta = command.Parameters["RESPDESCRI"].Value != DBNull.Value ? command.Parameters["RESPDESCRI"].Value?.ToString() : null;
     decimal codigoRespuesta = command.Parameters["RESPCODE"].Value as decimal? ?? 0;
 }
