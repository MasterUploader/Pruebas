using System;

namespace RestUtilities.QueryBuilder.Utils
{
    /// <summary>
    /// Proporciona métodos auxiliares para manejar tipos de datos en tiempo de ejecución,
    /// incluyendo validaciones y conversiones seguras.
    /// </summary>
    public static class TypeHelper
    {
        /// <summary>
        /// Intenta convertir un valor al tipo especificado. Si no es posible, lanza una excepción.
        /// </summary>
        /// <param name="value">Valor a convertir.</param>
        /// <param name="targetType">Tipo de destino al que se desea convertir.</param>
        /// <returns>Valor convertido al tipo destino.</returns>
        public static object ConvertTo(object value, Type targetType)
        {
            if (value == null || targetType.IsInstanceOfType(value))
                return value;

            return System.Convert.ChangeType(value, Nullable.GetUnderlyingType(targetType) ?? targetType);
        }

        /// <summary>
        /// Indica si el tipo proporcionado representa un valor numérico.
        /// </summary>
        /// <param name="type">Tipo a evaluar.</param>
        /// <returns>True si el tipo es numérico; de lo contrario, false.</returns>
        public static bool IsNumeric(Type type)
        {
            return type == typeof(byte) || type == typeof(short) || type == typeof(int) ||
                   type == typeof(long) || type == typeof(decimal) || type == typeof(double) ||
                   type == typeof(float);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RestUtilities.QueryBuilder.Attributes;

namespace RestUtilities.QueryBuilder.Utils
{
    /// <summary>
    /// Proporciona funciones utilitarias para inspeccionar clases por reflexión,
    /// especialmente para analizar sus atributos y estructura.
    /// </summary>
    public static class ReflectionUtils
    {
        /// <summary>
        /// Obtiene todas las propiedades públicas de una clase que no tengan el atributo SqlIgnore.
        /// </summary>
        /// <param name="type">Tipo de clase que se desea inspeccionar.</param>
        /// <returns>Lista de propiedades mapeables.</returns>
        public static IEnumerable<PropertyInfo> GetMappableProperties(Type type)
        {
            return type.GetProperties()
                       .Where(p => !Attribute.IsDefined(p, typeof(SqlIgnoreAttribute)));
        }

        /// <summary>
        /// Devuelve el nombre de la columna a utilizar en SQL.
        /// Puede provenir del atributo SqlColumnName o del nombre de la propiedad.
        /// </summary>
        /// <param name="property">Propiedad a analizar.</param>
        /// <returns>Nombre de columna asociado.</returns>
        public static string GetColumnName(PropertyInfo property)
        {
            var attr = property.GetCustomAttribute<SqlColumnNameAttribute>();
            return attr != null ? attr.ColumnName : property.Name;
        }
    }
}

using System;
using System.Linq.Expressions;

namespace RestUtilities.QueryBuilder.Utils
{
    /// <summary>
    /// Convierte expresiones lambda simples en fragmentos de SQL.
    /// Ideal para filtros dinámicos en WHERE u ON.
    /// </summary>
    public static class ExpressionParser
    {
        /// <summary>
        /// Convierte una expresión lambda básica a un fragmento SQL (solo binarios).
        /// </summary>
        /// <typeparam name="T">Tipo sobre el que se evalúa la expresión.</typeparam>
        /// <param name="expression">Expresión lambda.</param>
        /// <returns>Fragmento SQL equivalente.</returns>
        public static string Parse<T>(Expression<Func<T, bool>> expression)
        {
            if (expression.Body is BinaryExpression binary)
            {
                var left = binary.Left.ToString().Split('.')[^1];
                var right = ExpressionValue(binary.Right);
                var op = GetSqlOperator(binary.NodeType);

                return $"{left} {op} {right}";
            }

            return string.Empty;
        }

        private static string GetSqlOperator(ExpressionType nodeType) => nodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            _ => throw new NotSupportedException($"Operador no soportado: {nodeType}")
        };

        private static string ExpressionValue(Expression expr)
        {
            if (expr is ConstantExpression constExpr)
                return $"'{constExpr.Value}'";

            return expr.ToString().Split('.')[^1]; // Fallback
        }
    }
}
