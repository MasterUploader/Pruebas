// Teclas de control permitidas (mover, borrar, tab…)
private static readonly CONTROL_KEYS = new Set<string>([
  'Backspace','Delete','ArrowLeft','ArrowRight','ArrowUp','ArrowDown','Tab','Home','End'
]);

/** ¿El valor contiene 3 o más letras idénticas consecutivas en algún lugar? */
private hasTripleRun(value: string): boolean {
  return /([A-ZÑ])\1{2}/.test((value || '').toUpperCase());
}


/** Si ya hay una racha de 3, no permitimos seguir escribiendo nada (solo teclas de control) */
bloquearSiLimite(e: KeyboardEvent): void {
  if (e.ctrlKey || e.metaKey || ModalTarjetaComponent.CONTROL_KEYS.has(e.key)) return;
  const input = e.target as HTMLInputElement;
  if (this.hasTripleRun(input.value)) e.preventDefault();
}


validarEntrada(event: Event): void {
  const input = event.target as HTMLInputElement;
  let valor = (input.value || '').toUpperCase();

  // Solo letras/espacios; colapsa espacios
  valor = valor.replace(/[^A-ZÑ\s]/g, '').replace(/\s{2,}/g, ' ');

  // Si el pegado/auto-completado trae más de 3 seguidas, recorta a 3
  // (seguirá siendo inválido por la regla, pero evita “carreras” más largas)
  if (this.hasTripleRun(valor)) {
    valor = valor.replace(/([A-ZÑ])\1{3,}/g, (_m, ch) => ch.repeat(3));
  }

  input.value = valor;
  this.tarjeta.nombre = valor;

  if (this.disenoSeleccionado === 'dosFilas') this.dividirNombreCompleto(valor);

  this.nombreCharsCount = valor.length;
  this.aplicarValidaciones(valor);   // ⬅️ Ahora marcará error por triple repetición
  this.emitirNombreCambiado();
}

onPasteNombre(e: ClipboardEvent): void {
  const clip = (e.clipboardData?.getData('text') || '').toUpperCase();
  const limpio = clip.replace(/[^A-ZÑ\s]/g, '').replace(/\s{2,}/g, ' ');
  const input = e.target as HTMLInputElement;
  const combinado = (input.value || '') + limpio;

  if (this.hasTripleRun(combinado)) e.preventDefault();
}



private aplicarValidaciones(valor: string): void {
  const limpio = (valor || '').toUpperCase().trim().replace(/\s{2,}/g, ' ');
  const palabras = limpio ? limpio.split(' ') : [];

  this.nombreError = '';

  if (!limpio) {
    this.nombreError = 'El nombre no puede estar vacío.';
    return;
  }
  if (palabras.length < 2) {
    this.nombreError = 'Debe ingresar al menos nombre y apellido (mínimo 2 palabras).';
    return;
  }
  if (this.hasTripleRun(limpio)) {
    this.nombreError = 'No se permiten 3 o más letras idénticas consecutivas en ninguna palabra.';
    return;
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


  
