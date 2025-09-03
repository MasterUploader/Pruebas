private static string ReplaceNamedParameters(string sql, DbParameterCollection parameters)
{
    string result = sql;

    foreach (DbParameter p in parameters)
    {
        var name = p.ParameterName?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name))
            continue;

        // Probar ambos prefijos comunes
        var candidates = new[]
        {
            "@" + name,
            ":" + name
        };

        foreach (var cand in candidates)
        {
            var pattern = $@"(?<!\w){Regex.Escape(cand)}(?!\w)";

            // Opción A: usar la sobrecarga con 'evaluator' y 'options'
            result = Regex.Replace(
                result,
                pattern,
                m => FormatParameterValue(p), // <— evaluador
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );

            // Opción B (equivalente): compilar el Regex y luego Replace
            // var rx = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            // result = rx.Replace(result, m => FormatParameterValue(p));
        }
    }

    return result;
}
