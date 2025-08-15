import { Component, Input, Output, OnInit, Inject, EventEmitter, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { MatInputModule } from '@angular/material/input';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSelectModule } from '@angular/material/select';
import { DomSanitizer } from '@angular/platform-browser';
import { Tarjeta } from '../../../../core/models/tarjeta.model';
import { ImpresionService } from '../../../../core/services/impresion.service';
import { TarjetaService } from '../../../../core/services/tarjeta.service';
import { AuthService } from '../../../../core/services/auth.service';
import { MaskAccountNumberPipe } from '../../../../shared/pipes/mask-account-number.pipe';
import { Subscription, take } from 'rxjs';

type Diseno = 'unaFila' | 'dosFilas';

@Component({
  selector: 'app-modal-tarjeta',
  imports: [MatDialogModule, MatInputModule, MatSelectModule, FormsModule, ReactiveFormsModule, MaskAccountNumberPipe],
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

  // Form reactivo
  form!: FormGroup;

  // UI/estado existente
  nombreCompleto: string = '';
  nombres: string = '';
  apellidos: string = '';
  numeroCuenta: string = '';
  usuarioICBS: string = '';
  nombreMandar: string = '';
  disenoSeleccionado: Diseno = 'dosFilas';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog,
    public dialogRef: MatDialogRef<ModalTarjetaComponent>,
    private sanitizer: DomSanitizer,
    private impresionService: ImpresionService,
    private tarjetaService: TarjetaService,
    private cdr: ChangeDetectorRef,
    @Inject(MAT_DIALOG_DATA) public tarjeta: Tarjeta
  ) {
    // Inicializa nombre y vista
    this.actualizarNombre(tarjeta.nombre ?? '');
    this.nombreCompleto = tarjeta.nombre ?? '';
  }

  ngOnInit(): void {
    // 1) Usuario actual
    this.subscription.add(
      this.authService.sessionActive$.subscribe(isActive => {
        if (isActive) {
          this.usuarioICBS = this.authService.currentUserValue?.activeDirectoryData.usuarioICBS ?? '';
          this.actualizarNombre(this.tarjeta.nombre ?? '');
          this.cdr.detectChanges();
          this.cdr.markForCheck();
        } else {
          this.authService.logout();
        }
      })
    );

    // 2) FormGroup reactivo (nombre + diseño)
    this.form = this.fb.group({
      nombre: [
        (this.tarjeta?.nombre ?? '').toUpperCase(),
        [
          Validators.required,
          Validators.pattern(/^[A-ZÑ ]+$/), // solo letras y espacios en MAYÚSCULAS
          Validators.minLength(10),
          Validators.maxLength(26)
        ]
      ],
      diseno: [this.disenoSeleccionado]
    });

    // 3) Sincroniza cambios del nombre con el modelo y la vista (en mayúsculas)
    const nombreCtrl = this.form.get('nombre')!;
    this.subscription.add(
      nombreCtrl.valueChanges.subscribe((v: string) => {
        const up = (v ?? '').toUpperCase();
        if (v !== up) {
          nombreCtrl.setValue(up, { emitEvent: false });
        }
        this.tarjeta.nombre = up;
        this.actualizarNombre(up);
      })
    );

    // 4) Sincroniza el diseño con la variable existente
    const disenoCtrl = this.form.get('diseno')!;
    this.subscription.add(
      disenoCtrl.valueChanges.subscribe((d: Diseno) => {
        this.disenoSeleccionado = d;
        this.cambiarDiseno();
      })
    );
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  // === Acción principal ===
  imprimir(datosParaImprimir: Tarjeta): void {
    const nombreCtrl = this.form.get('nombre')!;
    const nombre = String(nombreCtrl.value ?? '').trim();

    // ✅ Validación reactiva: permite presionar, pero frena si inválido
    if (nombreCtrl.invalid) {
      nombreCtrl.markAsTouched();
      nombreCtrl.updateValueAndValidity();

      const msg =
        nombreCtrl.hasError('required') ? 'No puedes imprimir porque el nombre está vacío.' :
        nombreCtrl.hasError('minlength') ? 'El nombre es demasiado corto (mínimo 10 caracteres).' :
        nombreCtrl.hasError('maxlength') ? 'El nombre es demasiado largo (máximo 26 caracteres).' :
        nombreCtrl.hasError('pattern')   ? 'Solo se permiten letras y espacios en mayúsculas.' :
        'El nombre no es válido.';

      this.snackBar.open(msg, 'Cerrar', { duration: 3500 });
      return;
    }

    // Flujo existente (validación de impresión + impresión + registro)
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
              // true = diseño de una fila
              const tipoDiseno: boolean = this.form.get('diseno')?.value === 'unaFila';

              const ok = this.impresionService.imprimirTarjeta(datosParaImprimir, tipoDiseno);
              if (ok) {
                this.tarjetaService
                  .guardaEstadoImpresion(this.tarjeta.numero, this.usuarioICBS, (this.tarjeta.nombre ?? '').toUpperCase())
                  .pipe(take(1))
                  .subscribe({
                    next: () => {
                      // Mantengo tu comportamiento original
                      this.cerrarModal();
                      window.location.reload();
                    },
                    error: (error) => console.error('Error al guardar estado de impresión', error)
                  });
              }
            } else {
              // Ya estaba marcada como impresa
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

  emitirNombreCambiado(): void {
    this.nombreMandar = (this.tarjeta?.nombre ?? '').toUpperCase();
    this.nombreCambiado.emit(this.nombreMandar);
  }

  // === Lógica de formateo/nombres ===
  dividirYActualizarNombre(nombreCompleto: string) {
    this.actualizarNombre(nombreCompleto);
    this.emitirNombreCambiado();
  }

  dividirNombreCompleto(nombreCompleto: string) {
    const cadena = (nombreCompleto ?? '').toUpperCase().trim();
    const partes = cadena.split(' ').filter(p => p.length);

    if (partes.length >= 4) {
      this.nombres = `${partes[0]} ${partes[1]}`.trim();
      this.apellidos = `${partes[2]} ${partes.slice(3).join(' ')}`.trim();

    } else if (partes.length === 3) {
      // Heurística simple de 3 partes (ajústala según tus reglas)
      this.nombres = partes[0];
      this.apellidos = `${partes[1]} ${partes[2]}`;

      // Si nombre corto, intenta pasar 2 al nombre
      if (this.nombres.length <= 16 && this.apellidos.length > 16) {
        this.nombres = `${partes[0]} ${partes[1]}`.trim();
        this.apellidos = partes[2];
      }

    } else if (partes.length === 2) {
      this.nombres = partes[0];
      this.apellidos = partes[1];

    } else {
      // 1 parte o vacío
      this.nombres = cadena;
      this.apellidos = '';
    }
  }

  actualizarNombre(nombre: string) {
    let nombreActualizado = (nombre ?? '').toUpperCase();

    // Normaliza: solo letras/espacios, colapsa espacios
    nombreActualizado = this.validarNombre(nombreActualizado);
    this.tarjeta.nombre = nombreActualizado;

    if (this.disenoSeleccionado === 'dosFilas') {
      this.dividirNombreCompleto(nombreActualizado);
    }

    this.emitirNombreCambiado();
  }

  validarNombre(nombre: string): string {
    let out = (nombre ?? '').replace(/[^A-ZÑ\s]/g, ''); // elimina no permitidos
    out = out.replace(/\s+/g, ' ').trim();              // colapsa espacios
    return out;
  }

  cambiarDiseno() {
    // Recalcula cortes con el nombre actual
    this.actualizarNombre(this.tarjeta?.nombre ?? '');
  }
}

<h1 mat-dialog-title> Detalle Tarjeta</h1>

<form [formGroup]="form">
  <div mat-dialog-content id="contenidoImprimir">
    <div class="contenedor">
      <div class="content-imagen-tarjeta">
        <img
          [src]="(form.get('diseno')?.value === 'unaFila') ? '/assets/TarjetaDiseño2.png' : '/assets/Tarjeta3.PNG'"
          alt=" tarjeta"
          class="imagen-tarjeta no-imprimir">
      </div>

      <!-- Diseño para una fila -->
      @if (form.get('diseno')?.value === 'unaFila') {
        <div class="nombre-completo">
          <div class="nombres-una-fila">
            <b>{{ tarjeta.nombre }}</b>
          </div>
          <!-- Numero de Cuenta-->
          <div class="cuenta-una-fila"><b>{{ tarjeta.numeroCuenta | maskAccountNumber }}</b></div>
        </div>
      }

      <!-- Diseño para dos filas -->
      @if (form.get('diseno')?.value === 'dosFilas') {
        <div class="nombre-completo">
          <div class="nombres">
            <b>{{ nombres }}</b>
          </div>
          <div class="apellidos">
            <b>{{ apellidos }}</b>
          </div>
          <!-- Numero de Cuenta-->
          <div class="cuenta"><b>{{ tarjeta.numeroCuenta | maskAccountNumber }}</b></div>
        </div>
      }
    </div>

    <div mat-dialog-actions class="action-buttons">

      <!-- Selector de Diseño de Tarjeta (reactivo) -->
      <mat-form-field appearance="fill" class="diseño-input">
        <mat-label>Diseño</mat-label>
        <mat-select formControlName="diseno" (selectionChange)="cambiarDiseno()">
          <mat-option value="unaFila">Diseño 1</mat-option>
          <mat-option value="dosFilas">Diseño 2</mat-option>
        </mat-select>
      </mat-form-field>

      <!-- Nombre en tarjeta (reactivo) -->
      <mat-form-field appearance="fill" class="nombre-input">
        <mat-label>Nombre:</mat-label>
        <input
          placeholder="Nombre en Tarjeta"
          matInput
          formControlName="nombre"
          (input)="form.get('nombre')?.setValue((form.get('nombre')?.value || '').toUpperCase(), { emitEvent: true })"
          maxlength="26"
          autocomplete="off" />

        <mat-hint align="end">{{ (form.get('nombre')?.value?.length || 0) }}/26</mat-hint>

        <mat-error *ngIf="form.get('nombre')?.hasError('required') && form.get('nombre')?.touched">
          El nombre es obligatorio.
        </mat-error>
        <mat-error *ngIf="form.get('nombre')?.hasError('minlength') && form.get('nombre')?.touched">
          Mínimo 10 caracteres.
        </mat-error>
        <mat-error *ngIf="form.get('nombre')?.hasError('maxlength') && form.get('nombre')?.touched">
          Máximo 26 caracteres.
        </mat-error>
        <mat-error *ngIf="form.get('nombre')?.hasError('pattern') && form.get('nombre')?.touched">
          Solo letras y espacios en mayúsculas.
        </mat-error>
      </mat-form-field>

      <!-- Botones -->
      <button mat-button class="imprimir-btn" (click)="imprimir(tarjeta)">
        Imprimir
      </button>
      <span class="spacer"></span>
      <button mat-button class="cerrar-btn" (click)="cerrarModal()" [mat-dialog-close]="true">
        Cerrar
      </button>
    </div>
  </div>
</form>


