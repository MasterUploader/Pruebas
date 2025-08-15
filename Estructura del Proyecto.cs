<!--
  Vista del modal de impresión de tarjeta.
  - Siempre imprime en 2 filas.
  - El input usa Reactive Forms; el botón siempre está habilitado,
    pero la acción "imprimir" valida y frena si es inválido.
-->

<h1 mat-dialog-title>Detalle Tarjeta</h1>

<form [formGroup]="form">
  <div mat-dialog-content id="contenidoImprimir">
    <div class="contenedor">

      <!-- Imagen base de la tarjeta (diseño fijo de 2 filas) -->
      <div class="content-imagen-tarjeta">
        <img src="/assets/Tarjeta3.PNG" alt=" tarjeta" class="imagen-tarjeta no-imprimir">
      </div>

      <!-- Overlay: dos líneas de nombre y número de cuenta enmascarado -->
      <div class="nombre-completo">
        <div class="nombres">
          <b>{{ nombres }}</b>
        </div>
        <div class="apellidos">
          <b>{{ apellidos }}</b>
        </div>

        <!-- Número de Cuenta enmascarado con MaskAccountNumberPipe -->
        <div class="cuenta">
          <b>{{ tarjeta.numeroCuenta | maskAccountNumber }}</b>
        </div>
      </div>
    </div>

    <div mat-dialog-actions class="action-buttons">
      <!-- Campo de nombre (reactivo) -->
      <mat-form-field appearance="fill" class="nombre-input">
        <mat-label>Nombre:</mat-label>
        <input
          placeholder="NOMBRE EN TARJETA"
          matInput
          formControlName="nombre"
          (input)="form.get('nombre')?.setValue((form.get('nombre')?.value || '').toUpperCase(), { emitEvent: true })"
          maxlength="40"
          autocomplete="off" />

        <!-- Contador de caracteres -->
        <mat-hint align="end">{{ (form.get('nombre')?.value?.length || 0) }}/40</mat-hint>

        <!-- Único mensaje de error, priorizado en TS -->
        @if (nombreError) {
          <mat-error>{{ nombreError }}</mat-error>
        }
      </mat-form-field>

      <!-- Botones de acción -->
      <button mat-button class="imprimir-btn" (click)="imprimir(tarjeta)">Imprimir</button>
      <span class="spacer"></span>
      <button mat-button class="cerrar-btn" (click)="cerrarModal()" [mat-dialog-close]="true">Cerrar</button>
    </div>
  </div>
</form>

/**
 * ModalTarjetaComponent
 * -----------------------------------------------------------------------------
 * - Angular 20, Reactive Forms.
 * - Reglas solicitadas:
 *    • Máximo 40 caracteres para el nombre.
 *    • Siempre se imprime en 2 filas (hasta 20 caracteres por fila), sin cortar
 *      palabras; si no cabe una palabra, se coloca completa en la segunda fila.
 *    • Validación: mínimo 2 palabras (dos nombres), sin importar tamaño.
 *    • Solo mayúsculas, letras y espacios (Ñ incluida).
 *    • El botón "Imprimir" siempre es clickeable, pero se frena con un único
 *      mensaje de error si el dato es inválido.
 * - Calidad:
 *    • Se redujo complejidad cognitiva con helpers y early-returns.
 *    • Miembros inyectados marcados como readonly para Sonar.
 *    • Optional chaining para evitar null checks verbosos.
 * - Flujo:
 *    • Valida: si inválido → snackbar y no continúa.
 *    • Verifica sesión → valida bandera de impresión → imprime → registra estado.
 *    • Al guardar, cierra el modal y recarga (mantiene tu comportamiento actual).
 * -----------------------------------------------------------------------------
 */

import { Component, Input, Output, OnInit, Inject, EventEmitter, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, ValidatorFn, AbstractControl, ValidationErrors } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Subscription, take } from 'rxjs';

import { Tarjeta } from '../../../../core/models/tarjeta.model';
import { ImpresionService } from '../../../../core/services/impresion.service';
import { TarjetaService } from '../../../../core/services/tarjeta.service';
import { AuthService } from '../../../../core/services/auth.service';
import { MaskAccountNumberPipe } from '../../../../shared/pipes/mask-account-number.pipe';

