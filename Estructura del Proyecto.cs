<h1 mat-dialog-title> Detalle Tarjeta</h1>
<div mat-dialog-content id="contenidoImprimir">
  <div class="contenedor">
    <div class="content-imagen-tarjeta">
      <img
        [src]="disenoSeleccionado === 'unaFila' ? '/assets/TarjetaDiseño2.png' : '/assets/Tarjeta3.PNG'"
        alt="imagen tarjeta"
        class="imagen-tarjeta no-imprimir">
    </div>

    <!-- Diseño para una fila -->
    <div *ngIf="disenoSeleccionado === 'unaFila'" class="nombre-completo">
      <div class="nombres-una-fila">
        <b>{{ tarjeta.nombre }}</b>
      </div>
      <!-- Numero de Cuenta -->
      <div class="cuenta-una-fila"><b>{{ tarjeta.numeroCuenta | maskAccountNumber }}</b></div>
    </div>

    <!-- Diseño para dos filas -->
    <div *ngIf="disenoSeleccionado === 'dosFilas'" class="nombre-completo">
      <div class="nombres">
        <b>{{ nombres }}</b>
      </div>
      <div class="apellidos">
        <b>{{ apellidos }}</b>
      </div>
      <!-- Numero de Cuenta -->
      <div class="cuenta"><b>{{ tarjeta.numeroCuenta | maskAccountNumber }}</b></div>
    </div>
  </div>

  <div mat-dialog-actions class="action-buttons">
    <!-- Selector de Diseño de Tarjeta -->
    <mat-form-field appearance="fill" class="diseño-input">
      <mat-label>Diseño</mat-label>
      <mat-select [(value)]="disenoSeleccionado" (selectionChange)="cambiarDiseno()">
        <!-- Diseño 1 Primera Tarjeta Vertical Abril 2024 -->
        <mat-option value="unaFila">Diseño 1</mat-option>
        <!-- Diseño 2 Segunda Tarjeta Vertical Noviembre de 2024 -->
        <mat-option value="dosFilas">Diseño 2</mat-option>
      </mat-select>
    </mat-form-field>

    <!-- Campo de nombre (template-driven, se mantiene) -->
    <mat-form-field appearance="fill" class="nombre-input">
      <mat-label>Nombre:</mat-label>
      <input
        placeholder="Nombre en Tarjeta"
        matInput
        [(ngModel)]="tarjeta.nombre"
        name="nombreTarjeta"
        (input)="validarEntrada($event)"
        (keypress)="prevenirNumeroCaracteres($event)"
        maxlength="40"
        autocomplete="off">
      <mat-hint align="end">{{ (tarjeta.nombre?.length || 0) }}/40</mat-hint>
    </mat-form-field>

    <button
      mat-button
      class="imprimir-btn"
      (click)="imprimir(tarjeta)"
      [disabled]="!nombreValidoParaImprimir">
      Imprimir
    </button>

    <span class="spacer"></span>

    <button mat-button class="cerrar-btn" (click)="cerrarModal()" [mat-dialog-close]="true">
      Cerrar
    </button>
  </div>
</div>

import { Component, Input, Output, OnInit, Inject, EventEmitter, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSelectModule } from '@angular/material/select';
import { DomSanitizer } from '@angular/platform-browser';
import { Tarjeta } from '../../../../core/models/tarjeta.model';
import { ImpresionService } from '../../../../core/services/impresion.service';
import { ConfirmacionDialogoComponent } from '../../../../modules/variados/components/confirmacion-dialogo/confirmacion-dialogo.component';
import { TarjetaService } from '../../../../core/services/tarjeta.service';
import { AuthService } from '../../../../core/services/auth.service';
import { MaskAccountNumberPipe } from '../../../../shared/pipes/mask-account-number.pipe';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-modal-tarjeta',
  standalone: true,
  imports: [
    CommonModule, MatDialogModule, MatInputModule, MatSelectModule,
    FormsModule, MaskAccountNumberPipe
  ],
  templateUrl: './modal-tarjeta.component.html',
  styleUrl: './modal-tarjeta.component.css'
})
export class ModalTarjetaComponent implements OnInit, OnDestroy {

  private subscription: Subscription = new Subscription();
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
  disenoSeleccionado: string = 'dosFilas';

  /** Máximo por línea cuando son dos filas. */
  maxCaracteresFila: number = 20;

  /** Habilita/deshabilita el botón Imprimir (se mantiene del código original). */
  nombreValidoParaImprimir = false;

  constructor(
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
    this.actualizarNombre(tarjeta.nombre);
    this.nombreCompleto = tarjeta.nombre;
    this.imprime = false;
  }

