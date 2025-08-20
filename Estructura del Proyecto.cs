// --- teclas de navegación/edición que permitimos sin escribir ---
private static readonly ALLOWED_KEYS = new Set<string>([
  'Backspace', 'Delete', 'ArrowLeft', 'ArrowRight', 'Tab', 'Home', 'End'
]);

// --- atajos con Ctrl/Meta permitidos ---
private static readonly CTRL_COMBOS = new Set<string>([
  'a', // seleccionar todo
  'c', // copiar
  'v', // pegar
  'x', // cortar
  'z', // deshacer
  'y'  // rehacer
]);

/** Devuelve true si la tecla es de control/navegación o un atajo Ctrl/Meta permitido */
private isControlKey(e: KeyboardEvent): boolean {
  const k = e.key;
  const isCtrl = e.ctrlKey || e.metaKey;

  // lookup O(1), sin cadenas de OR
  return (
    ConsultaTarjetaComponent.ALLOWED_KEYS.has(k) ||
    (isCtrl && ConsultaTarjetaComponent.CTRL_COMBOS.has(k.toLowerCase()))
  );
}
