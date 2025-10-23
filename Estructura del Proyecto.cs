/** Normaliza una fecha (string ISO o Date) a milisegundos epoch, o null si inválida. */
private toEpochMs(exp: string | Date | null | undefined): number | null {
  if (!exp) return null;
  const d = exp instanceof Date ? exp : new Date(exp);
  const ms = d.getTime();
  return Number.isNaN(ms) ? null : ms;
}

/**
 * Mantiene viva la sesión en el servidor y sincroniza token/expiración locales.
 * - Acepta que el backend devuelva: { token?, expiration? } o { full: LoginResponse }.
 * - No navega; sólo actualiza almacenamiento y subjects.
 */
keepAlive(): Observable<void> {
  return this.http.post<{
    expiration?: string | Date | null;
    token?: string | null;
    full?: LoginResponse | null;
  }>(`${this.apiUrl}/api/Auth/KeepAlive`, {})
    .pipe(
      map(resp => {
        // 1) Caso: el backend devuelve el objeto completo.
        if (resp?.full) {
          this.persistSession(resp.full);
          return;
        }

        // 2) Token y/o expiración sueltos (posibles undefined/null).
        const token = resp?.token ?? null;
        const expirationAny = resp?.expiration ?? null;

        // Calcula ms para expiresAt si vino una expiración válida.
        const expMs = this.toEpochMs(expirationAny);
        if (expMs !== null) {
          localStorage.setItem('expiresAt', String(expMs));
        }

        // 3) Actualiza el usuario actual en memoria si existe.
        const current = this.currentUserSubject.value; // LoginResponse | null
        if (current !== null) {
          // Token renovado
          if (token !== null) {
            current.token.token = token;
            localStorage.setItem('token', token);
          }

          // Nueva expiración tipada como Date en el modelo
          if (expMs !== null) {
            current.token.expiration = new Date(expMs);
          }

          // Persiste una sola vez y emite una sola vez
          localStorage.setItem('currentUser', JSON.stringify(current));
          this.currentUserSubject.next({ ...current }); // copia superficial para disparar change detection
        } else {
          // No hay usuario cargado en memoria (caso raro). Aún así,
          // si vino token o expiración, deja al menos el storage consistente.
          if (token !== null) {
            localStorage.setItem('token', token);
          }
          // expiresAt ya se guardó más arriba si expMs !== null
        }

        // 4) Recalcula flag de sesión activa.
        this.sessionActive.next(this.sessionIsActive());
      }),
      catchError((err: HttpErrorResponse) => {
        // Si el keep-alive falla por 401/403, la sesión está muerta en servidor.
        if (err.status === 401 || err.status === 403) {
          this.logout();
        }
        return throwError(() => err);
      })
    );
}
