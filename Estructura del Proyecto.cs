// Helpers/OperacionesVarias.cs using System; using System.Security.Cryptography; using System.Text;

namespace SitiosIntranet.Web.Helpers { /// <summary> /// Clase utilitaria para encriptar y desencriptar cadenas. /// Soporta formato antiguo (Base64 Unicode) y moderno (AES). /// </summary> public static class OperacionesVarias { private static readonly string aesKey = "TuClaveAES128Bits"; // Debe tener 16 caracteres private static readonly string aesIV = "VectorInicialAES";   // Debe tener 16 caracteres

/// <summary>
    /// Encripta una cadena con el formato viejo (Base64 Unicode)
    /// </summary>
    public static string EncriptarCadena(string cadenaEncriptar)
    {
        byte[] encrypted = Encoding.Unicode.GetBytes(cadenaEncriptar);
        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    /// Desencripta una cadena con el formato viejo (Base64 Unicode)
    /// </summary>
    public static string DesencriptarCadena(string cadenaDesencriptar)
    {
        try
        {
            byte[] decrypted = Convert.FromBase64String(cadenaDesencriptar);
            return Encoding.Unicode.GetString(decrypted);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Encripta usando AES (nuevo formato seguro, listo para futura migración)
    /// </summary>
    public static string EncriptarCadenaAES(string textoPlano)
    {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(aesKey);
        aes.IV = Encoding.UTF8.GetBytes(aesIV);

        ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        byte[] inputBytes = Encoding.UTF8.GetBytes(textoPlano);

        byte[] encrypted;
        using (var ms = new System.IO.MemoryStream())
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(inputBytes, 0, inputBytes.Length);
            cs.FlushFinalBlock();
            encrypted = ms.ToArray();
        }

        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    /// Desencripta usando AES (para contraseñas guardadas con el nuevo formato seguro)
    /// </summary>
    public static string DesencriptarCadenaAES(string textoEncriptado)
    {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(aesKey);
        aes.IV = Encoding.UTF8.GetBytes(aesIV);

        ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        byte[] inputBytes = Convert.FromBase64String(textoEncriptado);

        using (var ms = new System.IO.MemoryStream(inputBytes))
        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
        using (var reader = new System.IO.StreamReader(cs))
        {
            return reader.ReadToEnd();
        }
    }
}

}

