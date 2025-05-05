command.CommandText = @"
    SELECT CODCCO, NOMAGE 
    FROM BCAH96DTA.RSAGE01 
    WHERE MARQUESINA = 'SI' 
    ORDER BY 
        CASE WHEN CODCCO = '0' THEN 0 ELSE 1 END,
        NOMAGE";
