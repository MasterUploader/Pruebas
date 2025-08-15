/**
 * ModalTarjetaComponent (OnPush)
 * - SIN CAMBIAR TU DISEÑO: solo arregla el binding del control y mantiene
 *   las validaciones + snackbars.
 * - Getter `nombreCtrl` tipado para usarlo en el template (evita error
 *   de AbstractControl en HTML).
 * - Emite `nombreCambiado` mientras se escribe (la grilla se actualiza en vivo).
 * - Snackbar de éxito al imprimir; si hay errores, muestra snackbar de validación.
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

  /** Form reactivo tipado (no cambia tu estructura visual). */
  public form!: FormGroup<{ nombre: FormControl<string> }>;

  private readonly MAX_NAME_LEN = 40;
  private readonly MAX_LINE = 20;

  public line1 = '';
  public line2 = '';

  /** Notifica al padre mientras escribes. */
  @Output() nombreCambiado = new EventEmitter<string>();

  constructor(
    @Inject(MAT_DIALOG_DATA) public readonly data: TarjetaLite,
    private readonly dialogRef: MatDialogRef<ModalTarjetaComponent>,
    private readonly fb: FormBuilder,
    private readonly cdr: ChangeDetectorRef,
    private readonly snackBar: MatSnackBar
  ) {
    const inicio = (data?.nombre ?? '').toUpperCase();

    this.form = this.fb.group({
      nombre: this.fb.control(inicio, {
        nonNullable: true,
        validators: [
          Validators.required,
          this.minTwoWords(),
          Validators.maxLength(this.MAX_NAME_LEN),
          Validators.pattern(/^[A-ZÑ\s]+$/) // solo mayúsculas y espacios
        ]
      })
    });

    // Recalcular líneas y emitir en cada cambio (sin alterar tu HTML)
    this.nombreCtrl.valueChanges.subscribe(v => {
      const val = (v ?? '').toUpperCase();
      this.nombreCambiado.emit(val);
      const { line1, line2 } = this.computeTwoLines(val);
      this.line1 = line1; this.line2 = line2;
      this.cdr.markForCheck();
    });

    const init = this.computeTwoLines(inicio);
    this.line1 = init.line1; this.line2 = init.line2;
  }

  /** ⬇️ Getter tipado para usar en el template (evita error AbstractControl). */
  get nombreCtrl(): FormControl<string> {
    return this.form.controls.nombre;
  }

  // ===== Validadores y helpers =====
  private minTwoWords() {
    return (control: AbstractControl): ValidationErrors | null => {
      const raw = String(control.value ?? '').trim();
      if (!raw) return null; // 'required' lo maneja
      const words = raw.split(/\s+/).filter(Boolean);
      return words.length >= 2 ? null : { twoWords: true };
    };
  }

  private computeTwoLines(full: string): { line1: string; line2: string } {
    const tokens = (full ?? '').split(' ').filter(Boolean);
    let l1 = '', l2 = '';
    for (const t of tokens) {
      if (!l1.length || (l1.length + 1 + t.length) <= this.MAX_LINE) {
        l1 = l1 ? `${l1} ${t}` : t;
      } else if (!l2.length || (l2.length + 1 + t.length) <= this.MAX_LINE) {
        l2 = l2 ? `${l2} ${t}` : t;
      } else {
        break; // no cortar palabras (misma regla que tenías)
      }
    }
    return { line1: l1, line2: l2 };
  }

  // ===== Acciones (sin cambiar tu UX) =====
  public cerrarModal(): void {
    this.dialogRef.close();
  }

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

    // Mantengo tu lógica de impresión
    window.print();

    // Éxito
    this.snackBar.open('Impresión enviada a la impresora.', 'Cerrar', {
      duration: 3500, verticalPosition: 'top', horizontalPosition: 'center'
    });

    this.dialogRef.close({ printed: true });
  }
}


<!-- Modal — HTML completo (mantiene tu diseño, solo cambia el binding al control tipado `nombreCtrl`) -->
<div class="modal-wrapper">
  <h3 class="modal-title">Impresión de tarjeta</h3>

  <!-- Vista previa (dos líneas de hasta 20 caracteres cada una) -->
  <div class="preview">
    <div class="preview-line">{{ line1 }}</div>
    <div class="preview-line">{{ line2 }}</div>
  </div>

  <!-- Campo de nombre (usa [formControl]="nombreCtrl" para evitar el error de AbstractControl) -->
  <mat-form-field appearance="fill" class="w-100">
    <mat-label>Nombre en tarjeta</mat-label>
    <input
      matInput
      [formControl]="nombreCtrl"
      placeholder="NOMBRE EN TARJETA"
      autocomplete="off"
      cdkFocusInitial
    />

    <!-- Hints / Contadores -->
    <mat-hint align="start">Máx. 40 caracteres. Se divide en 2 líneas (20 / 20).</mat-hint>
    <mat-hint align="end">{{ (nombreCtrl.value?.length || 0) }}/40</mat-hint>

    <!-- Errores -->
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

  <!-- Acciones -->
  <div class="actions">
    <button mat-flat-button color="primary" type="button" (click)="imprimirTarjeta()">
      Imprimir
    </button>
    <button mat-button color="warn" type="button" (click)="cerrarModal()">
      Cerrar
    </button>
  </div>
</div>







