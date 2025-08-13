using System;
using System.Collections.Generic;
using System.Linq;
using QueryBuilder.Builders;
using QueryBuilder.Enums;
// using QueryBuilder.Models; // <- No es estrictamente necesario aquí

public static class DemoSelectSamples
{
    /// <summary>
    /// Genera ejemplos de SELECT (de básico a complejo) usando tu versión actual del SelectQueryBuilder.
    /// Retorna un diccionario {clave -> SQL}.
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

        // 2) DISTINCT + alias (no mezclar overloads de Select)
        {
            var q = new SelectQueryBuilder("Orders", "SALES")
                .Distinct()
                .Select("ORDER_ID", "CUSTOMER_ID", "TOTAL_AMOUNT")
                .Select(("ORDER_DATE", "FEC_ORD"))
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

        // 6) ORDER BY combinado (None/Desc/Asc)
        {
            var q = new SelectQueryBuilder("Products", "INVENTORY")
                .Select("PRODUCT_ID", "SKU", "NAME", "CATEGORY", "PRICE", "STOCK_QTY")
                .OrderBy(("CATEGORY", SortDirection.None), ("PRICE", SortDirection.Desc), ("NAME", SortDirection.Asc))
                .Build();
            result["6_OrderBy_Combinado"] = q.Sql;
        }

        // 7) ORDER BY con CASE WHEN (sin “ASC” explícito => SortDirection.None)
        {
            var caseVipPrimero = new CaseWhenBuilder()
                .When("c.IS_VIP = 1").Then("0")
                .Else("1");

            var q = new SelectQueryBuilder("Customers", "SALES")
                .As("c")
                .Select("c.CUSTOMER_ID", "c.NAME", "c.IS_VIP", "c.CREATED_AT")
                .OrderByCase(caseVipPrimero, SortDirection.None)
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

        // 9) Múltiples JOIN (todos usando la firma “clásica”)
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
            result["9_Joins_Multiples"] = q.Sql;
        }

        // 10) GROUP BY + HAVING
        {
            var q = new SelectQueryBuilder("OrderLines", "INVENTORY")
                .As("l")
                .Join("Products", "INVENTORY", "p", "p.PRODUCT_ID", "l.PRODUCT_ID", "INNER")
                .Select("p.CATEGORY AS CATEGORY", "SUM(l.QTY * l.PRICE) AS TOTAL_SALES", "COUNT(*) AS LINES")
                .GroupBy("p.CATEGORY")
                .HavingFunction("SUM(l.QTY * l.PRICE) > 10000")
                .OrderBy(("TOTAL_SALES", SortDirection.Desc))
                .Build();
            result["10_GroupBy_Having"] = q.Sql;
        }

        // 11) SELECT con CASE WHEN como columna (usando SelectCase)
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
            result["11_Select_Case_As_Col"] = q.Sql;
        }

        // 12) WHERE EXISTS con subconsulta (usa el helper WhereExists(Action<SelectQueryBuilder>))
        {
            var q = new SelectQueryBuilder("Customers", "SALES")
                .As("c")
                .Select("c.CUSTOMER_ID", "c.NAME", "c.EMAIL")
                .WhereExists(sub =>
                {
                    sub = new SelectQueryBuilder("Orders", "SALES")
                        .As("o")
                        .Select("1")
                        .WhereRaw("o.CUSTOMER_ID = c.CUSTOMER_ID")
                        .WhereRaw("o.STATUS = 'OPEN'")
                        .WhereBetween("o.ORDER_DATE", "2024-01-01", "2024-12-31")
                        .Limit(1);
                })
                .Build();
            result["12_Exists"] = q.Sql;
        }

        // 13) Paginación (OFFSET/FETCH)
        {
            var q = new SelectQueryBuilder("AuditLog", "LOGS")
                .Select("ID", "EVENT_DATE", "USER_ID", "ACTION", "DETAIL")
                .WhereBetween("EVENT_DATE", "2024-10-01", "2024-10-31")
                .OrderBy(("EVENT_DATE", SortDirection.Desc))
                .Offset(75)
                .FetchNext(25)
                .Build();
            result["13_Paginacion"] = q.Sql;
        }

        // 14) Top N (FETCH FIRST)
        {
            var q = new SelectQueryBuilder("AuditLog", "LOGS")
                .Select("*")
                .Limit(100)
                .Build();
            result["14_TopN"] = q.Sql;
        }

        // 15) “Grande” (varios JOIN + CASE + HAVING + ORDER BY CASE + paginado)
        {
            var vipFirst = new CaseWhenBuilder()
                .When("c.IS_VIP = 1").Then("0")
                .Else("1");

            var q = new SelectQueryBuilder("Customers", "SALES")
                .As("c")
                .Join("Orders", "SALES", "o", "o.CUSTOMER_ID", "c.CUSTOMER_ID", "LEFT")
                .Join("Payments", "FINANCE", "p", "p.CUSTOMER_ID", "c.CUSTOMER_ID", "LEFT")
                .Select(
                    "c.CUSTOMER_ID", "c.NAME", "c.EMAIL", "c.CITY", "c.IS_VIP",
                    "COALESCE(SUM(o.TOTAL_AMOUNT), 0) AS TOTAL_ORDERS",
                    "COUNT(p.PAYMENT_ID) AS PAY_COUNT"
                )
                .WhereRaw("COALESCE(c.STATUS, 'ACTIVE') = 'ACTIVE'")
                .GroupBy("c.CUSTOMER_ID", "c.NAME", "c.EMAIL", "c.CITY", "c.IS_VIP")
                .HavingFunction("COALESCE(SUM(o.TOTAL_AMOUNT), 0) >= 25000")
                .OrderByCase(vipFirst, SortDirection.None)
                .OrderBy(("TOTAL_ORDERS", SortDirection.Desc), ("c.NAME", SortDirection.Asc))
                .Offset(0).FetchNext(50)
                .Build();

            result["15_Grande"] = q.Sql;
        }

        return result;
    }

    /// <summary>
    /// Helper para probar: imprime cada SQL por consola.
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
   DemoSelectSamples.PrintAllToConsole();

   // O si sólo quieres el diccionario para inspección:
   var sqls = DemoSelectSamples.BuildAll();
   Console.WriteLine(sqls["15_Grande"]);
*/
