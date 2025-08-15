import { Component, Input, Output, OnInit, Inject, EventEmitter, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatInputModule } from '@angular/material/input';
import { FormsModule, NgModel } from '@angular/forms';
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

  // === Reglas de validación ===
  private readonly MAX_NAME_LEN = 40; // total
  private readonly MAX_LINE = 20;     // por línea (solo diseño 2)

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

  nombreCompleto = '';
  nombres = '';     // línea 1 (diseño 2)
  apellidos = '';   // línea 2 (diseño 2)
  numeroCuenta = '';
  usuarioICBS = '';
  nombreMandar = '';
  disenoSeleccionado: 'unaFila' | 'dosFilas' = 'dosFilas';

  /** Controla el [disabled] del botón imprimir. */
  nombreValidoParaImprimir = false;

  /** Texto del único mat-error. */
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
      if (!isActive) { this.authService.logout(); return; }

      this.usuarioICBS = this.authService.currentUserValue?.activeDirectoryData.usuarioICBS ?? '';
      this.actualizarNombre(this.tarjeta.nombre);
      // Valida estado inicial
      this.nombreValidoParaImprimir = this.validarYSetErrores(this.tarjeta.nombre);
      this.cdr.detectChanges();
      this.cdr.markForCheck();
    }));
  }

  ngOnDestroy(): void { this.subscription.unsubscribe(); }

  // ======= Impresión (con guardas de validación) =======
  imprimir(datosParaImprimir: Tarjeta): void {
    if (!this.validarYSetErrores(this.tarjeta.nombre)) {
      this.snackBar.open('No puedes imprimir: corrige el nombre.', 'Cerrar', { duration: 3500, verticalPosition: 'top' });
      return;
    }

    let impresionExitosa = false;

    this.subscription.add(this.authService.sessionActive$.subscribe(isActive => {
      if (!isActive) { this.authService.logout(); return; }

      this.tarjetaService.validaImpresion(this.tarjeta.numero).subscribe({
        next: (r) => { this.imprime = !!r.imprime; }
      });

      if (!this.imprime) {
        const tipoDiseño = this.disenoSeleccionado === 'unaFila'; // true=una fila, false=dos
        impresionExitosa = this.impresionService.imprimirTarjeta(datosParaImprimir, tipoDiseño);

        if (impresionExitosa) {
          this.tarjetaService.guardaEstadoImpresion(
            this.tarjeta.numero,
            this.usuarioICBS,
            this.tarjeta.nombre.toUpperCase()
          ).subscribe({
            next: () => { this.cerrarModal(); window.location.reload(); },
            error: (e) => console.log('error', e)
          });
        }
      } else {
        this.cerrarModal();
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

  /** Construye 2 líneas sin cortar palabras, máx 20 c/u. */
  private computeTwoLines(full: string): { line1: string; line2: string } {
    const tokens = (full ?? '').split(' ').filter(Boolean);
    let l1 = '', l2 = '';
    for (const t of tokens) {
      if (!l1.length || (l1.length + 1 + t.length) <= this.MAX_LINE) {
        l1 = l1 ? `${l1} ${t}` : t;
      } else if (!l2.length || (l2.length + 1 + t.length) <= this.MAX_LINE) {
        l2 = l2 ? `${l2} ${t}` : t;
      } else {
        break; // descarta si no cabe en ninguna
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
    let n = (nombre ?? '').toUpperCase();
    n = this.validarNombre(n);
    if (n.length > this.MAX_NAME_LEN) n = n.slice(0, this.MAX_NAME_LEN).trim();
    this.tarjeta.nombre = n;

    if (this.disenoSeleccionado === 'dosFilas') {
      this.dividirNombreCompleto(n);
    }
    this.emitirNombreCambiado();
  }

  /** Sanea: solo letras (incluye Ñ/acentos), un solo espacio, sin espacios al final. */
  validarNombre(nombre: string): string {
    let v = nombre.replace(/[^A-ZÑÁÉÍÓÚÜ\s]/g, ''); // elimina caracteres no permitidos
    v = v.replace(/\s+/g, ' ').trim();               // un solo espacio y sin finales
    return v;
  }

  /** Bloquea teclas: solo letras/espacio y evita espacios dobles o al inicio/fin. */
  prevenirNumeroCaracteres(event: KeyboardEvent) {
    const key = event.key;
    const input = event.target as HTMLInputElement;

    // letras con acentos y Ñ
    const isLetter = /^[a-zA-ZñÑáéíóúÁÉÍÓÚüÜ]$/.test(key);
    const isSpace = key === ' ';

    if (!isLetter && !isSpace) { event.preventDefault(); return; }

    if (isSpace) {
      const start = input.selectionStart ?? 0;
      const end = input.selectionEnd ?? 0;
      const val = input.value;

      // No espacio al inicio, ni doble espacio, ni pegar contra otro espacio
      const left = start > 0 ? val[start - 1] : '';
      const right = end < val.length ? val[end] : '';
      if (start === 0 || left === ' ' || right === ' ') {
        event.preventDefault();
        return;
      }
    }
  }

  cambiarDiseno() { this.actualizarNombre(this.tarjeta.nombre); }

  /**
   * Input en tiempo real:
   * - Mayúsculas, limpia, ≤ 40.
   * - Recalcula 2 líneas (si aplica).
   * - Actualiza errores/estado del botón y, opcionalmente, el NgModel.
   */
  validarEntrada(event: Event, nombreNgModel?: NgModel): void {
    const input = (event.target as HTMLInputElement);
    let valor = (input.value ?? '').toUpperCase();

    // Solo letras/espacios, colapsa espacios y sin trailing
    valor = valor.replace(/[^A-ZÑÁÉÍÓÚÜ\s]/g, '').replace(/\s{2,}/g, ' ').trim();
    if (valor.length > this.MAX_NAME_LEN) valor = valor.slice(0, this.MAX_NAME_LEN).trim();

    input.value = valor;
    this.tarjeta.nombre = valor;

    if (this.disenoSeleccionado === 'dosFilas') this.dividirNombreCompleto(valor);
    this.emitirNombreCambiado();

    const esValido = this.validarYSetErrores(valor);
    this.nombreValidoParaImprimir = esValido;

    // Marca el NgModel como válido/ inválido para que <mat-error> se muestre
    if (nombreNgModel) {
      if (esValido) {
        nombreNgModel.control.setErrors(null);
      } else {
        nombreNgModel.control.setErrors({ custom: true });
      }
      nombreNgModel.control.markAsTouched();
      nombreNgModel.control.markAsDirty();
      nombreNgModel.control.updateValueAndValidity({ emitEvent: false });
    }
  }

  /**
   * Reglas y mensaje:
   *  - obligatorio
   *  - mínimo 2 palabras
   *  - total ≤ 40
   *  - ninguna palabra > 20
   *  - si diseño 2: cada línea ≤ 20
   */
  private validarYSetErrores(valor: string): boolean {
    const v = (valor ?? '').trim();
    if (!v) { this.nombreError = 'El nombre es obligatorio.'; return false; }

    const words = v.split(/\s+/).filter(Boolean);
    if (words.length < 2) { this.nombreError = 'Ingresa al menos dos nombres.'; return false; }

    if (v.length > this.MAX_NAME_LEN) { this.nombreError = 'Máximo 40 caracteres.'; return false; }

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
      <img [src]="disenoSeleccionado === 'unaFila' ? '/assets/TarjetaDiseño2.png' : '/assets/Tarjeta3.PNG'"
           alt="imagen tarjeta"
           class="imagen-tarjeta no-imprimir">
    </div>

    <!-- Diseño para una fila-->
    <div *ngIf="disenoSeleccionado === 'unaFila'" class="nombre-completo">
      <div class="nombres-una-fila">
        <b>{{ tarjeta.nombre }}</b>
      </div>
      <!-- Numero de Cuenta-->
      <div class="cuenta-una-fila"><b>{{ tarjeta.numeroCuenta | maskAccountNumber }}</b></div>
    </div>

    <!-- Diseño para dos filas-->
    <div *ngIf="disenoSeleccionado === 'dosFilas'" class="nombre-completo">
      <div class="nombres"><b>{{ nombres }}</b></div>
      <div class="apellidos"><b>{{ apellidos }}</b></div>
      <!-- Numero de Cuenta-->
      <div class="cuenta"><b>{{ tarjeta.numeroCuenta | maskAccountNumber }}</b></div>
    </div>
  </div>

  <div mat-dialog-actions class="action-buttons">
    <!--Selector de Diseño de Tarjeta-->
    <mat-form-field appearance="fill" class="diseño-input">
      <mat-label>Diseño</mat-label>
      <mat-select [(value)]="disenoSeleccionado" (selectionChange)="cambiarDiseno()">
        <mat-option value="unaFila">Diseño 1</mat-option>
        <mat-option value="dosFilas">Diseño 2</mat-option>
      </mat-select>
    </mat-form-field>

    <!-- Campo de nombre -->
    <mat-form-field appearance="fill" class="nombre-input">
      <mat-label>Nombre:</mat-label>
      <input
        placeholder="Nombre en Tarjeta"
        matInput
        [(ngModel)]="tarjeta.nombre"
        name="nombreTarjeta"
        #nombreNgModel="ngModel"
        (input)="validarEntrada($event, nombreNgModel)"
        (keypress)="prevenirNumeroCaracteres($event)"
        maxlength="40"
        autocomplete="off">
      <mat-hint align="end">{{ (tarjeta.nombre?.length || 0) }}/40</mat-hint>

      <!-- Error visible cuando el NgModel queda inválido -->
      <mat-error *ngIf="nombreNgModel.invalid">{{ nombreError }}</mat-error>
    </mat-form-field>

    <button mat-button class="imprimir-btn"
            (click)="imprimir(tarjeta)"
            [disabled]="!nombreValidoParaImprimir">
      Imprimir
    </button>
    <span class="spacer"></span>
    <button mat-button class="cerrar-btn" (click)="cerrarModal()" [mat-dialog-close]="true">Cerrar</button>
  </div>
</div>



