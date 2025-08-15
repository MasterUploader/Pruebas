import { Component, Input, Output, OnInit, Inject, EventEmitter, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSelectModule } from '@angular/material/select';
import { DomSanitizer } from '@angular/platform-browser';
import { Tarjeta } from '../../../../core/models/tarjeta.model';
import { ImpresionService } from '../../../../core/services/impresion.service';
import { TarjetaService } from '../../../../core/services/tarjeta.service';
import { AuthService } from '../../../../core/services/auth.service';
import { MaskAccountNumberPipe } from '../../../../shared/pipes/mask-account-number.pipe';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-modal-tarjeta',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatInputModule,
    MatSelectModule,
    FormsModule,
    MaskAccountNumberPipe,
    MatSnackBarModule
  ],
  templateUrl: './modal-tarjeta.component.html',
  styleUrl: './modal-tarjeta.component.css'
})
export class ModalTarjetaComponent implements OnInit, OnDestroy {

  private readonly subscription: Subscription = new Subscription();
  private imprime: boolean;

  @Output() nombreCambiado = new EventEmitter<string>();

  @Input() datosTarjeta: Tarjeta = {
    nombre: '',
    numero: '',
    fechaEmision: '',
    fechaVencimiento: '',
    motivo: '',
    numeroCuenta: ''
  };

  /** Copia de nombre completo (sólo informativo) */
  nombreCompleto: string = '';
  /** Línea superior para Diseño 2 */
  nombres: string = '';
  /** Línea inferior para Diseño 2 */
  apellidos: string = '';

  numeroCuenta: string = '';
  usuarioICBS: string = '';
  nombreMandar: string = '';

  /** Diseño seleccionado: 'unaFila' (Diseño 1) o 'dosFilas' (Diseño 2) */
  disenoSeleccionado: string = 'unaFila';

  /** Máximo por fila en diseño 2 */
  maxCaracteresFila: number = 32;

  /** ---- Soporte de validación visual ---- */
  nombreError: string = '';
  nombreCharsCount: number = 0;
  nombreMaxLength: number = 26;

  constructor(
    private readonly authService: AuthService,
    private readonly dialog: MatDialog,
    public dialogRef: MatDialogRef<ModalTarjetaComponent>,
    private readonly sanitizer: DomSanitizer,
    private readonly impresionService: ImpresionService,
    private readonly tarjetaService: TarjetaService,
    private readonly cdr: ChangeDetectorRef,
    private readonly snackBar: MatSnackBar,
    @Inject(MAT_DIALOG_DATA) public tarjeta: Tarjeta
  ) {
    // Inicialización respetando el flujo actual
    this.actualizarNombre(tarjeta.nombre);
    this.nombreCompleto = tarjeta.nombre;
    this.imprime = false;

    // Inicializar contador/validación
    this.nombreCharsCount = (this.tarjeta.nombre || '').length;
    this.aplicarValidaciones(this.tarjeta.nombre);
  }

