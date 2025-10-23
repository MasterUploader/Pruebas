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
  this.currentUserSubject.next({ ...current }); // copia superficial para change detection
}
// ⬇️ Antes: else { if (token !== null) { ... } }  →  Ahora: else if ( ... )
else if (token !== null) {
  // No hay usuario cargado en memoria (caso raro). Aún así,
  // si vino token, deja al menos el storage consistente.
  localStorage.setItem('token', token);
}
// (expiresAt ya se guardó más arriba si expMs !== null)
