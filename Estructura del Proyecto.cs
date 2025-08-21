// Máximo de letras repetidas consecutivas permitidas
private readonly MAX_REPEAT = 3;

/** Recorta cualquier racha de la misma letra que exceda MAX_REPEAT */
private clampRepeatedLetters(text: string): string {
  // Solo letras A-Z y Ñ (mayúsculas); si trabajas en minúsculas, añade la i al regex y normaliza
  return (text || '').replace(/([A-ZÑ])\1{3,}/g, (_m, ch) => ch.repeat(this.MAX_REPEAT));
}





validarNombre(nombre: string): string {
  let nombreValido = (nombre || '').toUpperCase();

  // 1) Mantén solo letras y espacios
  nombreValido = nombreValido.replace(/[^A-ZÑ\s]/g, '');

  // 2) Colapsa espacios múltiples
  nombreValido = nombreValido.replace(/\s+/g, ' ');

  // 3) Limita rachas de letras repetidas a MAX_REPEAT
  nombreValido = this.clampRepeatedLetters(nombreValido);

  return nombreValido;
}


validarEntrada(event: Event): void {
  const input = event.target as HTMLInputElement;
  let valor = (input.value || '').toUpperCase();

  // Solo letras + espacios y colapsa espacios
  valor = valor.replace(/[^A-ZÑ\s]/g, '').replace(/\s{2,}/g, ' ');

  // Limita rachas de letras repetidas a MAX_REPEAT (evita LLLL, AAAA, etc.)
  valor = this.clampRepeatedLetters(valor);

  input.value = valor;
  this.tarjeta.nombre = valor;

  if (this.disenoSeleccionado === 'dosFilas') this.dividirNombreCompleto(valor);

  this.nombreCharsCount = valor.length;
  this.aplicarValidaciones(valor);

  this.emitirNombreCambiado();
}



// Al final de aplicarValidaciones, si no hay errores previos:
if (!this.nombreError && /([A-ZÑ])\1{3,}/.test((valor || '').toUpperCase())) {
  this.nombreError = `No se permiten más de ${this.MAX_REPEAT} letras iguales consecutivas.`;
}

