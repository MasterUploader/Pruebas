<h1 mat-dialog-title> Detalle Tarjeta</h1>
<div mat-dialog-content id="contenidoImprimir">
  <div class="contenedor">
    <!-- La imagen es el contenedor de referencia (position: relative) -->
    <div class="content-imagen-tarjeta">
      <img
        [src]="disenoSeleccionado === 'unaFila' ? '/assets/TarjetaDiseño2.png' : '/assets/Tarjeta3.PNG'"
        alt="imagen tarjeta"
        class="imagen-tarjeta no-imprimir">

      <!-- PRELOAD de la otra imagen para evitar parpadeo -->
      <img src="/assets/TarjetaDiseño2.png" class="preload" alt="">
      <img src="/assets/Tarjeta3.PNG" class="preload" alt="">

      <!-- ====== STAGE: ambos diseños apilados (cross-fade por CSS) ====== -->
      <div class="design-stage">
        <!-- Diseño 1 -->
        <div class="design-layer" [class.active]="disenoSeleccionado === 'unaFila'">
          <div class="nombres-una-fila"><b>{{tarjeta.nombre}}</b></div>
          <div class="cuenta-una-fila"><b>{{tarjeta.numeroCuenta | maskAccountNumber}}</b></div>
        </div>

        <!-- Diseño 2 -->
        <div class="design-layer" [class.active]="disenoSeleccionado === 'dosFilas'">
          <div class="nombres"><b>{{nombres}}</b></div>
          <div class="apellidos"><b>{{apellidos}}</b></div>
          <div class="cuenta"><b>{{tarjeta.numeroCuenta | maskAccountNumber}}</b></div>
        </div>
      </div>
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

    <!-- Campo Nombre con matcher en el INPUT -->
    <mat-form-field appearance="fill" class="nombre-input">
      <mat-label>Nombre:</mat-label>
      <input
        placeholder="Nombre en Tarjeta"
        matInput
        [(ngModel)]="tarjeta.nombre"
        (input)="validarEntrada($event)"
        (keypress)="prevenirNumeroCaracteres($event)"
        maxlength="26"
        required
        [errorStateMatcher]="nombreErrorMatcher">
      <!-- El hint se oculta cuando hay mat-error; lo mostramos solo si NO hay error -->
      <mat-hint align="end" *ngIf="!nombreError">{{nombreCharsCount}}/{{nombreMaxLength}}</mat-hint>
      <mat-error *ngIf="nombreError">{{ nombreError }}</mat-error>
    </mat-form-field>

    <!-- Contador externo cuando hay error (para no perder 13/26) -->
    <div class="contador-externo" *ngIf="nombreError">
      {{nombreCharsCount}}/{{nombreMaxLength}}
    </div>

    <!-- Botón siempre habilitado; validación se hace al presionar -->
    <button mat-button class="imprimir-btn" (click)="imprimir(tarjeta)">Imprimir</button>

    <span class="spacer"></span>
    <button mat-button class="cerrar-btn" (click)="cerrarModal()" [mat-dialog-close]="true">Cerrar</button>
  </div>
</div>

import { Component, Input, Output, OnInit, Inject, EventEmitter, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ErrorStateMatcher } from '@angular/material/core';
import { FormControl, FormGroupDirective } from '@angular/forms';

import { DomSanitizer } from '@angular/platform-browser';
import { Subscription } from 'rxjs';

import { Tarjeta } from '../../../../core/models/tarjeta.model';
import { ImpresionService } from '../../../../core/services/impresion.service';
import { TarjetaService } from '../../../../core/services/tarjeta.service';
import { AuthService } from '../../../../core/services/auth.service';
import { MaskAccountNumberPipe } from '../../../../shared/pipes/mask-account-number.pipe';

/**
 * ErrorStateMatcher personalizado:
 * Muestra mat-error cuando hay error externo (nombreError) o validación normal.
 */
class NombreErrorStateMatcher implements ErrorStateMatcher {
  constructor(private hasExternalError: () => boolean) {}
  isErrorState(control: FormControl | null, form: FormGroupDirective | NgForm | null): boolean {
    const submitted = !!form && form.submitted;
    const invalidStd = !!control && control.invalid && (control.touched || control.dirty || submitted);
    return this.hasExternalError() || invalidStd;
  }
}

