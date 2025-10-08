Antes de los cambios de las descripciones y con pisteos exitosos para D y C, esto es lo que mandaba en INT_LOTES por ejemplo sin errores:

Esta Naturaleza C
CALL BCAH96.INT_LOTES(40, 293990015, 224.60000610351562, 'D', 198, 0, 1, 1240015443, 224.60000610351562, 'C', 0, 0, 0, 0, 0, '', 0, 0, 0, 0, 0, '', 0, 0, 'ADQ_INTERFAZ', 0, 'Banco Davivienda', '4000009-P0055638', 'CR-PRWS-P0055638-251007-00-AHO-293990015', 'Banco Davivienda', '4000009-P0055638', 'CR-PRWS-P0055638-251007-00-AHO', 0, '', '          ')
Esta Naturaleza D
    CALL BCAH96.INT_LOTES(1, 1011940165, 10, 'D', 0, 0, 40, 293990015, 10, 'C', 198, 0, 0, 0, 0, '', 0, 0, 0, 0, 0, '', 0, 0, 'ADQ_INTERFAZ', 0, 'Ferreteria el Ahorro', '4000006-P0055469', 'DB-ABCD-1234-AHO', 'Ferreteria el Ahorro', '4000006-P0055469', 'DB-ABCD-1234-AHO-293990015', 0, '', '          ')}


    Y esto es lo que mando ahora y me devuelve errores:

CALL BCAH96.INT_LOTES(40, 0, 224.60000610351562, 'D', 198, 0, 1, 1240015443, 224.60000610351562, 'C', 0, 0, 0, 0, 0, '', 0, 0, 0, 0, 0, '', 0, 0, 'ADQ_INTERFAZ', 0, 'Banco Davivienda', '4000009-P0055638', 'CR-PRWS-P0055638-250805-00-AHO-293990015', 'Banco Davivienda', '4000009-P0055638', 'CR-PRWS-P0055638-250805-00-AHO', 1, 'TRANSACCION UNO/DOS SIN VALORES NO SE GENERO LOTE', '          ')

    Por lo que veo no esta retrayendo correctamente la información en este caso para el campo CuentaMov1, es posible que no este interpretando correctamente cuando sea C o D y colocando cuando corresponde la información.
