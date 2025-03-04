
    /// <summary>
    /// Clase encargada de dar formato a los logs antes de ser escritos en archivo.
    /// </summary>
    public static class LogFormatter
    {
        public static string FormatBeginLog() =>
            $"\n---------------------------Inicio de Log---------------------------\n{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";

        public static string FormatEndLog() =>
            $"\n---------------------------Fin de Log---------------------------\n{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";

        public static string FormatRequestInfo(string method, string path, string queryParams, string body) =>
            $"\n----------------------------------Request Info---------------------------------\n" +
            $"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nMétodo: {method}\nRuta: {path}\nQuery Params: {queryParams}\nCuerpo:\n{body}\n";

        public static string FormatResponseInfo(string statusCode, string headers, string body) =>
            $"\n----------------------------------Response Info---------------------------------\n" +
            $"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nCódigo de Estado: {statusCode}\nEncabezados: {headers}\nCuerpo:\n{body}\n";

        public static string FormatEnvironmentInfo(string host, string machineName, string os, string ip) =>
            $"\n---------------------------Environment Info-------------------------\n" +
            $"Host: {host}\nServidor: {machineName}\nSistema Operativo: {os}\nIP: {ip}\n";

        public static string FormatExceptionDetails(string exceptionMessage) =>
            $"\n----------------------------------Exception Details---------------------------------\n" +
            $"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n{exceptionMessage}\n";

        public static string FormatInputParameters(string parameters) =>
            $"\n-----------------------Parámetros de Entrada-----------------------------------\n{parameters}\n";

        public static string FormatOutputParameters(string parameters) =>
            $"\n-----------------------Parámetros de Salida-----------------------------------\n{parameters}\n";

        public static string FormatSingleLog(string message) =>
            $"\n----------------------------------Log Manual---------------------------------\n" +
            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n";

        public static string FormatObjectLog(string objectName, object obj)
        {
            string formattedObj = ConvertObjectToString(obj);
            return $"\n----------------------------------Object -> {objectName}---------------------------------\n{formattedObj}\n";
        }

        /// <summary>
        /// Convierte cualquier objeto a JSON o XML si es posible.
        /// </summary>
        private static string ConvertObjectToString(object obj)
        {
            if (obj == null) return "NULL";

            try
            {
                string jsonString = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
                return jsonString;
            }
            catch
            {
                try
                {
                    return FormatXml(obj.ToString());
                }
                catch
                {
                    return obj.ToString();
                }
            }
        }

        /// <summary>
        /// Intenta formatear una cadena como XML.
        /// </summary>
        private static string FormatXml(string xml)
        {
            try
            {
                XDocument doc = XDocument.Parse(xml);
                return doc.ToString();
            }
            catch
            {
                return xml;
            }
        }
    }

