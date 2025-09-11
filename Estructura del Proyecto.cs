En el campo LOGB02UIL que es de tipo:

Campo              Archivo            Tipo               Longitud  Escal 
LOGB02UIL          ETD02LOG           TIMESTAMP                26     6 

La información se guarda así 2025-06-12-11.06.38.000000

Pero esta viajando así "9/11/2025 10:02:08 AM"

    No se tendria que hacer algo similar a esto?

    
            if (DateTime.TryParseExact(fecha2, "yyyy-MM-dd-HH.mm.ss.ffffff", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out fechaAnterior)) { }