@Component({
  selector: 'app-modal-tarjeta',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatSelectModule,
    MatFormFieldModule,
    MatInputModule,
    MatSnackBarModule,
    MaskAccountNumberPipe
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

  nombreCompleto: string = '';
  nombres: string = '';
  apellidos: string = '';

  numeroCuenta: string = '';
  usuarioICBS: string = '';
  nombreMandar: string = '';

  disenoSeleccionado: string = 'unaFila';

  // Máx 16 por fila en diseño 2 (lo cortamos en dividirNombreCompleto)
  maxCaracteresFila: number = 16;

  // Validación / UI
  nombreError: string = '';
  nombreCharsCount: number = 0;
  nombreMaxLength: number = 26;

  // Matcher para mat-error
  nombreErrorMatcher = new NombreErrorStateMatcher(() => this.nombreError.trim().length > 0);

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
    // Inicialización respetando tu flujo
    this.actualizarNombre(tarjeta.nombre);
    this.nombreCompleto = tarjeta.nombre;
    this.imprime = false;

    this.nombreCharsCount = (this.tarjeta.nombre || '').length;
    this.aplicarValidaciones(this.tarjeta.nombre);
  }

  ngOnInit(): void {
    // Precarga de imágenes para evitar parpadeos al cambiar diseño
    const img1 = new Image(); img1.src = '/assets/TarjetaDiseño2.png';
    const img2 = new Image(); img2.src = '/assets/Tarjeta3.PNG';

    this.subscription.add(this.authService.sessionActive$.subscribe(isActive => {
      if (isActive) {
        this.usuarioICBS = this.authService.currentUserValue?.activeDirectoryData.usuarioICBS!;
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
          next: (respuesta) => { this.imprime = respuesta.imprime; }
        });

        if (!this.imprime) {
          const tipoDiseño = this.disenoSeleccionado === 'unaFila';
          impresionExitosa = this.impresionService.imprimirTarjeta(datosParaImprimir, tipoDiseño);
          if (impresionExitosa) {
            this.tarjetaService
              .guardaEstadoImpresion(this.tarjeta.numero, this.usuarioICBS, this.tarjeta.nombre.toUpperCase())
              .subscribe({
                next: () => { this.cerrarModal(); window.location.reload(); },
                error: (error) => { console.log('error', error); }
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

  cerrarModal(): void { this.dialogRef.close(); }

  emitirNombreCambiado(): void {
    this.nombreMandar = this.tarjeta.nombre.toUpperCase();
    this.nombreCambiado.emit(this.nombreMandar);
  }

  dividirYActualizarNombre(nombreCompleto: string) {
    this.actualizarNombre(nombreCompleto);
    this.emitirNombreCambiado();
  }

  private dividirNombreCompleto(nombreCompleto: string): void {
    const cadena = (nombreCompleto || '').toUpperCase().trim();
    if (!cadena) { this.nombres = ''; this.apellidos = ''; return; }

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

    if (this.nombres.length > this.maxCaracteresFila) this.nombres = this.nombres.slice(0, this.maxCaracteresFila);
    if (this.apellidos.length > this.maxCaracteresFila) this.apellidos = this.apellidos.slice(0, this.maxCaracteresFila);
  }

  actualizarNombre(nombre: string) {
    let nombreActualizado = (nombre || '').toUpperCase();

    // Validación original (no tocamos tu regex base)
    nombreActualizado = this.validarNombre(nombreActualizado);

    this.tarjeta.nombre = nombreActualizado;

    if (this.disenoSeleccionado === 'dosFilas') this.dividirNombreCompleto(nombreActualizado);

    this.nombreCharsCount = nombreActualizado.length;
    this.aplicarValidaciones(nombreActualizado);

    this.emitirNombreCambiado();
  }

  validarNombre(nombre: string): string {
    let nombreValido = nombre.replace(/[^A-Z\s]/g, '');
    nombreValido = nombreValido.replace(/\s+/g, ' ');
    return nombreValido;
  }

  prevenirNumeroCaracteres(event: KeyboardEvent) {
    const regex = /^[a-zA-Z\s]*$/;
    if (!regex.test(event.key)) event.preventDefault();
  }

  cambiarDiseno() { this.actualizarNombre(this.tarjeta.nombre); }

  validarEntrada(event: Event): void {
    const input = (event.target as HTMLInputElement);
    let valor = (input.value || '').toUpperCase();

    valor = valor.replace(/[^A-ZÑ\s]/g, '').replace(/\s{2,}/g, ' ');

    input.value = valor;
    this.tarjeta.nombre = valor;

    if (this.disenoSeleccionado === 'dosFilas') this.dividirNombreCompleto(valor);

    this.nombreCharsCount = valor.length;
    this.aplicarValidaciones(valor);

    this.emitirNombreCambiado();
  }

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

  private mostrarNotificacionError(mensaje: string): void {
    this.snackBar.open(mensaje, 'Cerrar', {
      duration: 3500,
      horizontalPosition: 'right',
      verticalPosition: 'top'
    });
  }
}

/* ---------- Layout general ---------- */
.modal { display:none; position:fixed; z-index:1; inset:0; overflow:auto; background-position:center; }

.modal-content {
  background:#fefefe; margin:15% auto; padding:20px; border:1px solid #888;
  width:400px; height:600px; background-repeat:no-repeat; background-size:cover;
}

.contenedor{
  position: relative;
  display: flex;
  justify-content: center;
  align-items: center;
}

/* La imagen de la tarjeta es el marco de referencia */
.content-imagen-tarjeta {
  position: relative; /* clave: textos se posicionan contra la imagen */
  width: 207.87404194px;
  height: 321.25988299px;
  display: block;
}

@media print { .no-imprimir{ display:none; } }

.imagen-tarjeta { width:100%; height:100%; object-fit:contain; display:block; }

/* Preload: no ocupa espacio ni se ve */
.preload { position:absolute; width:0; height:0; opacity:0; pointer-events:none; }

/* ====== Stage: apila los dos diseños y hace cross-fade ====== */
.design-stage {
  position: absolute;
  inset: 0;
  overflow: hidden;
}

.design-layer {
  position: absolute;
  inset: 0;
  opacity: 0;
  transition: opacity 180ms ease;
  pointer-events: none;
}

.design-layer.active {
  opacity: 1;
  pointer-events: auto;
}

/* ---------- DISEÑO 1 (una fila) centrado, como ya tenías ---------- */
.nombres-una-fila {
  position: absolute;
  top: 60%;
  left: 50%;
  transform: translateX(-50%);
  font-size: 6pt;
  color: white;
  text-align: center;
  max-width: 90%;
  white-space: nowrap; overflow:hidden; text-overflow:ellipsis;
}

.cuenta-una-fila{
  position: absolute;
  top: 67%;
  left: 50%;
  transform: translateX(-50%);
  font-size: 7pt;
  text-align: center;
  max-width: 80%;
  color: white;
  white-space: nowrap; overflow:hidden; text-overflow:ellipsis;
}

/* ---------- DISEÑO 2 (dos filas) ---------- */
/* Anclado a la derecha para “nacer” a la derecha y crecer a la izquierda */
.nombres, .apellidos, .cuenta {
  position: absolute;
  right: 6%;            /* margen desde el borde derecho de la imagen */
  max-width: 82%;       /* área útil horizontal (evita pegarse al borde izquierdo) */
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  text-align: right;
}
.nombres   { top: 53%; font-size: 6pt; color: white; }
.apellidos { top: 58%; font-size: 6pt; color: white; }
.cuenta    { top: 65%; font-size: 7pt; color: white; }

/* ---------- Footer y acciones ---------- */
.modal-footer { padding:10px; display:flex; flex-direction:column; justify-content:space-around; height:100px; }

.mat-dialog-actions { align-items:center; justify-content:space-between; display:flex; flex-wrap:wrap; }

.action-buttons .flex-container { display:flex; justify-content:space-between; align-items:center; width:100%; }

.nombre-input { flex-grow:1; margin-right:20px; width:100%; text-transform:uppercase; }

.spacer { flex:1; }

.imprimir-btn { background:#4CAF50; color:#fff; } .imprimir-btn:hover { background:#45a049; }
.cerrar-btn { background:#f44336; color:#fff; } .cerrar-btn:hover { background:#da190b; }

/* Contador externo cuando hay error (Material oculta el mat-hint si hay mat-error) */
.contador-externo {
  width: 100%;
  text-align: right;
  margin-top: -8px;
  margin-bottom: 8px;
  font-size: 12px;
  color: rgba(0,0,0,0.6);
}