  ngOnInit(): void {
    this.subscription.add(this.authService.sessionActive$.subscribe(isActive => {
      if (isActive) {
        this.usuarioICBS = this.authService.currentUserValue?.activeDirectoryData.usuarioICBS!;

        // Recalcula nombres/apellidos al iniciar y ante cambios de sesión
        this.actualizarNombre(this.tarjeta.nombre);

        this.cdr.detectChanges();
        this.cdr.markForCheck();
      } else {
        this.authService.logout();
      }
    }));
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  /**
   * Imprimir con validación dura:
   * - Si hay error (vacío o una sola palabra), NO imprime.
   * - Muestra error en texto (ya visible) y además en SnackBar flotante.
   */
  imprimir(datosParaImprimir: Tarjeta): void {
    // Revalida justo al presionar
    this.aplicarValidaciones(this.tarjeta.nombre);

    if (this.nombreError) {
      this.mostrarNotificacionError(this.nombreError);
      return; // Bloquea la impresión
    }

    let impresionExitosa = false;

    this.subscription.add(this.authService.sessionActive$.subscribe(isActive => {
      if (isActive) {
        this.tarjetaService.validaImpresion(this.tarjeta.numero).subscribe({
          next: (respuesta) => {
            if (respuesta.imprime) {
              this.imprime = respuesta.imprime;
            } else {
              this.imprime = respuesta.imprime;
            }
          }
        });

        if (!this.imprime) {
          // true = Diseño 1 (una fila). false = Diseño 2 (dos filas)
          const tipoDiseño = this.disenoSeleccionado === 'unaFila';

          impresionExitosa = this.impresionService.imprimirTarjeta(datosParaImprimir, tipoDiseño);
          if (impresionExitosa) {
            this.tarjetaService
              .guardaEstadoImpresion(this.tarjeta.numero, this.usuarioICBS, this.tarjeta.nombre.toUpperCase())
              .subscribe({
                next: () => {
                  this.cerrarModal();
                  window.location.reload();
                },
                error: (error) => {
                  console.log('error', error);
                }
              });
          }
        } else {
          this.cerrarModal();
        }

      } else {
        this.authService.logout();
      }
    }));
  }

  cerrarModal(): void {
    this.dialogRef.close();
  }

  /** Emite el nombre (en mayúsculas) al componente padre */
  emitirNombreCambiado(): void {
    this.nombreMandar = this.tarjeta.nombre.toUpperCase();
    this.nombreCambiado.emit(this.nombreMandar);
  }

  dividirYActualizarNombre(nombreCompleto: string) {
    this.actualizarNombre(nombreCompleto);
    this.emitirNombreCambiado();
  }

  /**
   * Lógica de división para Diseño 2:
   * - 1 parte: arriba esa parte, abajo vacío
   * - 2 partes: 1 arriba, 1 abajo
   * - 3 partes: 2 arriba, 1 abajo
   * - 4+ partes: 2 arriba, resto abajo
   * - Cada fila recortada a 32 chars por seguridad
   */
  private dividirNombreCompleto(nombreCompleto: string): void {
    const cadena = (nombreCompleto || '').toUpperCase().trim();
    if (!cadena) {
      this.nombres = '';
      this.apellidos = '';
      return;
    }

    const partes = cadena.split(' ').filter(p => p.length > 0);

    if (partes.length === 1) {
      this.nombres = partes[0];
      this.apellidos = '';
    } else if (partes.length === 2) {
      this.nombres = partes[0];
      this.apellidos = partes[1];
    } else if (partes.length === 3) {
      this.nombres = `${partes[0]} ${partes[1]}`;
      this.apellidos = partes[2];
    } else {
      this.nombres = `${partes[0]} ${partes[1]}`;
      this.apellidos = partes.slice(2).join(' ');
    }

    if (this.nombres.length > this.maxCaracteresFila) {
      this.nombres = this.nombres.slice(0, this.maxCaracteresFila);
    }
    if (this.apellidos.length > this.maxCaracteresFila) {
      this.apellidos = this.apellidos.slice(0, this.maxCaracteresFila);
    }
  }

  /**
   * Actualiza el nombre (en mayúsculas), aplica validaciones existentes,
   * y si el diseño es “dosFilas”, divide en nombres/apellidos.
   * Recalcula contador + validación.
   */
  actualizarNombre(nombre: string) {
    let nombreActualizado = (nombre || '').toUpperCase();

    // Mantengo tu validación original (NO se tocan regex)
    nombreActualizado = this.validarNombre(nombreActualizado);

    this.tarjeta.nombre = nombreActualizado;

    if (this.disenoSeleccionado === 'dosFilas') {
      this.dividirNombreCompleto(nombreActualizado);
    }

    this.nombreCharsCount = nombreActualizado.length;
    this.aplicarValidaciones(nombreActualizado);

    this.emitirNombreCambiado();
  }

  /**
   * Validación de nombre existente (NO modifico tus regex).
   * - Elimina caracteres no permitidos (A-Z y espacios).
   * - Reduce múltiples espacios a uno.
   */
  validarNombre(nombre: string): string {
    let nombreValido = nombre.replace(/[^A-Z\s]/g, ''); // Eliminar caracteres no permitidos.
    nombreValido = nombreValido.replace(/\s+/g, ' ');   // Mantener un solo espacio
    return nombreValido;
  }

  prevenirNumeroCaracteres(event: KeyboardEvent) {
    const regex = /^[a-zA-Z\s]*$/;
    if (!regex.test(event.key)) {
      event.preventDefault();
    }
  }

  cambiarDiseno() {
    this.actualizarNombre(this.tarjeta.nombre);
  }

  /**
   * Valida entrada en tiempo real (flujo existente).
   * - Respeta tu regex que permite Ñ.
   * - Recalcula diseño 2, contador y validaciones para mantener vista consistente.
   */
  validarEntrada(event: Event): void {
    const input = (event.target as HTMLInputElement);
    let valor = (input.value || '').toUpperCase();

    valor = valor.replace(/[^A-ZÑ\s]/g, '').replace(/\s{2,}/g, ' ');

    input.value = valor;
    this.tarjeta.nombre = valor;

    if (this.disenoSeleccionado === 'dosFilas') {
      this.dividirNombreCompleto(valor);
    }

    this.nombreCharsCount = valor.length;
    this.aplicarValidaciones(valor);

    this.emitirNombreCambiado();
  }

  // ======== Validaciones solicitadas ========

  /**
   * Reglas:
   * 1) Vacío -> error: "El nombre no puede estar vacío."
   * 2) Una sola palabra -> error: "Debe ingresar al menos nombre y apellido (mínimo 2 palabras)."
   * *Ya NO se valida longitud mínima de 10.*
   */
  private aplicarValidaciones(valor: string): void {
    const limpio = (valor || '').trim();
    const palabras = limpio.length === 0 ? [] : limpio.split(' ').filter(p => p.length > 0);

    this.nombreError = '';

    if (limpio.length === 0) {
      this.nombreError = 'El nombre no puede estar vacío.';
      return;
    }

    if (palabras.length < 2) {
      this.nombreError = 'Debe ingresar al menos nombre y apellido (mínimo 2 palabras).';
      return;
    }
  }

  /** Muestra notificación flotante con el mensaje de error */
  private mostrarNotificacionError(mensaje: string): void {
    this.snackBar.open(mensaje, 'Cerrar', {
      duration: 3500,
      horizontalPosition: 'right',
      verticalPosition: 'top'
    });
  }
}

<h1 mat-dialog-title> Detalle Tarjeta</h1>
<div mat-dialog-content id="contenidoImprimir">
  <div class="contenedor">
    <div class="content-imagen-tarjeta">
      <img [src]="disenoSeleccionado === 'unaFila' ? '/assets/TarjetaDiseño2.png' : '/assets/Tarjeta3.PNG'"
        alt="imagen tarjeta" class="imagen-tarjeta no-imprimir">
    </div>

