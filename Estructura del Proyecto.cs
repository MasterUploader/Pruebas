Me esta generando el query asi


UPDATE BCAH96DTA.RSAGE01 SET NOMAGE = 'General', ZONA = '1', MARQUESINA = 'SI', RSTBRANCH = 'SI', NOMBD = 'Prueba', NOMSER = 'Pruebass', IPSER = '127.0.1.1'\r\nWHERE (CODCCO = 0)

Y deberia ser asi

UPDATE BCAH96DTA.RSAGE01 SET NOMAGE = 'General', ZONA = '1', MARQUESINA = 'SI', RSTBRANCH = 'SI', NOMBD = 'Prueba', NOMSER = 'Pruebass', IPSER = '127.0.1.1' WHERE (CODCCO = 0)
