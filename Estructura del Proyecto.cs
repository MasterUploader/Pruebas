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
        /// Serializa un objeto a una cadena XML codificada en UTF-8, con la opción de especificar namespaces.
        /// Postprocesa el XML para ajustar el nodo <gp:REQUEST> a <REQUEST xmlns="http://www.btsincusa.com/gp">.
        /// </summary>
        /// <typeparam name="T">Tipo del objeto a serializar.</typeparam>
        /// <param name="obj">Instancia del objeto a serializar.</param>
        /// <param name="namespaces">Namespaces XML opcionales para controlar los prefijos en el XML generado.</param>
        /// <returns>Cadena en formato XML con el ajuste aplicado.</returns>
        public static string SerializeToXml<T>(T obj, XmlSerializerNamespaces namespaces = null)
        {
            // Validación: asegura que el objeto no sea null para evitar errores en la serialización
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "El objeto no puede ser null.");

            // Crea un serializador XML para el tipo T
            var xmlSerializer = new XmlSerializer(typeof(T));

            // Configura las opciones de escritura XML: UTF-8, con indentación y declaración XML incluida
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,        // Codificación UTF-8 para compatibilidad amplia
                Indent = true,                  // Indentación para legibilidad del XML
                OmitXmlDeclaration = false      // Incluye <?xml version="1.0" encoding="utf-8"?>
            };

            // Usa un StringWriter personalizado para capturar el XML como cadena
            using var stringWriter = new Utf8StringWriter();
            // Crea un XmlWriter con las configuraciones definidas
            using var writer = XmlWriter.Create(stringWriter, settings);

            // Si no se proporcionan namespaces, usa los predeterminados (soapenv y gp)
            var ns = namespaces ?? GetDefaultNamespaces();
            // Serializa el objeto al writer, aplicando los namespaces especificados
            xmlSerializer.Serialize(writer, obj, ns);

            // Obtiene el XML generado como cadena
            var xml = stringWriter.ToString();

            // Aplica el postprocesamiento para ajustar <gp:REQUEST> a <REQUEST xmlns="...">
            return AdjustRequestNamespace(xml);
        }

        /// <summary>
        /// Devuelve un conjunto predeterminado de namespaces para SOAP y el namespace personalizado gp.
        /// Esto asegura que el XML siempre tenga soapenv y gp definidos si no se pasan namespaces externos.
        /// </summary>
        /// <returns>Objeto XmlSerializerNamespaces con soapenv y gp configurados.</returns>
        private static XmlSerializerNamespaces GetDefaultNamespaces()
        {
            var ns = new XmlSerializerNamespaces();
            ns.Add("soapenv", "http://schemas.xmlsoap.org/soap/envelope/"); // Namespace estándar SOAP 1.1
            ns.Add("gp", "http://www.btsincusa.com/gp");                   // Namespace personalizado para gp
            return ns;
        }

        /// <summary>
        /// Postprocesa el XML generado para reemplazar <gp:REQUEST> por <REQUEST xmlns="http://www.btsincusa.com/gp">.
        /// Este método asegura que el elemento REQUEST tenga un namespace por defecto local.
        /// </summary>
        /// <param name="xml">Cadena XML generada por el serializador.</param>
        /// <returns>Cadena XML ajustada con el cambio aplicado.</returns>
        private static string AdjustRequestNamespace(string xml)
        {
            // Carga el XML en un XmlDocument para manipulación
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            // Crea un administrador de namespaces para trabajar con prefijos como gp:
            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("gp", "http://www.btsincusa.com/gp"); // Asocia gp al namespace personalizado

            // Busca el nodo <gp:REQUEST> en el documento usando el prefijo gp
            var requestNode = doc.SelectSingleNode("//gp:REQUEST", nsmgr);
            if (requestNode != null)
            {
                // Crea un nuevo nodo <REQUEST> sin prefijo, pero con el namespace por defecto
                var newRequestNode = doc.CreateElement("REQUEST", "http://www.btsincusa.com/gp");

                // Copia todos los nodos hijos de <gp:REQUEST> al nuevo nodo <REQUEST>
                foreach (XmlNode child in requestNode.ChildNodes)
                {
                    newRequestNode.AppendChild(child.Clone()); // Clona los hijos para evitar referencias
                }

                // Reemplaza el nodo original <gp:REQUEST> por el nuevo <REQUEST xmlns="...">
                requestNode.ParentNode?.ReplaceChild(newRequestNode, requestNode);
            }

            // Guarda el XML ajustado en una nueva cadena
            using var writer = new StringWriter();
            doc.Save(writer);
            return writer.ToString();
        }

        /// <summary>
        /// Deserializa una cadena XML a una instancia del tipo especificado.
        /// </summary>
        /// <typeparam name="T">Tipo de destino para la deserialización.</typeparam>
        /// <param name="xml">Cadena XML a deserializar.</param>
        /// <returns>Instancia del tipo T creada a partir del XML.</returns>
        public static T DeserializeFromXml<T>(string xml)
        {
            // Validación: asegura que el XML no sea null o vacío
            if (string.IsNullOrWhiteSpace(xml))
                throw new ArgumentNullException(nameof(xml), "El XML no puede ser null o vacío.");

            // Crea un serializador XML para el tipo T
            var xmlSerializer = new XmlSerializer(typeof(T));
            // Usa un StringReader para leer la cadena XML
            using var stringReader = new StringReader(xml);
            // Deserializa y devuelve el objeto (con cast seguro a T)
            return (T)xmlSerializer.Deserialize(stringReader)!;
        }

        /// <summary>
        /// Clase auxiliar que extiende StringWriter para forzar la codificación UTF-8.
        /// Esto asegura que el XML generado use UTF-8 de manera consistente.
        /// </summary>
        private class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8; // Sobrescribe la codificación predeterminada
        }
    }
}
