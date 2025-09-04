Vamos llegando al punto, de lo que tienes no andas lejos de lo que quiero, pero para que me comprendas mejor:

Lo que si puedo asegurar es que van fijos y en este orden los siguientes elementos:

1. Inicio de Log

2. Enviroment Info

3. Controlador
4. Request Info

5. Response Info
6. Errores(Si se presentan)
7. Fin de Log

Esos son fijos porque se obtienen de forma automatica, pero logs como HTTP, SQL, ADDSingleLog, ADDOBjLog, etc., estos dependen de en que momento sucedio el evento, puede haber casos en los que primero se ejecute un comando sql, y luego un HTTP, o caso en los que primero se ejecute un HTTP y luego un ADDSingleLog, fuera de los que son estaticos la posicion de estos dependera del momento en el que se ejecutan.
Para seguir la logica estos deberan quedar siempre entre los elementos fijos 4 y 5, pero posicionados segun su ejecución.
No se si es posible lograr esto, si lo es dimelo e indicame que código necesitas que te facilite para que puedas ver la ultima versión de código que tengo y así agregues las mejoras sin alterar las funcionalidades existentes.
