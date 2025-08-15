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

    <!--Selector de Diseño de Tarjeta-->
    <mat-form-field appearance="fill" class="diseño-input">
      <mat-label>Diseño</mat-label>
      <mat-select [(value)]="disenoSeleccionado" (selectionChange)="cambiarDiseno()">
        <!--Diseño 1 Primera Tarjeta Vertical Abril 2024-->
        <mat-option value="unaFila">Diseño 1</mat-option>
        <!-- Diseño 2 Segunda Tarjeta Vertical Noviembre de 2024-->
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
    </mat-form-field>

    <button mat-button class="imprimir-btn" (click)="imprimir(tarjeta)" [disabled]="!nombreValidoParaImprimir">
      Imprimir
    </button>
    <span class="spacer"></span>
    <button mat-button class="cerrar-btn" (click)="cerrarModal()" [mat-dialog-close]="true">Cerrar</button>
  </div>
</div>


/* Modal */
/* Estilo base para el fondo oscuro del modal */
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

/* Estilo para la caja de contenido del modal */
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
  text-transform: uppercase; /* Se mantiene */
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
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-modal-tarjeta',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatInputModule, MatSelectModule, FormsModule, MaskAccountNumberPipe],
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

  /**
   * Máximo de caracteres por fila cuando el nombre se divide en dos líneas.
   * Requerimiento: 32.
   * NOTA: El nombre total viaja de 26 caracteres siempre, por lo que no debería sobrepasarlo,
   * pero lo aplicamos por robustez y futuras variaciones.
   */
  maxCaracteresFila: number = 32;

  /** Habilita o no el botón de imprimir */
  nombreValidoParaImprimir = false;

  constructor(
    private readonly authService: AuthService,
    private readonly dialog: MatDialog,
    public dialogRef: MatDialogRef<ModalTarjetaComponent>,
    private readonly sanitizer: DomSanitizer,
    private readonly impresionService: ImpresionService,
    private readonly tarjetaService: TarjetaService,
    private readonly cdr: ChangeDetectorRef,
    @Inject(MAT_DIALOG_DATA) public tarjeta: Tarjeta
  ) {
    // Inicializamos respetando el flujo actual
    this.actualizarNombre(tarjeta.nombre);
    this.nombreCompleto = tarjeta.nombre;
    this.imprime = false;
  }

  ngOnInit(): void {
    this.subscription.add(this.authService.sessionActive$.subscribe(isActive => {
      if (isActive) {
        this.usuarioICBS = this.authService.currentUserValue?.activeDirectoryData.usuarioICBS!;

        // Recalcula nombres/apellidos al iniciar y ante cambios de sesión
        this.actualizarNombre(this.tarjeta.nombre);

        // Mantén tu detección manual actual
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
   * Imprime la tarjeta utilizando el servicio actual de impresión.
   * Se respeta el comportamiento existente, únicamente se utiliza el flag de diseño (1 o 2 filas).
   */
  imprimir(datosParaImprimir: Tarjeta): void {
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

  /** Llama al flujo completo de actualización + emisión */
  dividirYActualizarNombre(nombreCompleto: string) {
    this.actualizarNombre(nombreCompleto);
    this.emitirNombreCambiado();
  }

  /**
   * Lógica de división para “Diseño 2”.
   * Reglas:
   * - Si hay 2 partes (nombre y apellido): 1 arriba (nombre), 1 abajo (apellido).
   * - Si hay 3 partes: 2 arriba, 1 abajo.
   * - Si hay 1 parte: arriba esa parte, abajo vacío.
   * - Si hay 4 o más: dos arriba y el resto abajo (unido por espacio).
   * - Cada fila se recorta a `maxCaracteresFila` (32) por seguridad, sin romper el flujo actual.
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
      // Un solo token -> arriba; abajo vacío
      this.nombres = partes[0];
      this.apellidos = '';
    } else if (partes.length === 2) {
      // Dos tokens -> “mitad y mitad”: 1 arriba, 1 abajo
      this.nombres = partes[0];
      this.apellidos = partes[1];
    } else if (partes.length === 3) {
      // Tres tokens -> 2 arriba, 1 abajo
      this.nombres = `${partes[0]} ${partes[1]}`;
      this.apellidos = partes[2];
    } else {
      // Cuatro o más -> 2 arriba, resto abajo (unidos)
      this.nombres = `${partes[0]} ${partes[1]}`;
      this.apellidos = partes.slice(2).join(' ');
    }

    // Enforce máximo 32 por fila sin alterar tu regex ni el maxlength global (26)
    if (this.nombres.length > this.maxCaracteresFila) {
      this.nombres = this.nombres.slice(0, this.maxCaracteresFila);
    }
    if (this.apellidos.length > this.maxCaracteresFila) {
      this.apellidos = this.apellidos.slice(0, this.maxCaracteresFila);
    }
  }

  /**
   * Actualiza el nombre (en mayúsculas), aplica validaciones existentes
   * y, si el diseño es “dosFilas”, divide en nombres/apellidos.
   */
  actualizarNombre(nombre: string) {
    let nombreActualizado = (nombre || '').toUpperCase();

    // Mantengo tu validación original (no se tocan regex)
    nombreActualizado = this.validarNombre(nombreActualizado);

    // Persistimos en el modelo de tarjeta
    this.tarjeta.nombre = nombreActualizado;

    // Si el diseño es de dos filas, calculamos las líneas
    if (this.disenoSeleccionado === 'dosFilas') {
      this.dividirNombreCompleto(nombreActualizado);
    }

    // Emitimos al padre (lo ya existente)
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

  /**
   * Previene teclas fuera de [a-zA-Z y espacio] en keypress (flujo existente).
   */
  prevenirNumeroCaracteres(event: KeyboardEvent) {
    const regex = /^[a-zA-Z\s]*$/;
    if (!regex.test(event.key)) {
      event.preventDefault();
    }
  }

  /**
   * Cambio de diseño desde el mat-select.
   * Recalcula división si corresponde.
   */
  cambiarDiseno() {
    this.actualizarNombre(this.tarjeta.nombre);
  }

  /**
   * Valida entrada en tiempo real (flujo existente).
   * - Se respeta tu regex actual que permite Ñ.
   * - Recalcula diseño 2 para mantener vista consistente.
   */
  validarEntrada(event: Event): void {
    const input = (event.target as HTMLInputElement);
    let valor = (input.value || '').toUpperCase();

    // Mantengo exactamente tu regex y normalización de espacios
    valor = valor.replace(/[^A-ZÑ\s]/g, '').replace(/\s{2,}/g, ' ');

    input.value = valor;
    this.tarjeta.nombre = valor;

    // Habilitación de imprimir: dejamos tu lógica actual (>= 10)
    this.nombreValidoParaImprimir = valor.trim().length >= 10;

    // Si el diseño es de dos filas, recalculamos inmediatamente para reflejar el cambio
    if (this.disenoSeleccionado === 'dosFilas') {
      this.dividirNombreCompleto(valor);
    }

    // Emitimos el cambio (mantener contrato actual)
    this.emitirNombreCambiado();
  }
}

