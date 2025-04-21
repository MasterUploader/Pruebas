/// <summary>
/// Genera la cadena de conexión compatible con IBM.EntityFrameworkCore para AS400.
/// </summary>
/// <param name="connectionName">Nombre de la conexión (ej. AS400_DBContext)</param>
public string GetEfCoreConnectionString(string connectionName)
{
    var section = CurrentEnvironment.GetSection("ConnectionSettings").GetSection(connectionName);

    string server = section["Server"];
    string database = section["Database"];
    string user = EncryptionHelper.Decrypt(section["User"], section["EncryptionType"], section["KeyDecrypt"]);
    string password = EncryptionHelper.Decrypt(section["Password"], section["EncryptionType"], section["KeyDecrypt"]);

    return $"Server={server};Database={database};UID={user};PWD={password};";
}

/// <summary>
/// Genera una cadena de conexión tradicional compatible con iDB2Connection o IBM.Data.Db2.
/// </summary>
/// <param name="connectionName">Nombre de la conexión (ej. AS400)</param>
public string GetRawConnectionString(string connectionName)
{
    var section = CurrentEnvironment.GetSection("ConnectionSettings").GetSection(connectionName);

    string driver = section["DriverConnection"];
    string server = section["ServerName"];
    string user = EncryptionHelper.Decrypt(section["User"], section["EncryptionType"], section["KeyDecrypt"]);
    string password = EncryptionHelper.Decrypt(section["Password"], section["EncryptionType"], section["KeyDecrypt"]);

    return $"Provider={driver};Data Source={server};User ID={user};Password={password};";
}
