
    /// <summary>
    /// Métodos de extensión para manipulación de cadenas en el logging.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Indenta cada línea de un texto con un número de espacios determinado.
        /// </summary>
        public static string Indent(this string text, int level = 4)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            string indentation = new string(' ', level);
            return indentation + text.Replace("\n", "\n" + indentation);
        }

        /// <summary>
        /// Normaliza los espacios en blanco dentro de una cadena eliminando espacios extra.
        /// </summary>
        public static string NormalizeWhitespace(this string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        }

        /// <summary>
        /// Convierte un objeto a formato de texto legible (JSON o XML).
        /// </summary>
        public static string ConvertObjectToString(object obj)
        {
            if (obj == null) return "Sin datos";
            string objString = obj.ToString();
            if (IsJson(objString)) return FormatJson(objString);
            if (IsXml(objString)) return FormatXml(objString);
            return objString;
        }

        /// <summary>
        /// Determina si una cadena está en formato JSON.
        /// </summary>
        public static bool IsJson(string input)
        {
            input = input.Trim();
            return input.StartsWith("{") && input.EndsWith("}") || input.StartsWith("[") && input.EndsWith("]");
        }

        /// <summary>
        /// Determina si una cadena está en formato XML.
        /// </summary>
        public static bool IsXml(string input)
        {
            input = input.Trim();
            return input.StartsWith("<") && input.EndsWith(">");
        }

        /// <summary>
        /// Aplica formato JSON legible con indentación.
        /// </summary>
        public static string FormatJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return "Sin datos";
            try
            {
                var jsonObject = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return json; // Si falla, retorna la cadena original.
            }
        }

        /// <summary>
        /// Aplica formato XML legible con indentación.
        /// </summary>
        public static string FormatXml(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml)) return "Sin datos";
            try
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(xml);
                using var stringWriter = new System.IO.StringWriter();
                using var xmlTextWriter = new XmlTextWriter(stringWriter) { Formatting = Formatting.Indented };
                xmlDocument.WriteContentTo(xmlTextWriter);
                return stringWriter.ToString();
            }
            catch
            {
                return xml; // Si falla, retorna la cadena original.
            }
        }
    }



    /// <summary>
    /// Clase estática encargada de formatear los logs con la estructura definida.
    /// </summary>
    public static class LogFormatter
    {
        public static string FormatBeginLog() =>
            $"\n---------------------------Inicio de Log---------------------------\n{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";

        public static string FormatEndLog() =>
            $"\n---------------------------Fin de Log---------------------------\n{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";

        public static string FormatEnvironmentInfoStart() =>
            $"\n---------------------------Enviroment Info-------------------------\nInicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n-------------------------------------------------------------------";

        public static string FormatEnvironmentInfoEnd() =>
            $"\n---------------------------Enviroment Info-------------------------\nFin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n-------------------------------------------------------------------";

        public static string FormatRequestInfo(string method, string path, string queryParams, string body) =>
            $"\n----------------------------------Request Info---------------------------------\nInicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n-----------------------------------------------------------------------------"
            + $"\nMétodo: {method}\nRuta: {path}\nQuery Params: {queryParams}\nCuerpo:\n{body.FormatJson()}\n"
            + $"\n----------------------------------Request Info---------------------------------\nFin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n-----------------------------------------------------------------------------";

        public static string FormatResponseInfo(string statusCode, string headers, string body) =>
            $"\n----------------------------------Response Info---------------------------------\nInicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n-----------------------------------------------------------------------------"
            + $"\nCódigo de Estado: {statusCode}\nEncabezados: {headers}\nCuerpo:\n{body.FormatJson()}\n"
            + $"\n----------------------------------Response Info---------------------------------\nFin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n-----------------------------------------------------------------------------";

        public static string FormatMethodEntry(string methodName, string parameters) =>
            $"\n----------------------------------Método: {methodName}---------------------------------\nInicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n-----------------------------------------------------------------------------"
            + $"\nParámetros de Entrada:\n{parameters}\n";

        public static string FormatMethodExit(string methodName, string returnValue) =>
            $"\nParámetros de Salida:\n{returnValue}"
            + $"\n----------------------------------Método: {methodName}---------------------------------\nFin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n-----------------------------------------------------------------------------";

        public static string FormatSingleLog(string message) =>
            $"\n----------------------------------Single Log---------------------------------\nInicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n-----------------------------------------------------------------------------"
            + $"\n{message}\n"
            + $"\n----------------------------------Single Log---------------------------------\nFin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n-----------------------------------------------------------------------------";

        public static string FormatObjectLog(string objectName, object obj) =>
            $"\n----------------------------------Object -> {objectName}---------------------------------\nInicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n-----------------------------------------------------------------------------"
            + $"\n{obj.ConvertObjectToString()}\n"
            + $"\n----------------------------------Object -> {objectName}---------------------------------\nFin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n-----------------------------------------------------------------------------";

        public static string FormatExceptionDetails(string exceptionMessage) =>
            $"\n----------------------------------Exception Details---------------------------------\nInicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n-----------------------------------------------------------------------------"
            + $"\n{exceptionMessage}\n"
            + $"\n----------------------------------Exception Details---------------------------------\nFin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n-----------------------------------------------------------------------------";
    }