@Component({
  selector: 'app-modal-tarjeta',
  // Este componente usa "imports" al estilo standalone para todo lo que necesita.
  imports: [
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSnackBarModule,
    ReactiveFormsModule,
    MaskAccountNumberPipe    // Pipe standalone para enmascarar número de cuenta en la vista
  ],
  templateUrl: './modal-tarjeta.component.html',
  styleUrl: './modal-tarjeta.component.css'
})
export class ModalTarjetaComponent implements OnInit, OnDestroy {

  // ────────────────────────────────────────────────────────────────────────────
  // Constantes de formato de impresión
  // ────────────────────────────────────────────────────────────────────────────
  /** Longitud máxima permitida para el nombre en el input. */
  private readonly MAX_NAME_LEN = 40;
  /** Longitud máxima de cada línea visible en la tarjeta. */
  private readonly MAX_LINE = 20;

  /** Bolsa de suscripciones del componente (readonly para Sonar). */
  private readonly subscription: Subscription = new Subscription();

  /** Bandera que devuelve el backend para indicar si ya se imprimió previamente. */
  private imprime = false;

  // ────────────────────────────────────────────────────────────────────────────
  // API del componente
  // ────────────────────────────────────────────────────────────────────────────
  /** Notifica al padre cuando cambia el nombre (para usos externos si se requiere). */
  @Output() nombreCambiado = new EventEmitter<string>();

  /** Datos de la tarjeta recibidos (fallback si no se usa MAT_DIALOG_DATA). */
  @Input() datosTarjeta: Tarjeta = {
    nombre: '',
    numero: '',
    fechaEmision: '',
    fechaVencimiento: '',
    motivo: '',
    numeroCuenta: ''
  };

  /** Formulario reactivo principal (solo el campo "nombre"). */
  form!: FormGroup;

  // ────────────────────────────────────────────────────────────────────────────
  // Estado de la vista (lo que se renderiza sobre la imagen de la tarjeta)
  // ────────────────────────────────────────────────────────────────────────────
  /** Línea 1 del nombre impreso. */
  nombres: string = '';
  /** Línea 2 del nombre impreso. */
  apellidos: string = '';
  /** Usuario que realiza la impresión (desde sesión). */
  usuarioICBS: string = '';
  /** Último nombre normalizado emitido hacia el padre. */
  nombreMandar: string = '';

  // ────────────────────────────────────────────────────────────────────────────
  // Inyección de dependencias (readonly cuando no se reasignan)
  // ────────────────────────────────────────────────────────────────────────────
  constructor(
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly snackBar: MatSnackBar,
    public  readonly dialogRef: MatDialogRef<ModalTarjetaComponent>,
    private readonly impresionService: ImpresionService,
    private readonly tarjetaService: TarjetaService,
    private readonly cdr: ChangeDetectorRef,
    @Inject(MAT_DIALOG_DATA) public tarjeta: Tarjeta    // Datos del row seleccionado
  ) {}

  // ────────────────────────────────────────────────────────────────────────────
  // Ciclo de vida
  // ────────────────────────────────────────────────────────────────────────────
  ngOnInit(): void {
    // Suscríbete al estado de sesión y reacciona a cambios.
    this.subscription.add(
      this.authService.sessionActive$.subscribe(isActive => this.handleSessionChange(isActive))
    );

    // Construye formulario y enlaza sus cambios.
    this.buildForm();
    this.bindForm();

    // Normaliza y pinta el nombre inicial.
    this.bootstrapName();
  }

  ngOnDestroy(): void {
    // Evita fugas de memoria.
    this.subscription.unsubscribe();
  }

  // ────────────────────────────────────────────────────────────────────────────
  // Inicialización y enlaces
  // ────────────────────────────────────────────────────────────────────────────

  /**
   * Reacciona a cambios de sesión.
   * - Si está inactiva: cierra sesión.
   * - Si está activa: actualiza usuario y fuerza detección de cambios.
   */
  private handleSessionChange(isActive: boolean): void {
    if (!isActive) {
      this.authService.logout();
      return;
    }
    this.usuarioICBS = this.authService.currentUserValue?.activeDirectoryData?.usuarioICBS ?? '';
    this.actualizarNombre((this.tarjeta?.nombre ?? '').toUpperCase());
    this.cdr.detectChanges();
    this.cdr.markForCheck();
  }

