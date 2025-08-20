import { firstValueFrom } from 'rxjs';
import { take } from 'rxjs/operators';

/** Obtiene el nombre a registrar (seleccionado > encontrado) en MAYÚSCULAS */
private getNombreParaRegistrar(numeroTarjeta: string): string {
  const seleccionado = this.tarjetaSeleccionada?.nombre ?? null;
  const encontrado = this.dataSource.data.find(x => x.numero === numeroTarjeta)?.nombre ?? null;
  const nombre = seleccionado ?? encontrado ?? '';
  return nombre.toUpperCase();
}

/** Quita la fila de la UI y marca change detection */
private removeFromUi(numeroTarjeta: string): void {
  this.dataSource.data = this.dataSource.data.filter(item => item.numero !== numeroTarjeta);
  this.cdr.markForCheck();
}

/**
 * Marca una tarjeta como impresa:
 * - NO elimina la fila hasta que el backend confirme
 * - Muestra snackbar de éxito/fracaso
 */
public eliminarTarjeta(event: Event, numeroTarjeta: string): void {
  event.stopPropagation();
  if (!numeroTarjeta) return; // guard clause

  const nombreParaRegistrar = this.getNombreParaRegistrar(numeroTarjeta);

  // Mantén tu validación de sesión; ejecutamos dentro la lógica completa
  this.withActiveSession(() => {
    // IIFE async para poder usar await dentro aunque withActiveSession espere sync
    (async () => {
      try {
        await firstValueFrom(
          this.datosTarjetaServices
            .guardaEstadoImpresion(numeroTarjeta, this.usuarioICBS, nombreParaRegistrar)
            .pipe(take(1))
        );

        // ✅ Sólo aquí (cuando el backend respondió OK) removemos de la UI
        this.removeFromUi(numeroTarjeta);

        this.showSnackOk('Tarjeta marcada como impresa.');
      } catch {
        // ❌ No removemos nada si falla
        this.showSnack('No se pudo registrar la impresión. Intenta de nuevo.');
      }
    })();
  });
}

this.withActiveSession(async () => {
  try {
    await firstValueFrom(
      this.datosTarjetaServices
        .guardaEstadoImpresion(numeroTarjeta, this.usuarioICBS, nombreParaRegistrar)
        .pipe(take(1))
    );
    this.removeFromUi(numeroTarjeta);
    this.showSnackOk('Tarjeta marcada como impresa.');
  } catch {
    this.showSnack('No se pudo registrar la impresión. Intenta de nuevo.');
  }
});
