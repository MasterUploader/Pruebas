/** Devuelve el primer string no vacío de la lista (sin ifs anidados) */
private pickFirstString(...candidates: unknown[]): string | null {
  for (const c of candidates) {
    if (typeof c === 'string') {
      const t = c.trim();
      if (t) return t;
    }
  }
  return null;
}

/** Extrae un mensaje legible del HttpErrorResponse (cuerpo string u objeto) */
private extractErrorMessage(err: HttpErrorResponse): string | null {
  if (!err) return null;

  const payload = err.error;

  // Si el backend devolvió un string tal cual
  const fromStringBody = typeof payload === 'string' ? payload : null;

  // Si devolvió un objeto, probamos campos comunes en orden de preferencia
  const fromObjectBody =
    payload && typeof payload === 'object'
      ? this.pickFirstString(
          (payload as any).codigo?.message,
          (payload as any).message,
          (payload as any).error_description,
          (payload as any).error
        )
      : null;

  // Como último recurso, el message del propio HttpErrorResponse
  return this.pickFirstString(fromObjectBody, fromStringBody, err.message);
}

/** Normaliza cualquier HttpErrorResponse a un Error con .message legible */
private toUserFacingError(error: HttpErrorResponse): Error {
  const msg = this.extractErrorMessage(error) ?? 'Ocurrió un error durante el inicio de sesión';
  return new Error(msg);
}




catchError((err: HttpErrorResponse) => throwError(() => this.toUserFacingError(err)))
