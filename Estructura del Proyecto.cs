using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Utilities
{
    /// <summary>
    /// Clase estática que proporciona métodos para serializar y deserializar objetos a/from XML,
    /// con soporte específico para estructuras SOAP y ajustes personalizados.
    /// </summary>
    public static class XmlHelper
    {
        /// <summary>
        /// Serializa un objeto a una cadena XML codificada en UTF-8.
        /// Postprocesa el XML para ajustar el nodo <gp:REQUEST> a <REQUEST xmlns="http://www.btsincusa.com/gp">.
        /// </summary>
        /// <typeparam name="T">Tipo del objeto a serializar.</typeparam>
        /// <param name="obj">Instancia del objeto a serializar.</param>
        /// <param name="namespaces">Namespaces XML opcionales (no usados en este caso para evitar conflictos).</param>
        /// <returns>Cadena en formato XML con el ajuste aplicado.</returns>
        public static string SerializeToXml<T>(T obj, XmlSerializerNamespaces namespaces = null)
        {
            // Validación: asegura que el objeto no sea null
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "El objeto no puede ser null.");

            // Crea un serializador XML para el tipo T
            var xmlSerializer = new XmlSerializer(typeof(T));

            // Configura las opciones de escritura XML
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,        // Codificación UTF-8
                Indent = true,                  // Indentación para legibilidad
                OmitXmlDeclaration = false      // Incluye la declaración XML
            };

            // Usa un StringWriter personalizado para capturar el XML
            using var stringWriter = new Utf8StringWriter();
            using var writer = XmlWriter.Create(stringWriter, settings);

            // Serializa el objeto sin pasar namespaces explícitos, dejando que la clase defina los namespaces
            xmlSerializer.Serialize(writer, obj);

            // Obtiene el XML generado
            var xml = stringWriter.ToString();

            // Aplica el postprocesamiento para ajustar <gp:REQUEST>
            return AdjustRequestNamespace(xml);
        }

        /// <summary>
        /// Postprocesa el XML generado para reemplazar <gp:REQUEST> por <REQUEST xmlns="http://www.btsincusa.com/gp">.
        /// </summary>
        /// <param name="xml">Cadena XML generada por el serializador.</param>
        /// <returns>Cadena XML ajustada.</returns>
        private static string AdjustRequestNamespace(string xml)
        {
            // Carga el XML en un XmlDocument
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            // Crea un administrador de namespaces para trabajar con gp:
            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("gp", "http://www.btsincusa.com/gp");

            // Busca el nodo <gp:REQUEST>
            var requestNode = doc.SelectSingleNode("//gp:REQUEST", nsmgr);
            if (requestNode != null)
            {
                // Crea un nuevo nodo <REQUEST> con namespace por defecto
                var newRequestNode = doc.CreateElement("REQUEST", "http://www.btsincusa.com/gp");

                // Copia los nodos hijos
                foreach (XmlNode child in requestNode.ChildNodes)
                {
                    newRequestNode.AppendChild(child.Clone());
                }

                // Reemplaza el nodo original
                requestNode.ParentNode?.ReplaceChild(newRequestNode, requestNode);
            }

            // Guarda el XML ajustado
            using var writer = new StringWriter();
            doc.Save(writer);
            return writer.ToString();
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