    <!-- Diseño para una fila (Diseño 1) -->
    <div *ngIf="disenoSeleccionado === 'unaFila'" class="nombre-completo">
      <div class="nombres-una-fila">
        <b>{{tarjeta.nombre}}</b>
      </div>

      <!-- Numero de Cuenta-->
      <div class="cuenta-una-fila"><b>{{tarjeta.numeroCuenta | maskAccountNumber}}</b></div>
    </div>

    <!-- Diseño para dos filas (Diseño 2) -->
    <div *ngIf="disenoSeleccionado === 'dosFilas'" class="nombre-completo">
      <div class="nombres">
        <b>{{nombres}}</b>
      </div>
      <div class="apellidos">
        <b>{{apellidos}}</b>
      </div>

      <!-- Numero de Cuenta-->
      <div class="cuenta"><b>{{tarjeta.numeroCuenta | maskAccountNumber}}</b></div>
    </div>
  </div>

  <div mat-dialog-actions class="action-buttons">

    <!-- Selector de Diseño -->
    <mat-form-field appearance="fill" class="diseño-input">
      <mat-label>Diseño</mat-label>
      <mat-select [(value)]="disenoSeleccionado" (selectionChange)="cambiarDiseno()">
        <mat-option value="unaFila">Diseño 1</mat-option>
        <mat-option value="dosFilas">Diseño 2</mat-option>
      </mat-select>
    </mat-form-field>

