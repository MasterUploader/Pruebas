import { Component, Input, Output, OnInit, Inject, EventEmitter, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { MatInputModule } from '@angular/material/input';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, ValidatorFn, AbstractControl, ValidationErrors } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { Tarjeta } from '../../../../core/models/tarjeta.model';
import { ImpresionService } from '../../../../core/services/impresion.service';
import { TarjetaService } from '../../../../core/services/tarjeta.service';
import { AuthService } from '../../../../core/services/auth.service';
import { MaskAccountNumberPipe } from '../../../../shared/pipes/mask-account-number.pipe';
import { Subscription, take } from 'rxjs';

@Component({
  selector: 'app-modal-tarjeta',
  imports: [MatDialogModule, MatInputModule, ReactiveFormsModule, MaskAccountNumberPipe],
  templateUrl: './modal-tarjeta.component.html',
  styleUrl: './modal-tarjeta.component.css'
})
export class ModalTarjetaComponent implements OnInit, OnDestroy {

  private subscription: Subscription = new Subscription();
  private imprime = false;

  @Output() nombreCambiado = new EventEmitter<string>();
  @Input() datosTarjeta: Tarjeta = {
    nombre: '',
    numero: '',
    fechaEmision: '',
    fechaVencimiento: '',
    motivo: '',
    numeroCuenta: ''
  };

  // Solo un FormGroup (nombre). El diseño es fijo: 2 filas
  form!: FormGroup;

  // Para la vista (línea 1 y 2)
  nombres: string = '';
  apellidos: string = '';
  usuarioICBS: string = '';
  nombreMandar: string = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private snackBar: MatSnackBar,
    public dialogRef: MatDialogRef<ModalTarjetaComponent>,
    private impresionService: ImpresionService,
    private tarjetaService: TarjetaService,
    private cdr: ChangeDetectorRef,
    @Inject(MAT_DIALOG_DATA) public tarjeta: Tarjeta
  ) { }

  ngOnInit(): void {
    // Estado de sesión
    this.subscription.add(
      this.authService.sessionActive$.subscribe(isActive => {
        if (isActive) {
          this.usuarioICBS = this.authService.currentUserValue?.activeDirectoryData.usuarioICBS ?? '';
          this.actualizarNombre((this.tarjeta?.nombre ?? '').toUpperCase());
          this.cdr.detectChanges();
          this.cdr.markForCheck();
        } else {
          this.authService.logout();
        }
      })
    );

    // FormGroup con reglas:
    // - required
    // - solo letras/espacios en MAYÚSCULAS
    // - máximo 40
    // - al menos dos palabras (dos nombres)
    this.form = this.fb.group({
      nombre: [
        (this.tarjeta?.nombre ?? '').toUpperCase(),
        [
          Validators.required,
          Validators.pattern(/^[A-ZÑ ]+$/),
          Validators.maxLength(40),
          this.minTwoWords()
        ]
      ]
    });

    // Mantener MAYÚSCULAS, modelo y vista sincronizados
    const nombreCtrl = this.form.get('nombre')!;
    this.subscription.add(
      nombreCtrl.valueChanges.subscribe((v: string) => {
        const up = (v ?? '').toUpperCase();
        if (v !== up) {
          nombreCtrl.setValue(up, { emitEvent: false });
        }
        this.tarjeta.nombre = this.normalizarNombre(up);
        this.actualizarNombre(this.tarjeta.nombre);
      })
    );

    // Primera actualización
    const inicial = (this.tarjeta?.nombre ?? '').toUpperCase();
    this.tarjeta.nombre = this.normalizarNombre(inicial);
    this.actualizarNombre(this.tarjeta.nombre);
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  // ====== Validadores y helpers ======

  /** Valida que existan al menos dos palabras (separadas por espacio). */
  private minTwoWords(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const raw = (control.value ?? '').toString().trim().replace(/\s+/g, ' ');
      const cleaned = this.normalizarNombre(raw);
      const words = cleaned.split(' ').filter(w => w.length > 0);
      return words.length >= 2 ? null : { twoWords: true };
    };
  }

  /** Deja solo letras/espacios, colapsa espacios y recorta. */
  private normalizarNombre(nombre: string): string {
    let out = (nombre ?? '').toUpperCase().replace(/[^A-ZÑ\s]/g, '');
    out = out.replace(/\s+/g, ' ').trim();
    return out;
  }

  /** Calcula 2 líneas (máx 20 c/u) sin cortar palabras. */
  private computeTwoLines(full: string): { line1: string; line2: string } {
    const MAX = 20;
    const tokens = (full ?? '').split(' ').filter(Boolean);

    let line1 = '';
    let line2 = '';

    for (const t of tokens) {
      // Intenta agregar a línea 1
      if (line1.length === 0 ? t.length <= MAX : (line1.length + 1 + t.length) <= MAX) {
        line1 = line1.length ? `${line1} ${t}` : t;
        continue;
      }
      // Intenta agregar a línea 2
      if (line2.length === 0 ? t.length <= MAX : (line2.length + 1 + t.length) <= MAX) {
        line2 = line2.length ? `${line2} ${t}` : t;
        continue;
      }
      // Si ya no cabe (caso raro por maxLength 40), lo agregamos a la 2 (puede exceder)
      line2 = line2.length ? `${line2} ${t}` : t;
    }

    return { line1, line2 };
  }

  private actualizarNombre(nombre: string) {
    const limpio = this.normalizarNombre(nombre);
    const { line1, line2 } = this.computeTwoLines(limpio);
    this.nombres = line1;
    this.apellidos = line2;
    this.emitirNombreCambiado();
  }

  private emitirNombreCambiado(): void {
    this.nombreMandar = (this.tarjeta?.nombre ?? '').toUpperCase();
    this.nombreCambiado.emit(this.nombreMandar);
  }

  // ====== Acción principal ======

  imprimir(datosParaImprimir: Tarjeta): void {
    const nombreCtrl = this.form.get('nombre')!;
    if (nombreCtrl.invalid) {
      nombreCtrl.markAsTouched();
      nombreCtrl.updateValueAndValidity();

      const msg =
        nombreCtrl.hasError('required')  ? 'No puedes imprimir porque el nombre está vacío.' :
        nombreCtrl.hasError('twoWords')  ? 'Debe ingresar al menos dos nombres.' :
        nombreCtrl.hasError('maxlength') ? 'El nombre no puede exceder 40 caracteres.' :
        nombreCtrl.hasError('pattern')   ? 'Solo se permiten letras y espacios en mayúsculas.' :
        'El nombre no es válido.';

      this.snackBar.open(msg, 'Cerrar', { duration: 3500 });
      return;
    }

    // Validación de backend y flujo de impresión/registro
    this.subscription.add(
      this.authService.sessionActive$.pipe(take(1)).subscribe(isActive => {
        if (!isActive) {
          this.authService.logout();
          return;
        }

        this.tarjetaService.validaImpresion(this.tarjeta.numero).pipe(take(1)).subscribe({
          next: (respuesta) => {
            this.imprime = !!respuesta.imprime;

            if (!this.imprime) {
              // Siempre 2 filas -> false (en tu ImpresionService: true = 1 fila, false = 2 filas)
              const tipoDiseno = false;

              const ok = this.impresionService.imprimirTarjeta(datosParaImprimir, tipoDiseno);
              if (ok) {
                this.tarjetaService
                  .guardaEstadoImpresion(this.tarjeta.numero, this.usuarioICBS, (this.tarjeta.nombre ?? '').toUpperCase())
                  .pipe(take(1))
                  .subscribe({
                    next: () => {
                      this.cerrarModal();
                      window.location.reload();
                    },
                    error: (error) => console.error('Error al guardar estado de impresión', error)
                  });
              }
            } else {
              this.cerrarModal();
            }
          },
          error: (error) => console.error('Error en validaImpresion', error)
        });
      })
    );
  }

  cerrarModal(): void {
    this.dialogRef.close();
  }
}



