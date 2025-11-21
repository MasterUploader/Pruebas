SELECT
    wo.WORKORDERID AS "Request id",
    wo.TITLE AS "Subject",
    wo.CREATEDTIME AS "Created time",
    gd_main.QUEUENAME AS "Grupo",

    (
        -- Subconsulta para columna "Grupos pasados"
        SELECT STRING_AGG(gd_sub.QUEUENAME, ', ') AS assessindex
        FROM WO_Assessment wa
        JOIN WO_Group_Info wfi
            ON wa.ASSESSMENTID = wfi.ASSESSMENTID
        LEFT JOIN QueueDefinition gd_sub
            ON wfi.NEXTGROUPID = gd_sub.QUEUEID
        WHERE wa.WORKORDERID = wo.WORKORDERID
          AND wfi.NEXTGROUPID IS NOT NULL
    ) AS "Grupos pasados"

FROM workorder wo

LEFT JOIN workorder_queue wo_main
    ON wo.WORKORDERID = wo_main.WORKORDERID

LEFT JOIN QueueDefinition gd_main
    ON wo_main.QUEUEID = gd_main.QUEUEID

WHERE

    EXISTS (
        SELECT 1
        FROM WO_Assessment wa_hist
        JOIN WO_Group_Info wfi_hist
            ON wa_hist.ASSESSMENTID = wfi_hist.ASSESSMENTID
        JOIN QueueDefinition gd_hist
            ON wfi_hist.NEXTGROUPID = gd_hist.QUEUEID
        WHERE
            wa_hist.WORKORDERID = wo.WORKORDERID
            AND gd_hist.QUEUENAME IN (CAST('Receptoría SPS' AS VARCHAR(200)),
                                                                   CAST( 'Receptoría TGU' AS VARCHAR(200))
    )

    AND wo.CREATEDTIME >= '2025-01-01 00:00:00'
    AND wo.CREATEDTIME <  '2025-10-31 00:00:00'
;
