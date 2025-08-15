<h1 mat-dialog-title> Detalle Tarjeta</h1>
<div mat-dialog-content id="contenidoImprimir">
  <div class="contenedor">
    <div class="content-imagen-tarjeta">
      <img
        [src]="disenoSeleccionado === 'unaFila' ? '/assets/TarjetaDiseño2.png' : '/assets/Tarjeta3.PNG'"
        alt="imagen tarjeta"
        class="imagen-tarjeta no-imprimir">

      <!-- DISEÑO 1 -->
      <ng-container *ngIf="disenoSeleccionado === 'unaFila'">
        <div class="nombres-una-fila">
          <b>{{tarjeta.nombre}}</b>
        </div>
        <div class="cuenta-una-fila">
          <b>{{tarjeta.numeroCuenta | maskAccountNumber}}</b>
        </div>
      </ng-container>

      <!-- DISEÑO 2 -->
      <ng-container *ngIf="disenoSeleccionado === 'dosFilas'">
        <div class="nombres"><b>{{nombres}}</b></div>
        <div class="apellidos"><b>{{apellidos}}</b></div>
        <div class="cuenta"><b>{{tarjeta.numeroCuenta | maskAccountNumber}}</b></div>
      </ng-container>
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

    <!-- Campo Nombre: el matcher va en el INPUT -->
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
      <mat-hint align="end">{{nombreCharsCount}}/{{nombreMaxLength}}</mat-hint>
      <mat-error *ngIf="nombreError">{{ nombreError }}</mat-error>
    </mat-form-field>

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
 * Pone el input en estado de error cuando:
 *  - Hay un error externo (nombreError !== '')
 *  - O el control es inválido y fue tocado/ensuciado o el form enviado (comportamiento estándar)
 *
 * NOTA: la firma correcta usa FormControl | null y FormGroupDirective | NgForm | null
 */
class NombreErrorStateMatcher implements ErrorStateMatcher {
  constructor(private hasExternalError: () => boolean) {}

  isErrorState(control: FormControl | null, form: FormGroupDirective | NgForm | null): boolean {
    const submitted = !!form && form.submitted;
    const invalidStandard = !!control && control.invalid && (control.touched || control.dirty || submitted);
    const externalError = this.hasExternalError(); // debe devolver boolean, no null
    return externalError || invalidStandard;
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

  maxCaracteresFila: number = 16;

  // Validación / UI
  nombreError: string = '';
  nombreCharsCount: number = 0;
  nombreMaxLength: number = 26;

  // Matcher que fuerza el estado de error cuando existe nombreError
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
    this.actualizarNombre(tarjeta.nombre);
    this.nombreCompleto = tarjeta.nombre;
    this.imprime = false;

    this.nombreCharsCount = (this.tarjeta.nombre || '').length;
    this.aplicarValidaciones(this.tarjeta.nombre);
  }

  ngOnInit(): void {
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
    this.aplicarValidaciones(this.tarjeta.nombre);

    if (this.nombreError) {
      this.mostrarNotificacionError(this.nombreError);
      return;
    }

    let impresionExitosa = false;

    this.subscription.add(this.authService.sessionActive$.subscribe(isActive => {
      if (isActive) {
        this.tarjetaService.validaImpresion(this.tarjeta.numero).subscribe({
          next: (respuesta) => {
            this.imprime = respuesta.imprime;
          }
        });

        if (!this.imprime) {
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

  actualizarNombre(nombre: string) {
    let nombreActualizado = (nombre || '').toUpperCase();

    nombreActualizado = this.validarNombre(nombreActualizado);

    this.tarjeta.nombre = nombreActualizado;

    if (this.disenoSeleccionado === 'dosFilas') {
      this.dividirNombreCompleto(nombreActualizado);
    }

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
    if (!regex.test(event.key)) {
      event.preventDefault();
    }
  }

  cambiarDiseno() {
    this.actualizarNombre(this.tarjeta.nombre);
  }

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
