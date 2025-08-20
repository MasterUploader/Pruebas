/** Extrae un mensaje legible del HttpErrorResponse con complejidad mínima */
private extractErrorMessage(err: HttpErrorResponse): string | null {
  const payload = err?.error;

  // 1) Si el backend devolvió un string tal cual
  if (typeof payload === 'string') {
    const t = payload.trim();
    return t ? t : null;
  }

  // 2) Si devolvió un objeto, probamos campos comunes en orden
  if (payload && typeof payload === 'object') {
    const p = payload as any;
    const candidates = [p?.codigo?.message, p?.message, p?.error_description, p?.error];
    for (const c of candidates) {
      if (typeof c === 'string') {
        const t = c.trim();
        if (t) return t;
      }
    }
  }

  // 3) Último recurso: el message del propio HttpErrorResponse
  const m = (err?.message ?? '').trim();
  return m ? m : null;
}
