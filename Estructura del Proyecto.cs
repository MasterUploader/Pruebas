namespace RestUtilities.QueryBuilder.Enums
{
    /// <summary>
    /// Representa los operadores de comparación que pueden usarse en condiciones SQL.
    /// </summary>
    public enum SqlOperator
    {
        /// <summary>Igual (=)</summary>
        Equal,

        /// <summary>Diferente (<> o !=)</summary>
        NotEqual,

        /// <summary>Mayor que (>)</summary>
        GreaterThan,

        /// <summary>Menor que (<)</summary>
        LessThan,

        /// <summary>Mayor o igual que (>=)</summary>
        GreaterThanOrEqual,

        /// <summary>Menor o igual que (<=)</summary>
        LessThanOrEqual,

        /// <summary>Contiene (LIKE %valor%)</summary>
        Contains,

        /// <summary>Empieza con (LIKE valor%)</summary>
        StartsWith,

        /// <summary>Termina con (LIKE %valor)</summary>
        EndsWith,

        /// <summary>IN (lista de valores)</summary>
        In,

        /// <summary>NOT IN (lista de valores)</summary>
        NotIn,

        /// <summary>IS NULL</summary>
        IsNull,

        /// <summary>IS NOT NULL</summary>
        IsNotNull,

        /// <summary>BETWEEN (rango)</summary>
        Between
    }
}


namespace RestUtilities.QueryBuilder.Enums
{
    /// <summary>
    /// Define los tipos de JOIN utilizados en SQL.
    /// </summary>
    public enum SqlJoinType
    {
        /// <summary>JOIN INTERNO (INNER JOIN)</summary>
        Inner,

        /// <summary>JOIN IZQUIERDO (LEFT JOIN)</summary>
        Left,

        /// <summary>JOIN DERECHO (RIGHT JOIN)</summary>
        Right,

        /// <summary>JOIN COMPLETO (FULL OUTER JOIN)</summary>
        Full,

        /// <summary>SELF JOIN</summary>
        Self
    }
}

namespace RestUtilities.QueryBuilder.Enums
{
    /// <summary>
    /// Define la dirección de ordenamiento utilizada en las cláusulas ORDER BY.
    /// </summary>
    public enum SqlSortDirection
    {
        /// <summary>Orden ascendente (ASC)</summary>
        Ascending,

        /// <summary>Orden descendente (DESC)</summary>
        Descending
    }
}

namespace RestUtilities.QueryBuilder.Enums
{
    /// <summary>
    /// Define los motores de base de datos soportados por el generador de consultas.
    /// </summary>
    public enum SqlEngineType
    {
        /// <summary>AS400 (IBM iSeries)</summary>
        AS400,

        /// <summary>Microsoft SQL Server</summary>
        SqlServer,

        /// <summary>Oracle</summary>
        Oracle,

        /// <summary>PostgreSQL</summary>
        PostgreSql,

        /// <summary>MySQL</summary>
        MySql
    }
}
