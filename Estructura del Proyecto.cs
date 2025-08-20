Este codigo me dice que tiene complejidad de 12, por favor ayudame a optimizarlo:

login(): void {
    if (this.loginForm.invalid || this.isLoading) return;

    const { userName, password } = this.loginForm.value;

    // Reset de estado previo
    this.errorMessage = '';

    // Deshabilita controles correctamente (evita warnings)
    this.isLoading = true;
    this.loginForm.disable({ emitEvent: false });

    this.authService.login(userName, password)
      .pipe(
        take(1),
        finalize(() => {
          this.isLoading = false;
          this.loginForm.enable({ emitEvent: false }); // reactivar SIEMPRE
        })
      )
      .subscribe({
        next: () => this.router.navigate(['/tarjetas']),
        error: (error: HttpErrorResponse) => {
          const msgFromApi =
            (error?.error && (error.error.codigo?.message || error.error.message)) ||
            (typeof error?.error === 'string' ? error.error : null);

          this.errorMessage = msgFromApi || 'Ocurrió un error durante el inicio de sesión';

          // Notificación flotante (si no la quieres, quita esta línea)
          this.snackBar.open(this.errorMessage, 'Cerrar', { duration: 5000 });
        }
      });
  }
