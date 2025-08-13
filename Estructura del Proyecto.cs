-- 1) Básico
SELECT * 
FROM SALES.Orders;

-- 2) DISTINCT + alias
SELECT DISTINCT 
  ORDER_ID,
  ORDER_DATE AS FEC_ORD,
  CUSTOMER_ID,
  TOTAL_AMOUNT
FROM SALES.Orders;

-- 3) WHERE simple (equivale a raw o tipado)
SELECT 
  ORDER_ID, CUSTOMER_ID, STATUS, TOTAL_AMOUNT
FROM SALES.Orders
WHERE STATUS = 'OPEN'
  AND TOTAL_AMOUNT > 1000;

-- 4) IN / NOT IN / BETWEEN
SELECT 
  ORDER_ID, CUSTOMER_ID, ORDER_DATE, TOTAL_AMOUNT, STATUS
FROM SALES.Orders
WHERE STATUS IN ('OPEN','HOLD','PENDING')
  AND ORDER_DATE BETWEEN '2024-01-01' AND '2024-12-31'
  AND CUSTOMER_ID NOT IN (9999, 8888);

-- 5) Funciones en WHERE (LIKE, LOWER/COALESCE)
SELECT 
  CUSTOMER_ID, NAME, EMAIL, CITY
FROM SALES.Customers
WHERE LOWER(EMAIL) LIKE LOWER('%@example.com')
  AND COALESCE(CITY, '') <> '';

-- 6) ORDER BY (None/Desc/Asc)
SELECT 
  PRODUCT_ID, SKU, NAME, CATEGORY, PRICE, STOCK_QTY
FROM INVENTORY.Products
ORDER BY CATEGORY, PRICE DESC, NAME ASC;

-- 7) ORDER BY con CASE WHEN (VIP primero), luego por nombre
SELECT 
  c.CUSTOMER_ID, c.NAME, c.IS_VIP, c.CREATED_AT
FROM SALES.Customers c
ORDER BY 
  CASE WHEN c.IS_VIP = 1 THEN 0 ELSE 1 END,
  c.NAME ASC;

-- 8) JOIN “clásico” con alias
SELECT 
  o.ORDER_ID, o.ORDER_DATE, o.STATUS,
  c.CUSTOMER_ID, c.NAME, c.EMAIL
FROM SALES.Orders o
INNER JOIN SALES.Customers c
  ON o.CUSTOMER_ID = c.CUSTOMER_ID
WHERE o.STATUS = 'OPEN';

-- 9) JOIN “convenience” + LEFT JOIN
SELECT
  o.ORDER_ID, o.ORDER_DATE, o.TOTAL_AMOUNT,
  c.NAME, c.EMAIL,
  p.PAYMENT_DATE, p.AMOUNT AS PAID
FROM SALES.Orders o
INNER JOIN SALES.Customers c
  ON o.CUSTOMER_ID = c.CUSTOMER_ID
LEFT JOIN FINANCE.Payments p
  ON p.ORDER_ID = o.ORDER_ID;

-- 10) Varios JOIN + filtros + ORDER BY
SELECT
  o.ORDER_ID, o.ORDER_DATE, o.STATUS, o.TOTAL_AMOUNT,
  c.NAME AS CUSTOMER_NAME, c.EMAIL,
  p.SKU, p.NAME AS PRODUCT_NAME, l.QTY, l.PRICE
FROM SALES.Orders o
INNER JOIN SALES.Customers c
  ON o.CUSTOMER_ID = c.CUSTOMER_ID
INNER JOIN INVENTORY.OrderLines l
  ON l.ORDER_ID = o.ORDER_ID
INNER JOIN INVENTORY.Products p
  ON p.PRODUCT_ID = l.PRODUCT_ID
WHERE o.STATUS IN ('OPEN','HOLD')
  AND p.CATEGORY = 'ELECTRONICS'
ORDER BY o.ORDER_DATE DESC;

