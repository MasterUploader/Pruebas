using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Utilities
{
    /// <summary>
    /// Proporciona métodos para serializar y deserializar objetos XML compatibles con SOAP.
    /// </summary>
    public static class XmlHelper
    {
        /// <summary>
        /// Serializa un objeto a una cadena XML codificada en UTF-8.
        /// </summary>
        /// <typeparam name="T">Tipo del objeto a serializar.</typeparam>
        /// <param name="obj">Instancia del objeto.</param>
        /// <returns>Cadena en formato XML.</returns>
        public static string SerializeToXml<T>(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "El objeto no puede ser null.");

            var xmlSerializer = new XmlSerializer(typeof(T));
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                OmitXmlDeclaration = false
            };

            using var stringWriter = new Utf8StringWriter();
            using var writer = XmlWriter.Create(stringWriter, settings);
            xmlSerializer.Serialize(writer, obj);
            return stringWriter.ToString();
        }

        /// <summary>
        /// Deserializa una cadena XML a una instancia del tipo especificado.
        /// </summary>
        /// <typeparam name="T">Tipo de destino.</typeparam>
        /// <param name="xml">XML como cadena.</param>
        /// <returns>Instancia deserializada del tipo especificado.</returns>
        public static T DeserializeFromXml<T>(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                throw new ArgumentNullException(nameof(xml), "El XML no puede ser null o vacío.");

            var xmlSerializer = new XmlSerializer(typeof(T));
            using var stringReader = new StringReader(xml);
            return (T)xmlSerializer.Deserialize(stringReader)!;
        }

        /// <summary>
        /// Implementación de StringWriter que asegura la codificación en UTF-8.
        /// </summary>
        private class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }
    }
}