  /** Construye el FormGroup con las reglas de validación. */
  private buildForm(): void {
    this.form = this.fb.group({
      nombre: [
        (this.tarjeta?.nombre ?? '').toUpperCase(),
        [
          Validators.required,               // obligatorio
          Validators.pattern(/^[A-ZÑ ]+$/),  // solo mayúsculas, letras y espacios
          Validators.maxLength(this.MAX_NAME_LEN), // 40 máx
          this.minTwoWords()                 // mínimo dos palabras
        ]
      ]
    });
  }

  /**
   * Enlaza los cambios del control 'nombre':
   * - Fuerza MAYÚSCULAS.
   * - Limpia caracteres no permitidos.
   * - Recalcula las dos líneas que se imprimen.
   */
  private bindForm(): void {
    const nombreCtrl = this.form.get('nombre')!;
    this.subscription.add(
      nombreCtrl.valueChanges.subscribe((v: string) => {
        const up = (v ?? '').toUpperCase();
        if (v !== up) {
          // Reescribe en mayúsculas sin disparar loop
          nombreCtrl.setValue(up, { emitEvent: false });
        }
        // Limpia y refleja en el modelo interno
        this.tarjeta.nombre = this.normalizarNombre(up);
        // Recalcula las líneas de impresión
        this.actualizarNombre(this.tarjeta.nombre);
      })
    );
  }

  /** Normaliza y representa el nombre inicial. */
  private bootstrapName(): void {
    const inicial = (this.tarjeta?.nombre ?? '').toUpperCase();
    this.tarjeta.nombre = this.normalizarNombre(inicial);
    this.actualizarNombre(this.tarjeta.nombre);
  }

  // ────────────────────────────────────────────────────────────────────────────
  // Validadores y utilitarios (baja complejidad cognitiva)
  // ────────────────────────────────────────────────────────────────────────────

