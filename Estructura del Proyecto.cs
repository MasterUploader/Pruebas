/**
 * ModalTarjetaComponent (OnPush)
 * -----------------------------------------------------------------------------
 * - Solución al error de template: usar un getter tipado `nombreCtrl` en lugar
 *   de `form.get('nombre')` para enlazar un FormControl<string> real.
 * - Mantiene validaciones:
 *     · requerido
 *     · al menos 2 palabras
 *     · solo MAYÚSCULAS y espacios
 *     · máximo 40 caracteres, en 2 líneas de 20 sin cortar palabras
 * - Emite `nombreCambiado` mientras se escribe (para reflejar en la grilla).
 * - Snackbar de éxito al imprimir; si inválido, muestra errores + snackbar.
 * -----------------------------------------------------------------------------
 */

import {
  Component, Inject, ChangeDetectionStrategy, ChangeDetectorRef,
  EventEmitter, Output
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

// Modelo mínimo que consume el modal
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
    MatFormFieldModule, MatInputModule, MatButtonModule, MatSnackBarModule
  ],
  templateUrl: './modal-tarjeta.component.html',
  styleUrl: './modal-tarjeta.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ModalTarjetaComponent {

  /** Form reactivo tipado. */
  public form!: FormGroup<{ nombre: FormControl<string> }>;

  /** Límite total y por línea. */
  private readonly MAX_NAME_LEN = 40;
  private readonly MAX_LINE = 20;

  /** Vista previa (dos líneas). */
  public line1 = '';
  public line2 = '';

  /** Emite el nombre (en mayúsculas) mientras el usuario escribe. */
  @Output() nombreCambiado = new EventEmitter<string>();

  constructor(
    @Inject(MAT_DIALOG_DATA) public readonly data: TarjetaLite,
    private readonly dialogRef: MatDialogRef<ModalTarjetaComponent>,
    private readonly fb: FormBuilder,
    private readonly cdr: ChangeDetectorRef,
    private readonly snackBar: MatSnackBar
  ) {
    // Inicializa el form con el nombre en MAYÚSCULAS
    const inicio = (data?.nombre ?? '').toUpperCase();
    this.form = this.fb.group({
      nombre: this.fb.control(inicio, {
        nonNullable: true,
        validators: [
          Validators.required,
          this.minTwoWords(),
          Validators.maxLength(this.MAX_NAME_LEN),
          Validators.pattern(/^[A-ZÑÁÉÍÓÚÜ\s]+$/) // solo mayúsculas + espacios
        ]
      })
    });

    // Suscripción a cambios para emitir al padre y recalcular las 2 líneas
    this.nombreCtrl.valueChanges.subscribe(v => {
      const val = (v ?? '').toUpperCase();
      this.nombreCambiado.emit(val);
      const { line1, line2 } = this.computeTwoLines(val);
      this.line1 = line1; this.line2 = line2;
      this.cdr.markForCheck();
    });

    // Cálculo inicial de las 2 líneas
    const init = this.computeTwoLines(inicio);
    this.line1 = init.line1; this.line2 = init.line2;
  }

  // ────────────────────────────────────────────────────────────────────────────
  //  Getter TIPADO para evitar el error en el template
  //  (Angular espera FormControl en [formControl], no AbstractControl)
  // ────────────────────────────────────────────────────────────────────────────
  get nombreCtrl(): FormControl<string> {
    return this.form.controls.nombre;
  }

  // ────────────────────────────────────────────────────────────────────────────
  //  Validadores y helpers
  // ────────────────────────────────────────────────────────────────────────────

  /** Valida al menos dos palabras; si está vacío, 'required' se encarga. */
  private minTwoWords() {
    return (control: AbstractControl): ValidationErrors | null => {
      const raw = String(control.value ?? '').trim();
      if (!raw) return null;
      const words = raw.split(/\s+/).filter(Boolean);
      return words.length >= 2 ? null : { twoWords: true };
    };
  }

  /** Calcula 2 líneas (máx. 20 c/u) sin cortar palabras; si no cabe, prioriza no cortar. */
  private computeTwoLines(full: string): { line1: string; line2: string } {
    const tokens = (full ?? '').split(' ').filter(Boolean);
    let l1 = '', l2 = '';
    for (const t of tokens) {
      if (!l1.length || (l1.length + 1 + t.length) <= this.MAX_LINE) {
        l1 = l1 ? `${l1} ${t}` : t;
      } else if (!l2.length || (l2.length + 1 + t.length) <= this.MAX_LINE) {
        l2 = l2 ? `${l2} ${t}` : t;
      } else {
        // Si tampoco cabe en la 2da, se ignora (regla original)
        break;
      }
    }
    return { line1: l1, line2: l2 };
  }

  // ────────────────────────────────────────────────────────────────────────────
  //  Acciones
  // ────────────────────────────────────────────────────────────────────────────

  /** Cierra el modal sin cambios. */
  public cerrarModal(): void {
    this.dialogRef.close();
  }

  /**
   * Imprimir:
   * - Si el formulario es inválido: NO imprime, muestra mat-error + snackbar.
   * - Si es válido: calcula líneas, `window.print()`, snackbar de éxito y cierra.
   */
  public imprimirTarjeta(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.snackBar.open('No puedes imprimir: corrige los errores del nombre.', 'Cerrar', {
        duration: 4000, verticalPosition: 'top', horizontalPosition: 'center'
      });
      return;
    }

    const nombre = this.nombreCtrl.value.toUpperCase();
    const { line1, line2 } = this.computeTwoLines(nombre);
    this.line1 = line1; this.line2 = line2;
    this.cdr.markForCheck();

    // Lógica de impresión (se mantiene)
    window.print();

    // ✅ Snackbar de éxito
    this.snackBar.open('Impresión enviada a la impresora.', 'Cerrar', {
      duration: 3500, verticalPosition: 'top', horizontalPosition: 'center'
    });

    // Cerrar modal (flujo actual)
    this.dialogRef.close({ printed: true });
  }
}


<!--
  Usamos [formControl]="nombreCtrl" (FormControl<string> tipado)
  para evitar el error "AbstractControl<...> no es asignable a FormControl".
-->
<div class="modal-wrapper">
  <h3>Impresión de tarjeta</h3>

  <mat-form-field appearance="fill" class="w-100">
    <mat-label>Nombre en tarjeta</mat-label>

    <!-- ⬇️ Enlace correcto al FormControl tipado -->
    <input matInput [formControl]="nombreCtrl" placeholder="NOMBRE EN TARJETA" />

    <!-- Mensajes de error -->
    <mat-error *ngIf="nombreCtrl.hasError('required')">
      El nombre es obligatorio.
    </mat-error>
    <mat-error *ngIf="nombreCtrl.hasError('twoWords')">
      Ingresa al menos dos nombres.
    </mat-error>
    <mat-error *ngIf="nombreCtrl.hasError('pattern')">
      Solo letras y espacios en MAYÚSCULAS.
    </mat-error>
    <mat-error *ngIf="nombreCtrl.hasError('maxlength')">
      Máximo 40 caracteres.
    </mat-error>
  </mat-form-field>

  <!-- Vista previa de líneas (opcional) -->
  <div class="preview">
    <div>{{ line1 }}</div>
    <div>{{ line2 }}</div>
  </div>

  <!-- Acciones -->
  <div class="actions">
    <button mat-button color="primary" (click)="imprimirTarjeta()">Imprimir</button>
    <button mat-button color="warn" (click)="cerrarModal()">Cerrar</button>
  </div>
</div>