-- 11) GROUP BY + HAVING (totales por categoría)
SELECT
  p.CATEGORY AS CATEGORY,
  SUM(l.QTY * l.PRICE) AS TOTAL_SALES,
  COUNT(*) AS LINES
FROM INVENTORY.OrderLines l
INNER JOIN INVENTORY.Products p
  ON p.PRODUCT_ID = l.PRODUCT_ID
GROUP BY p.CATEGORY
HAVING SUM(l.QTY * l.PRICE) > 10000
ORDER BY TOTAL_SALES DESC;

-- 12) SELECT con CASE WHEN (tamaño del pedido)
SELECT
  o.ORDER_ID, o.CUSTOMER_ID, o.TOTAL_AMOUNT,
  CASE
    WHEN o.TOTAL_AMOUNT >= 5000 THEN 'LARGE'
    WHEN o.TOTAL_AMOUNT >= 1000 THEN 'MEDIUM'
    ELSE 'SMALL'
  END AS ORDER_SIZE
FROM SALES.Orders o;

-- 13) EXISTS con subconsulta (ordenes OPEN en 2024)
SELECT 
  c.CUSTOMER_ID, c.NAME, c.EMAIL
FROM SALES.Customers c
WHERE EXISTS (
  SELECT 1
  FROM SALES.Orders o
  WHERE o.CUSTOMER_ID = c.CUSTOMER_ID
    AND o.STATUS = 'OPEN'
    AND o.ORDER_DATE BETWEEN '2024-01-01' AND '2024-12-31'
  FETCH FIRST 1 ROWS ONLY
);

-- 14) CTE (WITH) y consulta principal
WITH VentasMensuales AS (
  SELECT
    o.CUSTOMER_ID AS CUSTOMER_ID,
    MONTH(o.ORDER_DATE) AS MM,
    SUM(o.TOTAL_AMOUNT) AS TOTAL
  FROM SALES.Orders o
  WHERE o.ORDER_DATE BETWEEN '2024-01-01' AND '2024-12-31'
  GROUP BY o.CUSTOMER_ID, MONTH(o.ORDER_DATE)
)
SELECT CUSTOMER_ID, MM, TOTAL
FROM VentasMensuales
WHERE TOTAL > 50000
ORDER BY TOTAL DESC;

-- 15) Paginación (OFFSET/FETCH)
SELECT 
  ID, EVENT_DATE, USER_ID, ACTION, DETAIL
FROM LOGS.AuditLog
WHERE EVENT_DATE BETWEEN '2024-10-01' AND '2024-10-31'
ORDER BY EVENT_DATE DESC
OFFSET 75 ROWS FETCH NEXT 25 ROWS ONLY;

-- 15b) Top N (FETCH FIRST)
SELECT *
FROM LOGS.AuditLog
FETCH FIRST 100 ROWS ONLY;

-- 16) JOIN con subconsulta derivada
SELECT 
  c.CUSTOMER_ID, c.NAME, pr.LAST_PAY
FROM SALES.Customers c
LEFT JOIN (
  SELECT p.CUSTOMER_ID, MAX(p.PAYMENT_DATE) AS LAST_PAY
  FROM FINANCE.Payments p
  GROUP BY p.CUSTOMER_ID
) pr
  ON pr.CUSTOMER_ID = c.CUSTOMER_ID
ORDER BY pr.LAST_PAY DESC;

-- 17) Mega (CTE + varios JOIN + CASE + EXISTS + HAVING + ORDER BY CASE + paginado)
WITH Spend2024 AS (
  SELECT
    o.CUSTOMER_ID AS CUSTOMER_ID,
    SUM(o.TOTAL_AMOUNT) AS TOTAL_2024
  FROM SALES.Orders o
  WHERE o.ORDER_DATE BETWEEN '2024-01-01' AND '2024-12-31'
  GROUP BY o.CUSTOMER_ID
)
SELECT
  c.CUSTOMER_ID,
  c.NAME,
  c.EMAIL,
  c.CITY,
  c.IS_VIP,
  COALESCE(s.TOTAL_2024, 0) AS TOTAL_2024,
  COUNT(p.PAYMENT_ID) AS PAY_COUNT
