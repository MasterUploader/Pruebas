using QueryBuilder.Attributes;
using System;
using System.Reflection;

namespace QueryBuilder.Utils
{
    public static class SqlMetadataHelper
    {
        public static string GetFullTableName<T>()
        {
            var attr = typeof(T).GetCustomAttribute<SqlTableDefinitionAttribute>();
            if (attr == null)
                throw new InvalidOperationException($"El modelo '{typeof(T).Name}' no tiene el atributo SqlTableDefinition.");

            return $"{attr.Library}.{attr.TableName}";
        }
    }
}