  ngOnInit(): void {
    this.subscription.add(this.authService.sessionActive$.subscribe(isActive => {
      if (isActive) {
        this.usuarioICBS = this.authService.currentUserValue?.activeDirectoryData.usuarioICBS ?? '';
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

  /** Click en Imprimir. Mantiene tu flujo y agrega snackbar de éxito. */
  imprimir(datosParaImprimir: Tarjeta): void {
    // Validación final antes de imprimir
    if (!this.esNombreValido(this.tarjeta.nombre)) {
      this.mostrarSnack('No puedes imprimir: el nombre es inválido o incompleto.');
      return;
    }

    let impresionExitosa = false;

    this.subscription.add(this.authService.sessionActive$.subscribe(isActive => {
      if (isActive) {
        this.tarjetaService.validaImpresion(this.tarjeta.numero).subscribe({
          next: (respuesta) => {
            this.imprime = !!respuesta.imprime;
          },
          error: () => { this.imprime = false; }
        });

        if (!this.imprime) {
          const tipoDiseño = this.disenoSeleccionado === 'unaFila'; // true=una fila, false=dos filas
          impresionExitosa = this.impresionService.imprimirTarjeta(datosParaImprimir, tipoDiseño);

          if (impresionExitosa) {
            // ✅ Snackbar de éxito de impresión
            this.mostrarSnackOk('Impresión enviada a la impresora.');

            this.tarjetaService.guardaEstadoImpresion(
              this.tarjeta.numero,
              this.usuarioICBS,
              this.tarjeta.nombre.toUpperCase()
            ).subscribe({
              next: () => {
                // (opcional) podrías avisar también aquí:
                // this.mostrarSnackOk('Tarjeta marcada como impresa.');
                this.cerrarModal();
                window.location.reload();
              },
              error: (error) => {
                console.log('error', error);
              }
            });
          }
        } else {
          // Ya estaba marcada para no imprimir
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

  emitirNombreCambiado(): void {
    this.nombreMandar = this.tarjeta.nombre.toUpperCase();
    this.nombreCambiado.emit(this.nombreMandar);
  }

  dividirYActualizarNombre(nombreCompleto: string): void {
    this.actualizarNombre(nombreCompleto);
    this.emitirNombreCambiado();
  }

  /**
   * Divide el nombre en 2 líneas sin cortar palabras, máx 20 por línea (dosFilas).
   * Si el diseño es unaFila, se muestra `tarjeta.nombre` tal cual en el HTML.
   */
  dividirNombreCompleto(nombreCompleto: string): void {
    const cadena = (nombreCompleto ?? '').toUpperCase().trim().replace(/\s+/g, ' ');
    const { line1, line2 } = this.computeTwoLines(cadena, this.maxCaracteresFila);
    this.nombres = line1;
    this.apellidos = line2;
  }

  actualizarNombre(nombre: string): void {
    let nombreActualizado = (nombre ?? '').toUpperCase();
    nombreActualizado = this.validarNombre(nombreActualizado);
    this.tarjeta.nombre = nombreActualizado;

    if (this.disenoSeleccionado === 'dosFilas') {
      this.dividirNombreCompleto(nombreActualizado);
    }

    this.emitirNombreCambiado();
  }

  /**
   * Sanea: solo letras (incluye Ñ y acentos) y un solo espacio entre palabras.
   */
  validarNombre(nombre: string): string {
    let nombreValido = nombre.replace(/[^A-ZÑÁÉÍÓÚÜ\s]/g, '');
    nombreValido = nombreValido.replace(/\s+/g, ' ').trim();
    return nombreValido;
  }

  /** Bloquea teclas que no sean letras o espacio. */
  prevenirNumeroCaracteres(event: KeyboardEvent): void {
    const regex = /^[a-zA-ZñÑáéíóúÁÉÍÓÚüÜ\s]*$/;
    if (!regex.test(event.key)) {
      event.preventDefault();
    }
  }

  /** Cambio de diseño ⇒ recalcular líneas si corresponde. */
  cambiarDiseno(): void {
    this.actualizarNombre(this.tarjeta.nombre);
  }

  /**
   * Input en tiempo real:
   * - Mayúsculas y sanitiza.
   * - Recalcula overlay (nombres/apellidos).
   * - Actualiza bandera para [disabled] según validación.
   */
  validarEntrada(event: Event): void {
    const input = event.target as HTMLInputElement;
    let valor = (input.value ?? '').toUpperCase();

    // Sanea: letras + espacios, colapsa espacios
    valor = valor.replace(/[^A-ZÑÁÉÍÓÚÜ\s]/g, '').replace(/\s{2,}/g, ' ').trim();

    // Limite total 40
    if (valor.length > 40) {
      valor = valor.slice(0, 40).trim();
    }

    input.value = valor;
    this.tarjeta.nombre = valor;

    // Recalcular líneas y emitir cambio para la grilla
    this.actualizarNombre(valor);

    // Valida: mínimo 2 palabras
    this.nombreValidoParaImprimir = this.esNombreValido(valor);
  }

  /** Reglas de validez: 2 palabras mín. + ≤ 40 chars + solo letras/espacios. */
  private esNombreValido(valor: string): boolean {
    const okChars = /^[A-ZÑÁÉÍÓÚÜ\s]*$/.test(valor);
    const words = valor.trim().split(/\s+/).filter(Boolean);
    return okChars && valor.length > 0 && valor.length <= 40 && words.length >= 2;
  }

  /** Crea dos líneas sin cortar palabras, respetando el máximo por línea. */
  private computeTwoLines(full: string, maxPerLine: number): { line1: string; line2: string } {
    const tokens = (full ?? '').split(' ').filter(Boolean);
    let l1 = '', l2 = '';
    for (const t of tokens) {
      if (!l1.length || (l1.length + 1 + t.length) <= maxPerLine) {
        l1 = l1 ? `${l1} ${t}` : t;
      } else if (!l2.length || (l2.length + 1 + t.length) <= maxPerLine) {
        l2 = l2 ? `${l2} ${t}` : t;
      } else {
        // Si no cabe en ninguna, se omite (criterio original)
        break;
      }
    }
    return { line1: l1, line2: l2 };
  }

  // ────────── Snackbars ──────────
  private mostrarSnack(mensaje: string): void {
    this.snackBar.open(mensaje, 'Cerrar', {
      duration: 4000,
      verticalPosition: 'top',
      horizontalPosition: 'center'
    });
  }

  private mostrarSnackOk(mensaje: string): void {
    this.mostrarSnack(mensaje);
  }
}




