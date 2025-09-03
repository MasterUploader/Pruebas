Así tengo actualmente esos metodos

   /// <inheritdoc />
   public void LogDatabaseSuccess(SqlLogModel model, HttpContext? context = null)
   {
       // Usa el formateador que ya tienes para texto plano, si lo deseas
       var formatted = LogFormatter.FormatDbExecution(model);

       LogHelper.SaveStructuredLog(formatted, context);
   }

   /// <summary>
   /// Método para registrar comandos SQL fallidos
   /// </summary>
   /// <param name="command"></param>
   /// <param name="ex"></param>
   /// <param name="context"></param>
   public void LogDatabaseError(DbCommand command, Exception ex, HttpContext? context = null)
   {
       try
       {
           var connectionInfo = LogHelper.ExtractDbConnectionInfo(command.Connection?.ConnectionString);
           var tabla = LogHelper.ExtractTableName(command.CommandText);

           var formatted = LogFormatter.FormatDbExecutionError(
               nombreBD: connectionInfo.Database,
               ip: connectionInfo.Ip,
               puerto: connectionInfo.Port,
               biblioteca: connectionInfo.Library,
               tabla: tabla,
               sentenciaSQL: command.CommandText,
               exception: ex,
               horaError: DateTime.Now
           );

           WriteLog(context, formatted);
           AddExceptionLog(ex); // También lo guardás como log general si usás esa ruta
       }
       catch (Exception errorAlLoguear)
       {
           LogInternalError(errorAlLoguear);
       }
   }


Aplica las correcciones sin cambiar su funcionalidad
