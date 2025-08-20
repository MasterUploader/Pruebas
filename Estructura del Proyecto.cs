import { firstValueFrom } from 'rxjs';
import { take } from 'rxjs/operators';

// === Helpers de bajo acoplamiento ===

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

// === Método optimizado ===

/**
 * Marca una tarjeta como impresa:
 * - Detiene propagación del click
 * - Calcula nombre a registrar (una sola vez)
 * - Valida sesión y remueve la fila de la UI
 * - Llama al backend con await (sin subscribe next/error)
 * - Notifica éxito / error
 */
public async eliminarTarjeta(event: Event, numeroTarjeta: string): Promise<void> {
  event.stopPropagation();
  if (!numeroTarjeta) return; // guard clause

  const nombreParaRegistrar = this.getNombreParaRegistrar(numeroTarjeta);

  // Mantén tu helper de sesión; aquí solo ejecutamos el efecto de UI
  this.withActiveSession(() => this.removeFromUi(numeroTarjeta));

  try {
    await firstValueFrom(
      this.datosTarjetaServices
        .guardaEstadoImpresion(numeroTarjeta, this.usuarioICBS, nombreParaRegistrar)
        .pipe(take(1))
    );

    this.showSnackOk('Tarjeta marcada como impresa.');
  } catch {
    // Mantengo el mensaje genérico que tenías
    this.showSnack('No se pudo registrar la impresión. Intenta de nuevo.');
  }
}