    <!-- Input nombre en tarjeta (26 caracteres siempre) -->
    <mat-form-field appearance="fill" class="nombre-input">
      <mat-label>Nombre:</mat-label>
      <input
        placeholder="Nombre en Tarjeta"
        matInput
        [(ngModel)]="tarjeta.nombre"
        (input)="validarEntrada($event)"
        (keypress)="prevenirNumeroCaracteres($event)"
        maxlength="26">
      <!-- Contador a la derecha -->
      <mat-hint align="end">{{nombreCharsCount}}/{{nombreMaxLength}}</mat-hint>
      <!-- Mensaje de error -->
      <mat-error *ngIf="nombreError">{{nombreError}}</mat-error>
    </mat-form-field>

    <!-- Botón siempre habilitado; la validación es interna en imprimir() -->
    <button mat-button class="imprimir-btn" (click)="imprimir(tarjeta)">Imprimir</button>

    <span class="spacer"></span>

    <button mat-button class="cerrar-btn" (click)="cerrarModal()" [mat-dialog-close]="true">
      Cerrar
    </button>
  </div>
</div>


/* Modal */
.modal {
  display: none;
  position: fixed;
  z-index: 1;
  left: 0;
  top: 0;
  width: 100%;
  height: 100%;
  overflow: auto;
  background-position: center;
}

/* Contenido del modal */
.modal-content {
  background-color: #fefefe;
  margin: 15% auto;
  padding: 20px;
  border: 1px solid #888;
  width: 400px;
  height: 600px;
  background-size: 87404194px 321.25988299px;
  background-repeat: no-repeat;
  background-size: cover;
}

.contenedor{
  position: relative;
  display: flex;
  justify-content: center;
  align-items: center;
}

@media print {
  .no-imprimir{
    display: none;
  }
}

.content-imagen-tarjeta {
  width: 207.87404194px;
  height: 321.25988299px;
  display: flex;
  align-content: center;
  justify-content: center;
  align-items: center;
  position: relative;
}

.imagen-tarjeta {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

/* Diseño 1 - una fila */
.nombres-una-fila {
  position: absolute;
  top: 60%;
  left:50%;
  font-size: 6pt;
  color: white;
  text-align: center;
  max-width: 90%;
  transform: translate(-50%);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.cuenta-una-fila{
  position: absolute;
  top: 67%;
  left: 50%;
  transform: translate(-50%);
  font-size: 7pt;
  text-align: center;
  max-width: 80%;
  color: white;
}

/* Diseño 2 - dos filas */
.nombres {
  position: absolute;
  top: 170px;
  font-size: 6pt;
  color: black;
  right: 190px;
  justify-content: end;
}

.apellidos {
  position: absolute;
  top: 185px;
  font-size: 6pt;
  color: black;
  right: 190px;
  justify-content: end;
}

.cuenta{
  position: absolute;
  top: 210px;
  font-size: 7pt;
  right: 220px;
  color: white;
}

.modal-footer {
  padding: 10px;
  display: flex;
  flex-direction: column;
  justify-content: space-around;
  height: 100px;
}

.mat-dialog-actions {
  align-items: center;
  justify-content: space-between;
  display: flex;
  flex-wrap: wrap;
}

.action-buttons .flex-container {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
}

.nombre-input {
  flex-grow: 1;
  margin-right: 20px;
  width: 100%;
  text-transform: uppercase;
}

.spacer {
  flex: 1;
}

.imprimir-btn {
  background-color: #4CAF50;
  color: white;
}

.imprimir-btn:hover {
  background-color: #45a049;
}

.cerrar-btn {
  background-color: #f44336;
  color: white;
}

.cerrar-btn:hover {
  background-color: #da190b;
}



