Puede hacerse algo similar a esto no necesariamente el mismo formato:

.SelectCase( .When(TIPO = 'A') .THEN( 'Administrador') .WHEN( TIPO = 'U') .THEN( 'Usuario') .ELSE( 'Otro'), "DESCRIPCION")