<h1 mat-dialog-title> Detalle Tarjeta</h1>

<form [formGroup]="form">
  <div mat-dialog-content id="contenidoImprimir">
    <div class="contenedor">
      <div class="content-imagen-tarjeta">
        <!-- Siempre 2 filas -->
        <img src="/assets/Tarjeta3.PNG" alt=" tarjeta" class="imagen-tarjeta no-imprimir">
      </div>

      <!-- Siempre dos filas -->
      <div class="nombre-completo">
        <div class="nombres">
          <b>{{ nombres }}</b>
        </div>
        <div class="apellidos">
          <b>{{ apellidos }}</b>
        </div>
        <!-- Número de Cuenta -->
        <div class="cuenta"><b>{{ tarjeta.numeroCuenta | maskAccountNumber }}</b></div>
      </div>
    </div>

    <div mat-dialog-actions class="action-buttons">
      <!-- Nombre en tarjeta (reactivo) -->
      <mat-form-field appearance="fill" class="nombre-input">
        <mat-label>Nombre:</mat-label>
        <input
          placeholder="Nombre en Tarjeta"
          matInput
          formControlName="nombre"
          (input)="form.get('nombre')?.setValue((form.get('nombre')?.value || '').toUpperCase(), { emitEvent: true })"
          maxlength="40"
          autocomplete="off" />

        <mat-hint align="end">{{ (form.get('nombre')?.value?.length || 0) }}/40</mat-hint>

        @if (form.get('nombre')?.hasError('required') && form.get('nombre')?.touched) {
          <mat-error>El nombre es obligatorio.</mat-error>
        }
        @if (form.get('nombre')?.hasError('twoWords') && form.get('nombre')?.touched) {
          <mat-error>Debe ingresar al menos dos nombres.</mat-error>
        }
        @if (form.get('nombre')?.hasError('maxlength') && form.get('nombre')?.touched) {
          <mat-error>El nombre no puede exceder 40 caracteres.</mat-error>
        }
        @if (form.get('nombre')?.hasError('pattern') && form.get('nombre')?.touched) {
          <mat-error>Solo letras y espacios en mayúsculas.</mat-error>
        }
      </mat-form-field>

      <!-- Botones -->
      <button mat-button class="imprimir-btn" (click)="imprimir(tarjeta)">Imprimir</button>
      <span class="spacer"></span>
      <button mat-button class="cerrar-btn" (click)="cerrarModal()" [mat-dialog-close]="true">Cerrar</button>
    </div>
  </div>
</form>