  /**
   * Validador: exige al menos 2 palabras.
   * Si el campo está vacío, NO marca error aquí para que sea 'required' quien lo haga.
   */
  private minTwoWords(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const raw = (control.value ?? '').toString().trim();
      if (!raw) return null; // vacío lo maneja 'required'
      const words = this.normalizarNombre(raw).split(/\s+/).filter(Boolean);
      return words.length >= 2 ? null : { twoWords: true };
    };
  }

  /** Limpia: mayúsculas, elimina no permitidos, colapsa espacios. */
  private normalizarNombre(nombre: string): string {
    let out = (nombre ?? '').toUpperCase().replace(/[^A-ZÑ\s]/g, '');
    return out.replace(/\s+/g, ' ').trim();
  }

  /** Separa el texto en tokens (palabras) ignorando vacíos. */
  private tokenize(full: string): string[] {
    return (full ?? '').split(' ').filter(Boolean);
  }

  /** Indica si un token cabe en 'line' respetando el máximo. */
  private canFit(line: string, token: string, max: number = this.MAX_LINE): boolean {
    return line.length === 0 ? token.length <= max : (line.length + 1 + token.length) <= max;
  }

  /** Concatena un token a la línea agregando espacio si corresponde. */
  private concatLine(line: string, token: string): string {
    return line.length ? `${line} ${token}` : token;
  }

  /**
   * Calcula 2 líneas (máx 20 c/u) sin cortar palabras.
   * Si una palabra no cabe en la línea 2, se agrega completa (no se corta),
   * pudiendo exceder el máximo en casos extremos (raro por maxlength=40).
   */
  private computeTwoLines(full: string): { line1: string; line2: string } {
    const tokens = this.tokenize(full);
    let line1 = '';
    let line2 = '';

    for (const t of tokens) {
      if (this.canFit(line1, t)) { line1 = this.concatLine(line1, t); continue; }
      if (this.canFit(line2, t)) { line2 = this.concatLine(line2, t); continue; }
      // Fallback: no cortar
      line2 = this.concatLine(line2, t);
    }
    return { line1, line2 };
  }

  /** Recalcula líneas visibles y emite cambio. */
  private actualizarNombre(nombre: string): void {
    const limpio = this.normalizarNombre(nombre);
    const { line1, line2 } = this.computeTwoLines(limpio);
    this.nombres = line1;
    this.apellidos = line2;
    this.emitirNombreCambiado();
  }

  /** Emite el nombre normalizado hacia el padre (si lo desean escuchar). */
  private emitirNombreCambiado(): void {
    this.nombreMandar = (this.tarjeta?.nombre ?? '').toUpperCase();
    this.nombreCambiado.emit(this.nombreMandar);
  }

  /**
   * Devuelve un único mensaje de error (priorizado) para evitar superposición
   * de múltiples <mat-error> en el template.
   */
  get nombreError(): string | null {
    const c = this.form.get('nombre');
    if (!(c?.touched)) return null;

    const messages: Record<string, string> = {
      required: 'El nombre es obligatorio.',
      twoWords: 'Debe ingresar al menos dos nombres.',
      maxlength: `El nombre no puede exceder ${this.MAX_NAME_LEN} caracteres.`,
      pattern: 'Solo se permiten letras y espacios en mayúsculas.'
    };

    for (const key of Object.keys(messages)) {
      if (c.hasError(key)) return messages[key];
    }
    return null;
  }

  // ────────────────────────────────────────────────────────────────────────────
  // Flujo principal
  // ────────────────────────────────────────────────────────────────────────────

  /**
   * Acción de imprimir:
   * 1) Bloquea si el formulario es inválido (muestra snackbar).
   * 2) Verifica sesión activa.
   * 3) Consulta si se puede imprimir; si sí → imprime y registra.
   * 4) Al guardar, cierra y recarga (manteniendo UX actual).
   */
  imprimir(datosParaImprimir: Tarjeta): void {
    const nombreCtrl = this.form.get('nombre')!;
    if (this.blockIfInvalid(nombreCtrl)) return;

    this.ensureSessionActive(() => {
      this.checkImpresionAndPrint(datosParaImprimir);
    });
  }

  /** Si el control es inválido, lo marca, muestra snackbar y retorna true. */
  private blockIfInvalid(ctrl: AbstractControl): boolean {
    if (ctrl.valid) return false;
    ctrl.markAsTouched();
    ctrl.updateValueAndValidity();
    this.showSnack(this.nombreError ?? 'El nombre no es válido.');
    return true;
  }

  /** Verifica sesión activa antes de continuar con el flujo. */
  private ensureSessionActive(onActive: () => void): void {
    this.authService.sessionActive$.pipe(take(1)).subscribe(isActive => {
      if (!isActive) { this.authService.logout(); return; }
      onActive();
    });
  }

  /**
   * Consulta si ya estaba impresa. Si no lo está, ejecuta flujo de impresión+registro.
   * Si el backend marca que ya está impresa, simplemente cierra el modal.
   */
  private checkImpresionAndPrint(datosParaImprimir: Tarjeta): void {
    this.tarjetaService.validaImpresion(this.tarjeta.numero).pipe(take(1)).subscribe({
      next: (r) => {
        this.imprime = !!r.imprime;
        if (this.imprime) { this.cerrarModal(); return; }
        this.performPrintAndRegister(datosParaImprimir);
      },
      error: (e) => console.error('Error en validaImpresion', e)
    });
  }

  /**
   * Imprime (en 2 filas) y registra el estado de impresión en el backend.
   * Al finalizar, cierra el modal y recarga la página (comportamiento original).
   */
  private performPrintAndRegister(datosParaImprimir: Tarjeta): void {
    const tipoDiseno = false; // true = 1 fila, false = 2 filas → aquí SIEMPRE 2 filas
    const ok = this.impresionService.imprimirTarjeta(datosParaImprimir, tipoDiseno);
    if (!ok) return;

    this.tarjetaService
      .guardaEstadoImpresion(this.tarjeta.numero, this.usuarioICBS, (this.tarjeta.nombre ?? '').toUpperCase())
      .pipe(take(1))
      .subscribe({
        next: () => { this.cerrarModal(); window.location.reload(); },
        error: (e) => console.error('Error al guardar estado de impresión', e)
      });
  }

  /** Muestra un snackbar en la parte superior centrado. */
  private showSnack(message: string): void {
    this.snackBar.open(message, 'Cerrar', {
      duration: 3500,
      verticalPosition: 'top',
      horizontalPosition: 'center'
    });
  }

  /** Cierra el modal (sin valor de retorno adicional). */
  cerrarModal(): void {
    this.dialogRef.close();
  }
}


