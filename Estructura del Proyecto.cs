Así deje el codigo y funciona, pero necesito que this.getNombreParaRegistrar(numeroTarjeta) donde muestra el numero de la tarjeta este oculto

public eliminarTarjeta(event: Event, numeroTarjeta: string): void {

    event.stopPropagation();

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '350px',
      data: {
        title: 'Confirmar eliminación',
        message: `¿Seguro que deseas eliminar la tarjeta ${numeroTarjeta}?`
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === true) {
        if (!numeroTarjeta) return; // guard clause

        const nombreParaRegistrar = this.getNombreParaRegistrar(numeroTarjeta);

        // Mantén tu validación de sesión; ejecutamos dentro la lógica completa
        this.withActiveSession(async () => {
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
    });


  }
}
