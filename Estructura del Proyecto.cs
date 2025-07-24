Necesito este SQL convertido a usar el paquete QueryBuilder

command.CommandText = $@"
        UPDATE BCAH96DTA.RSAGE01
        SET NOMAGE = '{agencia.NomAge}', ZONA = {agencia.Zona},
            MARQUESINA = '{agencia.Marquesina}', RSTBRANCH = '{agencia.RstBranch}',
            NOMBD = '{agencia.NomBD}', NOMSER = '{agencia.NomSer}', IPSER = '{agencia.IpSer}'
        WHERE CODCCO = {agencia.Codcco}";
