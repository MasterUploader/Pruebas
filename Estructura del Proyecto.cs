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
  imports: [CommonModule, MatDialogModule, MatInputModule, MatSelectModule, FormsModule, MaskAccountNumberPipe],
  templateUrl: './modal-tarjeta.component.html',
  styleUrl: './modal-tarjeta.component.css'
})
export class ModalTarjetaComponent implements OnInit, OnDestroy {

  // ===== Constantes de validación =====
  private readonly MAX_NAME_LEN = 40; // total
  private readonly MAX_LINE = 20;     // por línea (diseño de dos filas)

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
  nombres: string = '';     // línea 1 (diseño 2)
  apellidos: string = '';   // línea 2 (diseño 2)
  numeroCuenta: string = '';
  usuarioICBS: string = '';
  nombreMandar: string = '';
  disenoSeleccionado: string = 'dosFilas';

  /** Bandera para habilitar el botón Imprimir (flujo original). */
  nombreValidoParaImprimir = false;

  /** Mensaje único para mostrar debajo del campo cuando hay error. */
  nombreError: string | null = null;

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
        this.actualizarNombre(this.tarjeta.nombre); // recalcula líneas
        // Inicializa validación para el estado actual del campo
        this.nombreValidoParaImprimir = this.validarYSetErrores(this.tarjeta.nombre);
        this.cdr.detectChanges();
        this.cdr.markForCheck();
      } else {
        this.authService.logout();
      }
    }));
  }

  ngOnDestroy(): void { this.subscription.unsubscribe(); }

  // ===== Impresión (flujo original, con guardas de validación) =====
  imprimir(datosParaImprimir: Tarjeta): void {
    // 1) Guardar de validación: no avanzar si hay errores
    if (!this.validarYSetErrores(this.tarjeta.nombre)) {
      this.snackBar.open('No puedes imprimir: corrige el nombre.', 'Cerrar', { duration: 3500, verticalPosition: 'top' });
      return;
    }

    let impresionExitosa = false;

    this.subscription.add(this.authService.sessionActive$.subscribe(isActive => {
      if (isActive) {
        this.tarjetaService.validaImpresion(this.tarjeta.numero).subscribe({
          next: (respuesta) => { this.imprime = !!respuesta.imprime; }
        });

        if (!this.imprime) {
          const tipoDiseño = this.disenoSeleccionado === 'unaFila'; // true=una fila, false=dos filas
          impresionExitosa = this.impresionService.imprimirTarjeta(datosParaImprimir, tipoDiseño);
          if (impresionExitosa) {
            this.tarjetaService.guardaEstadoImpresion(
              this.tarjeta.numero,
              this.usuarioICBS,
              this.tarjeta.nombre.toUpperCase()
            ).subscribe({
              next: () => {
                this.cerrarModal();
                window.location.reload();
              },
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

  /**
   * Divide en 2 líneas SIN cortar palabras.
   * Cada línea admite máximo 20 caracteres.
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
        // Si no cabe en ninguna línea, se omite (mismo criterio que tenías).
        break;
      }
    }
    return { line1: l1, line2: l2 };
  }

  dividirNombreCompleto(nombreCompleto: string) {
    const cadena = (nombreCompleto ?? '').toUpperCase().trim().replace(/\s+/g, ' ');
    const { line1, line2 } = this.computeTwoLines(cadena);
    this.nombres = line1;
    this.apellidos = line2;
  }

  actualizarNombre(nombre: string) {
    let nombreActualizado = (nombre ?? '').toUpperCase();
    nombreActualizado = this.validarNombre(nombreActualizado);
    // límite total 40
    if (nombreActualizado.length > this.MAX_NAME_LEN) {
      nombreActualizado = nombreActualizado.slice(0, this.MAX_NAME_LEN).trim();
    }
    this.tarjeta.nombre = nombreActualizado;

    if (this.disenoSeleccionado === 'dosFilas') {
      this.dividirNombreCompleto(nombreActualizado);
    }
    this.emitirNombreCambiado();
  }

  /**
   * Sanitiza: solo letras (incluye Ñ y acentos) y un solo espacio.
   */
  validarNombre(nombre: string): string {
    let nombreValido = nombre.replace(/[^A-ZÑÁÉÍÓÚÜ\s]/g, ''); // Elimina no permitidos
    nombreValido = nombreValido.replace(/\s+/g, ' ').trim();    // Colapsa espacios
    return nombreValido;
  }

  prevenirNumeroCaracteres(event: KeyboardEvent) {
    const regex = /^[a-zA-ZñÑáéíóúÁÉÍÓÚüÜ\s]*$/;
    if (!regex.test(event.key)) { event.preventDefault(); }
  }

  cambiarDiseno() { this.actualizarNombre(this.tarjeta.nombre); }

  /**
   * Entrada en tiempo real:
   * - Mayúsculas, sanitiza, recorta a 40.
   * - Recalcula dos líneas (si aplica).
   * - Actualiza bandera [disabled] y mensaje de error.
   */
  validarEntrada(event: Event): void {
    const input = (event.target as HTMLInputElement);
    let valor = (input.value ?? '').toUpperCase();
    valor = valor.replace(/[^A-ZÑÁÉÍÓÚÜ\s]/g, '').replace(/\s{2,}/g, ' ').trim();
    if (valor.length > this.MAX_NAME_LEN) valor = valor.slice(0, this.MAX_NAME_LEN).trim();

    input.value = valor;
    this.tarjeta.nombre = valor;

    // Recalcula nombres/apellidos si es diseño de dos filas
    if (this.disenoSeleccionado === 'dosFilas') {
      this.dividirNombreCompleto(valor);
    }
    this.emitirNombreCambiado();

    // Valida y setea mensaje + habilita/deshabilita imprimir
    this.nombreValidoParaImprimir = this.validarYSetErrores(valor);
  }

  /**
   * Reglas:
   *  - obligatorio
   *  - mínimo 2 palabras
   *  - total ≤ 40
   *  - ninguna palabra > 20
   *  - si es diseño de dos filas: cada línea ≤ 20
   * Retorna true si todo OK (permite imprimir).
   */
  private validarYSetErrores(valor: string): boolean {
    const v = (valor ?? '').trim();
    if (!v) { this.nombreError = 'El nombre es obligatorio.'; return false; }

    const words = v.split(/\s+/).filter(Boolean);
    if (words.length < 2) { this.nombreError = 'Ingresa al menos dos nombres.'; return false; }

    if (v.length > this.MAX_NAME_LEN) { this.nombreError = 'Máximo 40 caracteres.'; return false; }

    // Palabra demasiado larga para la línea
    if (words.some(w => w.length > this.MAX_LINE)) {
      this.nombreError = 'Ninguna palabra puede exceder 20 caracteres.';
      return false;
    }

    if (this.disenoSeleccionado === 'dosFilas') {
      const { line1, line2 } = this.computeTwoLines(v);
      if (line1.length > this.MAX_LINE || line2.length > this.MAX_LINE) {
        this.nombreError = 'Cada línea admite hasta 20 caracteres.';
        return false;
      }
    }

    this.nombreError = null;
    return true;
  }
}




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
      <div class="cuenta-una-fila">
        <b>{{ tarjeta.numeroCuenta | maskAccountNumber }}</b>
      </div>
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
      <div class="cuenta">
        <b>{{ tarjeta.numeroCuenta | maskAccountNumber }}</b>
      </div>
    </div>
  </div>

  <div mat-dialog-actions class="action-buttons">
    <!-- Selector de Diseño de Tarjeta -->
    <mat-form-field appearance="fill" class="diseño-input">
      <mat-label>Diseño</mat-label>
      <mat-select [(value)]="disenoSeleccionado" (selectionChange)="cambiarDiseno()">
        <mat-option value="unaFila">Diseño 1</mat-option>
        <mat-option value="dosFilas">Diseño 2</mat-option>
      </mat-select>
    </mat-form-field>

    <!-- Campo de nombre (se mantiene template-driven) -->
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

      <!-- Único mensaje de error (bloquea el flujo de impresión) -->
      <mat-error *ngIf="!!nombreError">{{ nombreError }}</mat-error>
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
