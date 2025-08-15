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
  imports: [
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSnackBarModule,
    ReactiveFormsModule,
    MaskAccountNumberPipe
  ],
  templateUrl: './modal-tarjeta.component.html',
  styleUrl: './modal-tarjeta.component.css'
})
export class ModalTarjetaComponent implements OnInit, OnDestroy {

  // ===== Constantes de formato =====
  private readonly MAX_NAME_LEN = 40;
  private readonly MAX_LINE = 20;

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

  form!: FormGroup;

  // Vista (dos líneas)
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
  ) {}

  // ===== Ciclo de vida =====
  ngOnInit(): void {
    this.subscription.add(
      this.authService.sessionActive$.subscribe(isActive => this.handleSessionChange(isActive))
    );

    this.buildForm();
    this.bindForm();
    this.bootstrapName();
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  // ===== Inicialización =====
  private handleSessionChange(isActive: boolean): void {
    if (!isActive) {
      this.authService.logout();
      return;
    }
    this.usuarioICBS = this.authService.currentUserValue?.activeDirectoryData.usuarioICBS ?? '';
    this.actualizarNombre((this.tarjeta?.nombre ?? '').toUpperCase());
    this.cdr.detectChanges();
    this.cdr.markForCheck();
  }

  private buildForm(): void {
    this.form = this.fb.group({
      nombre: [
        (this.tarjeta?.nombre ?? '').toUpperCase(),
        [
          Validators.required,
          Validators.pattern(/^[A-ZÑ ]+$/),
          Validators.maxLength(this.MAX_NAME_LEN),
          this.minTwoWords()
        ]
      ]
    });
  }

  private bindForm(): void {
    const nombreCtrl = this.form.get('nombre')!;
    this.subscription.add(
      nombreCtrl.valueChanges.subscribe((v: string) => {
        const up = (v ?? '').toUpperCase();
        if (v !== up) nombreCtrl.setValue(up, { emitEvent: false });
        this.tarjeta.nombre = this.normalizarNombre(up);
        this.actualizarNombre(this.tarjeta.nombre);
      })
    );
  }

  private bootstrapName(): void {
    const inicial = (this.tarjeta?.nombre ?? '').toUpperCase();
    this.tarjeta.nombre = this.normalizarNombre(inicial);
    this.actualizarNombre(this.tarjeta.nombre);
  }

  // ===== Validadores y helpers =====

  /** >= 2 palabras; si está vacío, deja que 'required' lo maneje. */
  private minTwoWords(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const raw = (control.value ?? '').toString().trim();
      if (!raw) return null;
      const words = this.normalizarNombre(raw).split(/\s+/).filter(Boolean);
      return words.length >= 2 ? null : { twoWords: true };
    };
  }

  /** Mayúsculas, solo letras/espacios, colapsa espacios. */
  private normalizarNombre(nombre: string): string {
    let out = (nombre ?? '').toUpperCase().replace(/[^A-ZÑ\s]/g, '');
    return out.replace(/\s+/g, ' ').trim();
  }

  private tokenize(full: string): string[] {
    return (full ?? '').split(' ').filter(Boolean);
  }

  private canFit(line: string, token: string, max: number = this.MAX_LINE): boolean {
    return line.length === 0 ? token.length <= max : (line.length + 1 + token.length) <= max;
  }

  private concatLine(line: string, token: string): string {
    return line.length ? `${line} ${token}` : token;
  }

  /** Calcula 2 líneas (máx 20 c/u) sin cortar palabras; si no cabe, prioriza no cortar. */
  private computeTwoLines(full: string): { line1: string; line2: string } {
    const tokens = this.tokenize(full);
    let line1 = '';
    let line2 = '';

    for (const t of tokens) {
      if (this.canFit(line1, t)) { line1 = this.concatLine(line1, t); continue; }
      if (this.canFit(line2, t)) { line2 = this.concatLine(line2, t); continue; }
      line2 = this.concatLine(line2, t); // fallback sin cortar
    }
    return { line1, line2 };
  }

  private actualizarNombre(nombre: string): void {
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

  /** Único mensaje de error para el mat-error (evita superposición). */
  get nombreError(): string | null {
    const c = this.form.get('nombre');
    if (!c || !c.touched) return null;

    const map: Record<string, string> = {
      required: 'El nombre es obligatorio.',
      twoWords: 'Debe ingresar al menos dos nombres.',
      maxlength: `El nombre no puede exceder ${this.MAX_NAME_LEN} caracteres.`,
      pattern: 'Solo se permiten letras y espacios en mayúsculas.'
    };

    for (const key of Object.keys(map)) {
      if ((c as any).hasError(key)) return map[key];
    }
    return null;
  }

  // ===== Flujo principal =====

  imprimir(datosParaImprimir: Tarjeta): void {
    const nombreCtrl = this.form.get('nombre')!;
    if (this.blockIfInvalid(nombreCtrl)) return;

    this.ensureSessionActive(() => {
      this.checkImpresionAndPrint(datosParaImprimir);
    });
  }

  /** Marca control, muestra snackbar y retorna true si inválido. */
  private blockIfInvalid(ctrl: AbstractControl): boolean {
    if (ctrl.valid) return false;
    ctrl.markAsTouched();
    ctrl.updateValueAndValidity();
    this.showSnack(this.nombreError ?? 'El nombre no es válido.');
    return true;
  }

  /** Verifica sesión activa antes de continuar. */
  private ensureSessionActive(onActive: () => void): void {
    this.authService.sessionActive$.pipe(take(1)).subscribe(isActive => {
      if (!isActive) {
        this.authService.logout();
        return;
      }
      onActive();
    });
  }

  /** Consulta si se puede imprimir; si sí, imprime y registra; si ya estaba impresa, cierra. */
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

  /** Imprime (2 filas) y registra estado; al finalizar, cierra y recarga. */
  private performPrintAndRegister(datosParaImprimir: Tarjeta): void {
    const tipoDiseno = false; // siempre 2 filas (en tu servicio: true=1 fila, false=2 filas)
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

  private showSnack(message: string): void {
    this.snackBar.open(message, 'Cerrar', {
      duration: 3500,
      verticalPosition: 'top',
      horizontalPosition: 'center'
    });
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
          placeholder="NOMBRE EN TARJETA"
          matInput
          formControlName="nombre"
          (input)="form.get('nombre')?.setValue((form.get('nombre')?.value || '').toUpperCase(), { emitEvent: true })"
          maxlength="40"
          autocomplete="off" />

        <mat-hint align="end">{{ (form.get('nombre')?.value?.length || 0) }}/40</mat-hint>

        @if (nombreError) {
          <mat-error>{{ nombreError }}</mat-error>
        }
      </mat-form-field>

      <!-- Botones -->
      <button mat-button class="imprimir-btn" (click)="imprimir(tarjeta)">Imprimir</button>
      <span class="spacer"></span>
      <button mat-button class="cerrar-btn" (click)="cerrarModal()" [mat-dialog-close]="true">Cerrar</button>
    </div>
  </div>
</form>

