/**
 * ModalTarjetaComponent (OnPush)
 * ---------------------------------------------------------------------------
 * Alineado con el HTML provisto:
 *  - Variables públicas: `nombres`, `apellidos`, `tarjeta`, `nombreError`.
 *  - Método `imprimir(tarjeta)` (firma que usa el template).
 *  - Form reactivo con control `nombre` (formControlName="nombre").
 *  - Snackbars: éxito al imprimir, validación cuando es inválido.
 *  - No toca tu diseño; solo lógica y nombres que el template espera.
 * ---------------------------------------------------------------------------
 */

import {
  Component, Inject, ChangeDetectionStrategy, ChangeDetectorRef, EventEmitter, Output
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import {
  FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators,
  AbstractControl, ValidationErrors
} from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

// ⚠️ Ajusta la ruta si tu proyecto la tiene distinta
import { MaskAccountNumberPipe } from '../../../../shared/pipes/mask-account-number.pipe';

export interface TarjetaLite {
  nombre: string;
  numero: string;
  fechaEmision?: string;
  fechaVencimiento?: string;
  motivo?: string;
  numeroCuenta?: string;
}

@Component({
  selector: 'app-modal-tarjeta',
  standalone: true,
  imports: [
    CommonModule, MatDialogModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatButtonModule, MatSnackBarModule,
    // Necesario para usar | maskAccountNumber en el HTML del modal
    MaskAccountNumberPipe
  ],
  templateUrl: './modal-tarjeta.component.html',
  styleUrl: './modal-tarjeta.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ModalTarjetaComponent {

  /** Form reactivo tipado con el control `nombre` (se usa formControlName en el HTML). */
  public form!: FormGroup<{ nombre: FormControl<string> }>;

  // Límites
  private readonly MAX_NAME_LEN = 40;
  private readonly MAX_LINE = 20;

  /** Lo que el HTML muestra como overlay sobre la tarjeta. */
  public nombres = '';   // Primera línea (antes line1)
  public apellidos = ''; // Segunda línea (antes line2)

  /**
   * El HTML espera `tarjeta` (no `data`) para `tarjeta.numeroCuenta`.
   * Usamos un getter que apunta al MAT_DIALOG_DATA:
   */
  get tarjeta(): TarjetaLite {
    return this.data;
  }

  /**
   * Único mensaje para el <mat-error> (el HTML muestra solo uno).
   * Prioridad: required > twoWords > pattern > maxlength.
   */
  get nombreError(): string | null {
    const c = this.form?.controls?.nombre;
    if (!c) return null;
    if (c.hasError('required'))  return 'El nombre es obligatorio.';
    if (c.hasError('twoWords'))  return 'Ingresa al menos dos nombres.';
    if (c.hasError('pattern'))   return 'Solo letras y espacios en MAYÚSCULAS.';
    if (c.hasError('maxlength')) return 'Máximo 40 caracteres.';
    return null;
  }

  /** Emite el nombre (en mayúsculas) mientras se escribe para que la tabla se actualice en vivo. */
  @Output() nombreCambiado = new EventEmitter<string>();

  constructor(
    @Inject(MAT_DIALOG_DATA) public readonly data: TarjetaLite,
    private readonly dialogRef: MatDialogRef<ModalTarjetaComponent>,
    private readonly fb: FormBuilder,
    private readonly cdr: ChangeDetectorRef,
    private readonly snackBar: MatSnackBar
  ) {
    // Inicial: nombre en mayúsculas (el HTML ya fuerza uppercase en (input), pero normalizamos)
    const inicio = (data?.nombre ?? '').toUpperCase();

    // Formulario
    this.form = this.fb.group({
      nombre: this.fb.control(inicio, {
        nonNullable: true,
        validators: [
          Validators.required,
          this.minTwoWords(),
          Validators.maxLength(this.MAX_NAME_LEN),
          Validators.pattern(/^[A-ZÑÁÉÍÓÚÜ\s]+$/) // solo mayúsculas y espacios
        ]
      })
    });

    // Suscripción a cambios del control (NO reasignamos el control para evitar loops con el (input) del HTML)
    this.nombreCtrl.valueChanges.subscribe(val => {
      const upper = (val ?? '').toUpperCase(); // mostramos en mayúsculas, sin setear al control
      // Overlay (dos líneas)
      const { line1, line2 } = this.computeTwoLines(upper);
      this.nombres = line1;
      this.apellidos = line2;

      // Notificar al padre para refrescar la grilla en vivo
      this.nombreCambiado.emit(upper);

      this.cdr.markForCheck();
    });

    // Calcular las líneas iniciales para el overlay
    const init = this.computeTwoLines(inicio);
    this.nombres = init.line1;
    this.apellidos = init.line2;
  }

  /** Acceso tipado al control; útil dentro del TS. */
  get nombreCtrl(): FormControl<string> {
    return this.form.controls.nombre;
  }

  // ───────────── Validadores y helpers ─────────────

  /** Valida al menos 2 palabras (si está vacío, deja que `required` se encargue). */
  private minTwoWords() {
    return (control: AbstractControl): ValidationErrors | null => {
      const raw = String(control.value ?? '').trim();
      if (!raw) return null; // `required` ya marcará el error
      const words = raw.split(/\s+/).filter(Boolean);
      return words.length >= 2 ? null : { twoWords: true };
    };
  }

  /**
   * Calcula 2 líneas sin cortar palabras, máximo 20 c/u.
   * Si la siguiente palabra no cabe, pasa a la segunda; si tampoco cabe, se omite
   * (mismo criterio que ya usabas).
   */
  private computeTwoLines(full: string): { line1: string; line2: string } {
    const tokens = (full ?? '').split(' ').filter(Boolean);
    let l1 = '', l2 = '';
    for (const t of tokens) {
      if (!l1.length || (l1.length + 1 + t.length) <= this.MAX_LINE) {
        l1 = l1 ? `${l1} ${t}` : t;
      } else if (!l2.length || (l2.length + 1 + t.length) <= this.MAX_LINE) {
        l2 = l2 ? `${l2} ${t}` : t;
      } else {
        break;
      }
    }
    return { line1: l1, line2: l2 };
  }

  // ───────────── Acciones (sin cambiar tu UX) ─────────────

  /** Botón "Cerrar". */
  public cerrarModal(): void {
    this.dialogRef.close();
  }

  /**
   * Botón "Imprimir" con la firma que usa tu HTML: (click)="imprimir(tarjeta)".
   * Delegamos en `imprimirTarjeta()` para mantener una única implementación.
   */
  public imprimir(_: TarjetaLite): void {
    this.imprimirTarjeta();
  }

  /** Lógica de impresión con validaciones y snackbar de éxito. */
  public imprimirTarjeta(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.snackBar.open('No puedes imprimir: corrige los errores del nombre.', 'Cerrar', {
        duration: 4000, verticalPosition: 'top', horizontalPosition: 'center'
      });
      return;
    }

    // El overlay ya está en this.nombres / this.apellidos; no hay que recalcular
    // (si quisieras recalcular por seguridad, descomenta las 2 líneas siguientes):
    // const { line1, line2 } = this.computeTwoLines(this.nombreCtrl.value.toUpperCase());
    // this.nombres = line1; this.apellidos = line2;

    // Impresión (mantengo tu comportamiento)
    window.print();

    // ✅ Snackbar de éxito
    this.snackBar.open('Impresión enviada a la impresora.', 'Cerrar', {
      duration: 3500, verticalPosition: 'top', horizontalPosition: 'center'
    });

    // Cerrar el modal
    this.dialogRef.close({ printed: true });
  }
}
