using RestUtilities.Helpers;

public string GetAS400ConnectionString(string connectionName)
{
    // Obtener sección actual del ambiente (DEV, UAT, PROD)
    var section = CurrentEnvironment
        .GetSection("ConnectionSettings")
        .GetSection(connectionName);

    string driver = section["DriverConnection"];
    string server = section["ServerName"];
    string userEncoded = section["User"];
    string passEncoded = section["Password"];
    string encryptionType = section["EncryptionType"] ?? "Base64";
    string keyDecrypt = section["KeyDecrypt"] ?? "";

    // Desencriptar dinámicamente
    string user = EncryptionHelper.Decrypt(userEncoded, encryptionType, keyDecrypt);
    string password = EncryptionHelper.Decrypt(passEncoded, encryptionType, keyDecrypt);

    return $"Provider={driver};Data Source={server};User ID={user};Password={password};";
}
