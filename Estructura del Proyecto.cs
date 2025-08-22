Esta es la clase que uso para la encriptación y desencriptacion

using System.Security.Cryptography;
using System.Text;

namespace CAUAdministracion.Helpers;

/// <summary> 
/// Clase utilitaria para encriptar y desencriptar cadenas compatibles con el sistema anterior. 
/// /// </summary> 
public static class OperacionesVarias
{
    /// <summary>
    /// Clase utilitaria para encriptar y desencriptar cadenas. 
    /// Soporta formato antiguo (Base64 Unicode) y moderno (AES). 
    /// </summary> 
    /// 
    private static readonly string aesKey = "TuClaveAES128Bits"; // 16 caracteres
    private static readonly string aesIV = "VectorInicialAES";   // 16 caracteres

    public static string EncriptarCadena(string cadenaEncriptar)
    {
        byte[] encrypted = Encoding.Unicode.GetBytes(cadenaEncriptar);
        return Convert.ToBase64String(encrypted);
    }

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

    public static string EncriptarCadenaAES(string textoPlano)
    {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(aesKey);
        aes.IV = Encoding.UTF8.GetBytes(aesIV);

        ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        byte[] inputBytes = Encoding.UTF8.GetBytes(textoPlano);

        using var ms = new System.IO.MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(inputBytes, 0, inputBytes.Length);
            cs.FlushFinalBlock();
            return Convert.ToBase64String(ms.ToArray());
        }
    }

    public static string DesencriptarCadenaAES(string textoEncriptado)
    {
        try
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(aesKey);
            aes.IV = Encoding.UTF8.GetBytes(aesIV);

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            byte[] inputBytes = Convert.FromBase64String(textoEncriptado);

            using var ms = new System.IO.MemoryStream(inputBytes);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new System.IO.StreamReader(cs);
            return reader.ReadToEnd();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Detecta automáticamente si la contraseña está en formato viejo o nuevo y la desencripta.
    /// </summary>
    public static string DesencriptarAuto(string cadena)
    {
        string desencriptado = DesencriptarCadenaAES(cadena);
        if (!string.IsNullOrWhiteSpace(desencriptado))
            return desencriptado;

        desencriptado = DesencriptarCadena(cadena);
        return desencriptado;
    }

    /// <summary>
    /// Detecta si una cadena parece ser AES (nuevo formato) por su tamaño y contenido.
    /// </summary>
    public static bool EsFormatoNuevo(string cadena)
    {
        try
        {
            string intento = DesencriptarCadenaAES(cadena);
            return !string.IsNullOrWhiteSpace(intento);
        }
        catch
        {
            return false;
        }
    }
}

