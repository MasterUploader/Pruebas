// Teclas de control permitidas para poder corregir
private static readonly CONTROL_KEYS = new Set<string>([
  'Backspace','Delete','ArrowLeft','ArrowRight','ArrowUp','ArrowDown','Tab','Home','End'
]);

/** Verdadero si el valor actual ya tiene alguna racha de 3 letras iguales seguidas */
private hasTripleRun(value: string): boolean {
  return /([A-ZÑ])\1{2}/.test((value || '').toUpperCase());
}

/** Bloquea cualquier nueva entrada si ya hay 3 repetidas consecutivas en el valor */
bloquearSiLimite(e: KeyboardEvent): void {
  const key = e.key;
  // Permitir combinaciones con Ctrl/Meta (copiar, pegar, etc.) y teclas de control
  if (e.ctrlKey || e.metaKey || ModalTarjetaComponent.CONTROL_KEYS.has(key)) return;

  const input = e.target as HTMLInputElement;
  if (this.hasTripleRun(input.value)) {
    // Ya existe una racha de 3 → no dejamos escribir nada más
    e.preventDefault();
  }
}

validarEntrada(event: Event): void {
  const input = event.target as HTMLInputElement;
  let valor = (input.value || '').toUpperCase();

  // Solo letras + espacios y colapsa espacios
  valor = valor.replace(/[^A-ZÑ\s]/g, '').replace(/\s{2,}/g, ' ');

  // Si ya tiene triple, no dejamos crecer el string: lo dejamos como estaba antes del input
  // (el keydown ya lo bloquea, pero esto cubre pegas/autocompletado)
  if (this.hasTripleRun(valor)) {
    // Recorta para que no pase de 3 iguales consecutivas
    valor = valor.replace(/([A-ZÑ])\1{3,}/g, (_m, ch) => ch.repeat(3));
  }

  input.value = valor;
  this.tarjeta.nombre = valor;

  if (this.disenoSeleccionado === 'dosFilas') this.dividirNombreCompleto(valor);

  this.nombreCharsCount = valor.length;
  this.aplicarValidaciones(valor);
  this.emitirNombreCambiado();
}

onPasteNombre(e: ClipboardEvent): void {
  const clip = (e.clipboardData?.getData('text') || '').toUpperCase();
  const soloLetras = clip.replace(/[^A-ZÑ\s]/g, '').replace(/\s{2,}/g, ' ');
  const input = e.target as HTMLInputElement;
  const combinado = (input.value || '') + soloLetras;

  if (this.hasTripleRun(combinado)) {
    e.preventDefault();
  }
}

<input
  placeholder="Nombre en Tarjeta"
  matInput
  [(ngModel)]="tarjeta.nombre"
  (keydown)="bloquearSiLimite($event)"
  (input)="validarEntrada($event)"
  (paste)="onPasteNombre($event)"
  (keypress)="prevenirNumeroCaracteres($event)"
  maxlength="26"
  required
  [errorStateMatcher]="nombreErrorMatcher">
