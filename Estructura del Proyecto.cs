// antes:
interface KeepAliveResponse {
  expiration?: string | null;
  token?: string | null;
  full?: LoginResponse | null;
}

// después (admite string o Date):
interface KeepAliveResponse {
  expiration?: string | Date | null;
  token?: string | null;
  full?: LoginResponse | null;
}





/** Normaliza una fecha (string ISO o Date) a milisegundos epoch. */
private toEpochMs(exp: string | Date | null | undefined): number | null {
  if (!exp) return null;
  const d = exp instanceof Date ? exp : new Date(exp);
  const ms = d.getTime();
  return isNaN(ms) ? null : ms;
}






const expiresAt = new Date(response.token.expiration);
localStorage.setItem('expiresAt', String(expiresAt.getTime()));




const expMs = this.toEpochMs(response.token.expiration);
if (expMs) {
  localStorage.setItem('expiresAt', String(expMs));
} else {
  // fallback por si el backend no envía expiración válida
  localStorage.removeItem('expiresAt');
}





// ✅ normalizamos y persistimos
const expMs = this.toEpochMs(expirationIso);
if (expMs) {
  localStorage.setItem('expiresAt', String(expMs));
  if (current) {
    current.token.expiration = new Date(expMs); // <- ahora sí es Date
    localStorage.setItem('currentUser', JSON.stringify(current));
    this.currentUserSubject.next(current);
  }
}
