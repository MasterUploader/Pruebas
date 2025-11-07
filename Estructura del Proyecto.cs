1. Contexto

Entre el 23 y el 27 de noviembre de 2024 se presentaron incidencias en el procesamiento del archivo INCOMING de operaciones de tarjeta.
Se realizaron reprocesos y limpiezas sobre tablas y objetos asociados (entre ellas PRODUCTOS.INCOMGLF00, librerías EDPLIB INC24329 / INC24330) con el objetivo de corregir diferencias contables y registros pendientes.

En paralelo existía un proceso automático nocturno (ICBS / cierre diario) encargado de reprocesar transacciones rechazadas: revisa una tabla de rechazos y, cuando detecta saldo disponible en la cuenta, aplica el cobro en forma automática.


---

2. Error reportado

Los clientes reportan:

Cobros duplicados (manual + automático) de transacciones del incoming.

Cobros con montos erróneos, específicamente montos multiplicados por 100.

Diferencias contables significativas entre los archivos procesados y los saldos de las cuentas.


El área de Tarjetas informa que el proceso de incoming se ejecutó “como todos los días” desde su perspectiva funcional, por lo que requiere análisis técnico.


---

3. Cronología resumida

1. 23/11

El archivo de incoming no genera el proceso contable correcto.

Se solicita limpieza de tablas y reproceso.



2. 24/11

Se ejecuta reverso de ~1950 transacciones.

Persisten ~10,000 transacciones pendientes de cobro.



3. 25/11

El archivo de incoming no se procesa; queda en bandejas pendientes.

Se solicita soporte para limpieza de bandejas y reproceso.



4. 26/11

Se instruye:

Ejecutar sentencias sobre PRODUCTOS.INCOMGLF00 filtrando por NARCH = INC24329 / INC24330.

Eliminar objetos en EDPLIB INC24329 / INC24330.


Se otorga VoBo técnico para estos cambios.

Comienzan a evidenciarse cobros incorrectos en clientes (duplicados y montos x100).



5. 26–27/11

Se solicitan ejemplos de clientes afectados.

Se identifica que un programa del Proceso Cierre Diario de ICBS:

Lee la tabla de rechazos del incoming.

Aplica cobros automáticos cuando encuentra saldo.

Está utilizando registros inconsistentes/incorrectos (incluyendo montos multiplicados por 100).


Se confirma que este programa genera los cobros indebidos sobre transacciones ya intervenidas en los reprocesos.



6. 27/11

Se acuerda inhabilitar temporalmente el programa automático.

Se solicita envío de base/archivo (POD / Excel) con las transacciones para realizar reversión masiva y normalización.





---

4. Impacto a clientes

Cobros duplicados: clientes con transacciones aplicadas más de una vez (manual + automático).

Cobros con monto incorrecto: valores multiplicados por 100 respecto al importe real.

Descuadre de saldos en cuentas de ahorro/corriente.

Incremento de reclamos, llamadas y gestión operativa.

Riesgo reputacional y regulatorio por cobros indebidos y demoras en la corrección.



---

5. Plan de acción

Acciones inmediatas

1. Deshabilitar el programa automático de recobro del cierre diario que usa la tabla de rechazos, para detener nuevos cobros indebidos.


2. Congelar nuevos reprocesos hasta tener un inventario completo de:

Tablas afectadas (INCOMGLF00, tabla de rechazados, objetos en EDPLIB, etc.).

Registros eliminados, reprocesados o pendientes.



3. Reversión controlada:

Utilizar el archivo POD / Excel con las transacciones identificadas.

Ejecutar reversos masivos sobre los cobros duplicados o con monto x100.

Conciliar saldos finales de las cuentas afectadas.



4. Comunicación formal:

Informar a las áreas de negocio, servicio al cliente y cumplimiento sobre el incidente, pasos de corrección y tiempos.




Acciones correctivas / preventivas

1. Revisión de la tabla de rechazados:

Asegurar que después de un reproceso se limpien o actualicen correctamente los registros para que el programa automático no reutilice información obsoleta.



2. Control de dependencias:

Documentar que cualquier cambio/reproceso del incoming requiere:

Validar el impacto sobre la tabla de rechazados.

Coordinar activación/desactivación del programa automático.




3. Validaciones técnicas adicionales:

Implementar validación de rangos (ej. detección de montos anómalos x100) antes de aplicar cargos.

Trazabilidad clara (logs) que permita ver origen del cargo: manual, incoming normal o programa automático.



4. Procedimiento de cambio:

Incorporar en el flujo de control de cambios:

Checklist de procesos batch dependientes.

Revisión obligatoria por Tecnología y Operaciones antes de autorizar eliminaciones masivas y reprocesos.






---

6. Conclusiones

1. El incidente no se debió a un único error operativo aislado, sino a la combinación de reprocesos del incoming y la ejecución activa de un programa automático de recobro, que consumía datos inconsistentes.


2. La falta de alineación entre limpieza de tablas, reprocesos y lógica del programa automático permitió:

Cobros duplicados.

Cobros con montos incorrectos multiplicados por 100.



3. El riesgo se materializó por ausencia de:

Control estricto de dependencias entre procesos.

Validaciones automáticas de integridad de montos y estados antes de aplicar cargos.





---

7. Recomendaciones

1. Mapeo formal de procesos
Definir y mantener un diagrama/procedimiento donde se documenten todos los programas batch relacionados con incoming, tablas de trabajo y reprocesos.


2. Gestión de cambios reforzada

Todo reproceso o eliminación masiva debe:

Revisar procesos automáticos dependientes (activar/desactivar según corresponda).

Contar con plan de reversión y evidencia de aprobación técnica y de negocio.




3. Validaciones de negocio automáticas

Reglas para bloquear:

Montos fuera de rango esperado.

Cobros sobre transacciones ya regularizadas.


Alertas tempranas al detectar patrones anómalos (múltiples cargos iguales en corto periodo).



4. Mejora de logging y auditoría

Registrar origen del cobro (manual, incoming, batch de recobro).

Mantener logs que permitan reconstruir rápidamente qué programa generó cada cargo.



5. Pruebas en ambiente controlado

Antes de ejecutar reprocesos en producción, simular el escenario completo (incluyendo programas automáticos) en un ambiente de pruebas con datos representativos.




Si quieres, en el siguiente mensaje puedo adaptar este informe al formato corporativo que uses (con numeración, responsables y fechas) para que lo presentes directamente.
