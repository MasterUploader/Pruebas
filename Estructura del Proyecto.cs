El comando lo crea de esta forma
SELECT TIPUSU, ESTADO, PASS FROM BCAH96DTA.USUADMIN WHERE (USUARIO = @p0)
  
Y da este error 

System.NullReferenceException: 'Object reference not set to an instance of an object.'

  Si yo reviso las propiedades de command, y verifico el campo param este indica el valor correcto, pero a lo interno no genero el comando como corresponde.