FROM SALES.Customers c
LEFT JOIN Spend2024 s
  ON s.CUSTOMER_ID = c.CUSTOMER_ID
LEFT JOIN FINANCE.Payments p
  ON p.CUSTOMER_ID = c.CUSTOMER_ID
WHERE COALESCE(c.STATUS, 'ACTIVE') = 'ACTIVE'
  AND EXISTS (
    SELECT 1
    FROM SALES.Orders o
    WHERE o.CUSTOMER_ID = c.CUSTOMER_ID
      AND o.STATUS = 'OPEN'
    FETCH FIRST 1 ROWS ONLY
  )
GROUP BY 
  c.CUSTOMER_ID, c.NAME, c.EMAIL, c.CITY, c.IS_VIP, s.TOTAL_2024
HAVING COALESCE(s.TOTAL_2024, 0) >= 25000
ORDER BY
  CASE WHEN c.IS_VIP = 1 THEN 0 ELSE 1 END,
  s.TOTAL_2024 DESC,
  c.NAME ASC
OFFSET 0 ROWS FETCH NEXT 50 ROWS ONLY;


using System;
using System.Collections.Generic;
using QueryBuilder.Builders;
using QueryBuilder.Enums;
using QueryBuilder.Models;

public static class DemoSelectSamples
{
    /// <summary>
    /// Genera todos los ejemplos de SELECT (de básico a muy complejo) y retorna
    /// un diccionario {clave -> SQL}. También puedes recorrer y escribir en consola.
    /// </summary>
    public static IReadOnlyDictionary<string, string> BuildAll()
    {
        var result = new Dictionary<string, string>();

        // 1) Básico
        {
            var q = new SelectQueryBuilder("Orders", "SALES")
                .Select("*")
                .Build();
            result["1_Basico"] = q.Sql;
        }

        // 2) DISTINCT + alias
        {
            var q = new SelectQueryBuilder("Orders", "SALES")
                .Distinct()
                .Select(
                    "ORDER_ID",
                    ("ORDER_DATE", "FEC_ORD"),
                    "CUSTOMER_ID",
                    "TOTAL_AMOUNT"
                )
                .Build();
            result["2_Distinct_Alias"] = q.Sql;
        }

        // 3) WHERE simple
        {
            var q = new SelectQueryBuilder("Orders", "SALES")
                .Select("ORDER_ID", "CUSTOMER_ID", "STATUS", "TOTAL_AMOUNT")
                .WhereRaw("STATUS = 'OPEN'")
                .WhereRaw("TOTAL_AMOUNT > 1000")
                .Build();
            result["3_Where_Simple"] = q.Sql;
        }

        // 4) IN / NOT IN / BETWEEN
        {
            var q = new SelectQueryBuilder("Orders", "SALES")
                .Select("ORDER_ID", "CUSTOMER_ID", "ORDER_DATE", "TOTAL_AMOUNT", "STATUS")
                .WhereIn("STATUS", new object[] { "OPEN", "HOLD", "PENDING" })
                .WhereBetween("ORDER_DATE", "2024-01-01", "2024-12-31")
                .WhereNotIn("CUSTOMER_ID", new object[] { 9999, 8888 })
                .Build();
            result["4_In_NotIn_Between"] = q.Sql;
        }

        // 5) Funciones en WHERE
        {
            var q = new SelectQueryBuilder("Customers", "SALES")
                .Select("CUSTOMER_ID", "NAME", "EMAIL", "CITY")
                .WhereRaw("LOWER(EMAIL) LIKE LOWER('%@example.com')")
                .WhereRaw("COALESCE(CITY, '') <> ''")
                .Build();
            result["5_Funciones_Where"] = q.Sql;
        }

        // 6) ORDER BY (None/Desc/Asc)
        {
            var q = new SelectQueryBuilder("Products", "INVENTORY")
                .Select("PRODUCT_ID", "SKU", "NAME", "CATEGORY", "PRICE", "STOCK_QTY")
                .OrderBy(("CATEGORY", SortDirection.None), ("PRICE", SortDirection.Desc), ("NAME", SortDirection.Asc))
                .Build();
            result["6_OrderBy_Combinado"] = q.Sql;
        }

        // 7) ORDER BY con CASE WHEN
        {
            var caseVipPrimero = new CaseWhenBuilder()
                .When("c.IS_VIP = 1").Then("0")
                .Else("1");

            var q = new SelectQueryBuilder("Customers", "SALES")
                .As("c")
                .Select("c.CUSTOMER_ID", "c.NAME", "c.IS_VIP", "c.CREATED_AT")
                .OrderByCase(caseVipPrimero, SortDirection.None) // sin "ASC" explícito
                .OrderBy(("c.NAME", SortDirection.Asc))
                .Build();
            result["7_OrderBy_Case"] = q.Sql;
        }

        // 8) JOIN “clásico” con alias
        {
            var q = new SelectQueryBuilder("Orders", "SALES")
                .As("o")
                .Join("Customers", "SALES", "c", "o.CUSTOMER_ID", "c.CUSTOMER_ID", "INNER")
                .Select("o.ORDER_ID", "o.ORDER_DATE", "o.STATUS", "c.CUSTOMER_ID", "c.NAME", "c.EMAIL")
                .WhereRaw("o.STATUS = 'OPEN'")
                .Build();
            result["8_Join_Clasico"] = q.Sql;
        }

        // 9) JOIN “convenience” + LEFT JOIN (usa Join(table, onCondition, JoinType))
        {
            var q = new SelectQueryBuilder("Orders", "SALES")
                .As("o")
                .Join("SALES.Customers c", "o.CUSTOMER_ID = c.CUSTOMER_ID", JoinType.Inner)
                .Join("FINANCE.Payments p", "p.ORDER_ID = o.ORDER_ID", JoinType.Left)
                .Select("o.ORDER_ID", "o.ORDER_DATE", "o.TOTAL_AMOUNT", "c.NAME", "c.EMAIL", "p.PAYMENT_DATE", "p.AMOUNT AS PAID")
                .Build();
            result["9_Join_Convenience"] = q.Sql;
        }

        // 10) Varios JOIN + filtros + ORDER BY
        {
            var q = new SelectQueryBuilder("Orders", "SALES")
                .As("o")
                .Join("Customers", "SALES", "c", "o.CUSTOMER_ID", "c.CUSTOMER_ID", "INNER")
                .Join("OrderLines", "INVENTORY", "l", "l.ORDER_ID", "o.ORDER_ID", "INNER")
                .Join("Products", "INVENTORY", "p", "p.PRODUCT_ID", "l.PRODUCT_ID", "INNER")
                .Select("o.ORDER_ID", "o.ORDER_DATE", "o.STATUS", "o.TOTAL_AMOUNT",
                        "c.NAME AS CUSTOMER_NAME", "c.EMAIL",
                        "p.SKU", "p.NAME AS PRODUCT_NAME", "l.QTY", "l.PRICE")
                .WhereIn("o.STATUS", new object[] { "OPEN", "HOLD" })
                .WhereRaw("p.CATEGORY = 'ELECTRONICS'")
                .OrderBy(("o.ORDER_DATE", SortDirection.Desc))
                .Build();
            result["10_Joins_Multiples"] = q.Sql;
        }

        // 11) GROUP BY + HAVING
        {
            var q = new SelectQueryBuilder("OrderLines", "INVENTORY")
                .As("l")
                .Join("Products", "INVENTORY", "p", "p.PRODUCT_ID", "l.PRODUCT_ID", "INNER")
                .Select("p.CATEGORY AS CATEGORY", "SUM(l.QTY * l.PRICE) AS TOTAL_SALES", "COUNT(*) AS LINES")
                .GroupBy("p.CATEGORY")
                .HavingFunction("SUM(l.QTY * l.PRICE) > 10000")
                .OrderBy(("TOTAL_SALES", SortDirection.Desc))
                .Build();
            result["11_GroupBy_Having"] = q.Sql;
        }

        // 12) SELECT con CASE WHEN (columna calculada)
        {
            var caseTam = new CaseWhenBuilder()
                .When("o.TOTAL_AMOUNT >= 5000").Then("'LARGE'")
                .When("o.TOTAL_AMOUNT >= 1000").Then("'MEDIUM'")
                .Else("'SMALL'");

            var q = new SelectQueryBuilder("Orders", "SALES")
                .As("o")
                .Select("o.ORDER_ID", "o.CUSTOMER_ID", "o.TOTAL_AMOUNT")
                .SelectCase(caseTam.Build(), "ORDER_SIZE")
                .Build();
            result["12_Select_Case_As_Col"] = q.Sql;
        }

        // 13) EXISTS con subconsulta
        {
            var q = new SelectQueryBuilder("Customers", "SALES")
                .As("c")
                .Select("c.CUSTOMER_ID", "c.NAME", "c.EMAIL")
                .WhereExists(sub =>
                {
                    sub.As("o")
                       .Select("1")
                       .From("Orders", "SALES")    // helper: vamos a simularlo usando el mismo builder
                       .WhereRaw("o.CUSTOMER_ID = c.CUSTOMER_ID")
                       .WhereRaw("o.STATUS = 'OPEN'")
                       .WhereBetween("o.ORDER_DATE", "2024-01-01", "2024-12-31")
                       .Limit(1);
                })
                .Build();
            result["13_Exists"] = q.Sql;
        }

        // 14) CTE (WITH ...) + consulta
        {
            var inner = new SelectQueryBuilder("Orders", "SALES")
                .As("o")
                .Select("o.CUSTOMER_ID AS CUSTOMER_ID", "MONTH(o.ORDER_DATE) AS MM", "SUM(o.TOTAL_AMOUNT) AS TOTAL")
                .WhereBetween("o.ORDER_DATE", "2024-01-01", "2024-12-31")
                .GroupBy("o.CUSTOMER_ID", "MONTH(o.ORDER_DATE)")
                .Build();

            var cte = new CommonTableExpression("VentasMensuales", inner.Sql);

            var q = new SelectQueryBuilder("VentasMensuales")
                .With(cte)
                .Select("CUSTOMER_ID", "MM", "TOTAL")
                .WhereRaw("TOTAL > 50000")
                .OrderBy(("TOTAL", SortDirection.Desc))
                .Build();
            result["14_CTE"] = q.Sql;
        }

        // 15) Paginación (OFFSET/FETCH)
        {
            var q = new SelectQueryBuilder("AuditLog", "LOGS")
                .Select("ID", "EVENT_DATE", "USER_ID", "ACTION", "DETAIL")
                .WhereBetween("EVENT_DATE", "2024-10-01", "2024-10-31")
                .OrderBy(("EVENT_DATE", SortDirection.Desc))
                .Offset(75)
                .FetchNext(25)
                .Build();
            result["15_Paginacion"] = q.Sql;
        }

        // 15b) Top N (FETCH FIRST)
        {
            var q = new SelectQueryBuilder("AuditLog", "LOGS")
                .Select("*")
                .Limit(100)
                .Build();
            result["15b_TopN"] = q.Sql;
        }

        // 16) JOIN con subconsulta derivada
        {
            var lastPayInner = new SelectQueryBuilder("Payments", "FINANCE")
                .As("p")
                .Select("p.CUSTOMER_ID", "MAX(p.PAYMENT_DATE) AS LAST_PAY")
                .GroupBy("p.CUSTOMER_ID")
                .Build();

            var lastPaySub = new Subquery(lastPayInner.Sql);

            var q = new SelectQueryBuilder("Customers", "SALES")
                .As("c")
                .Join(lastPaySub, "pr", "pr.CUSTOMER_ID", "c.CUSTOMER_ID", "LEFT")
                .Select("c.CUSTOMER_ID", "c.NAME", "pr.LAST_PAY")
                .OrderBy(("pr.LAST_PAY", SortDirection.Desc))
                .Build();
            result["16_Join_Subconsulta"] = q.Sql;
        }

        // 17) Mega (CTE + varios JOIN + CASE + EXISTS + HAVING + ORDER BY CASE + paginado)
        {
            var spendInner = new SelectQueryBuilder("Orders", "SALES")
                .As("o")
                .Select("o.CUSTOMER_ID AS CUSTOMER_ID", "SUM(o.TOTAL_AMOUNT) AS TOTAL_2024")
                .WhereBetween("o.ORDER_DATE", "2024-01-01", "2024-12-31")
                .GroupBy("o.CUSTOMER_ID")
                .Build();

            var spendCte = new CommonTableExpression("Spend2024", spendInner.Sql);

            var vipFirst = new CaseWhenBuilder()
                .When("c.IS_VIP = 1").Then("0")
                .Else("1");

            var q = new SelectQueryBuilder("Customers", "SALES")
                .With(spendCte)
                .As("c")
                .Join("Spend2024", null, "s", "s.CUSTOMER_ID", "c.CUSTOMER_ID", "LEFT")
                .Join("Payments", "FINANCE", "p", "p.CUSTOMER_ID", "c.CUSTOMER_ID", "LEFT")
                .Select(
                    "c.CUSTOMER_ID", "c.NAME", "c.EMAIL", "c.CITY", "c.IS_VIP",
                    "COALESCE(s.TOTAL_2024, 0) AS TOTAL_2024",
                    "COUNT(p.PAYMENT_ID) AS PAY_COUNT"
                )
                .WhereRaw("COALESCE(c.STATUS, 'ACTIVE') = 'ACTIVE'")
                .HavingExists(new Subquery(
                    new SelectQueryBuilder("Orders", "SALES")
                        .As("o")
                        .Select("1")
                        .WhereRaw("o.CUSTOMER_ID = c.CUSTOMER_ID")
                        .WhereRaw("o.STATUS = 'OPEN'")
                        .Limit(1)
                        .Build().Sql
                ))
                .GroupBy("c.CUSTOMER_ID", "c.NAME", "c.EMAIL", "c.CITY", "c.IS_VIP", "s.TOTAL_2024")
                .HavingFunction("COALESCE(s.TOTAL_2024, 0) >= 25000")
                .OrderByCase(vipFirst, SortDirection.None)
                .OrderBy(("s.TOTAL_2024", SortDirection.Desc), ("c.NAME", SortDirection.Asc))
                .Offset(0).FetchNext(50)
                .Build();

            result["17_Mega"] = q.Sql;
        }

        return result;
    }

    /// <summary>
    /// Helper para ejecutar el demo: imprime cada SQL por consola.
    /// </summary>
    public static void PrintAllToConsole()
    {
        var all = BuildAll();
        foreach (var kv in all)
        {
            Console.WriteLine($"-- {kv.Key}");
            Console.WriteLine(kv.Value);
            Console.WriteLine();
        }
    }
}

/* USO:
   // En tu Main:
   DemoSelectSamples.PrintAllToConsole();

   // O si solo necesitas el diccionario:
   var sqls = DemoSelectSamples.BuildAll();
   var sqlMega = sqls["17_Mega"];
*/
