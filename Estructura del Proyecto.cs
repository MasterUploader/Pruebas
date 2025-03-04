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
